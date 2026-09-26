using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace MathRPG.Tests
{
    public class QuizAndCheckpointTests
    {
        QuestionDefinition question;
        string path;

        [SetUp]
        public void SetUp()
        {
            question = ScriptableObject.CreateInstance<QuestionDefinition>();
            question.conceptId = "test";
            question.steps = new[]
            {
                new QuestionStep { prompt = "Pick a concept", choices = new[] { "A", "B", "C", "D" }, correctIndex = 2, hint = "Consider C" },
                new QuestionStep { prompt = "Apply the concept", choices = new[] { "4", "-4" }, correctIndex = 0, hint = "Positive" }
            };
            path = Path.GetFullPath(Path.Combine(Application.dataPath, "../Temp/MathRPGTests/" + Guid.NewGuid() + ".json"));
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(question);
            foreach (var file in new[] { path, path + ".bak", path + ".tmp" }) if (File.Exists(file)) File.Delete(file);
        }

        [Test]
        public void WrongChoiceIsRemovedWithoutAdvancingOrDoublePenalty()
        {
            var quiz = new QuizSession(question, 3);
            Assert.AreEqual(AnswerResult.Wrong, quiz.Answer(1));
            Assert.IsTrue(quiz.IsEliminated(1));
            Assert.AreEqual(0, quiz.StepIndex);
            Assert.AreEqual(AnswerResult.Ignored, quiz.Answer(1));
            Assert.AreEqual(1, quiz.Mistakes);
            Assert.AreEqual(AnswerResult.Advanced, quiz.Answer(2));
            Assert.IsFalse(quiz.IsEliminated(1));
        }

        [Test]
        public void HintIsChargedOncePerStepAndCannotGoBelowZero()
        {
            var quiz = new QuizSession(question, 1);
            Assert.IsTrue(quiz.UseHint());
            Assert.IsFalse(quiz.UseHint());
            Assert.AreEqual(0, quiz.HintsRemaining);
            quiz.Answer(2);
            Assert.IsFalse(quiz.HintVisible);
            Assert.IsFalse(quiz.UseHint());
        }

        [Test]
        public void CompletingFinalStepCannotCastTwice()
        {
            var quiz = new QuizSession(question, 3);
            quiz.Answer(2);
            Assert.AreEqual(AnswerResult.Completed, quiz.Answer(0));
            Assert.IsTrue(quiz.Completed);
            Assert.AreEqual(AnswerResult.Ignored, quiz.Answer(0));
        }

        [Test]
        public void SkillBookSkipsSetupStep()
        {
            var quiz = new QuizSession(question, 3, true);
            Assert.AreEqual(1, quiz.StepIndex);
            Assert.AreEqual(AnswerResult.Completed, quiz.Answer(0));
        }

        [Test]
        public void InvalidQuestionCannotStart()
        {
            question.steps[0].correctIndex = 99;
            Assert.Throws<ArgumentException>(() => new QuizSession(question, 0));
        }

        [TestCase(2d)]
        [TestCase(-2d)]
        [TestCase(.5d)]
        [TestCase(12d)]
        public void BlockInputIgnoresIncompleteSlotsAndPreservesAuthoredNumbers(double first)
        {
            var calculation = new BlockCalculationDefinition
            {
                blocks = new[]
                {
                    new CalculationBlock { label = "first", number = first },
                    new CalculationBlock { label = "3", number = 3 }
                },
                acceptedPaths = new[] { new BlockAnswerPath { blocks = new[] { 0, 1 }, links = new[] { ArithmeticLink.Add } } },
                expectedResults = new[] { first + 3 }
            };
            question.steps = new[] { new QuestionStep { type = QuestionStepType.BlockInput,
                prompt = "Add the numbers", hint = "Use addition", calculation = calculation } };
            var quiz = new QuizSession(question, 0);
            Assert.AreEqual(AnswerResult.Ignored,
                quiz.AnswerBlocks(new BlockSubmission(new[] { 0, -1 }, new[] { ArithmeticLink.Add })));
            Assert.AreEqual(AnswerResult.Ignored,
                quiz.AnswerBlocks(new BlockSubmission(new[] { 0, 1 }, new[] { (ArithmeticLink)(-1) })));
            Assert.AreEqual(0, quiz.Mistakes);
            Assert.IsNull(quiz.LastCalculation);
            int firstIndex = first == 2 ? calculation.blocks.Length + 2 : 0;
            Assert.AreEqual(AnswerResult.Completed, quiz.AnswerBlocks(new BlockSubmission(
                new[] { firstIndex, calculation.blocks.Length + 3 }, new[] { ArithmeticLink.Add })));
            Assert.AreEqual(first + 3, quiz.LastCalculation.Values[0]);
        }

        [Test]
        public void BasicDigitCannotReplaceAnExpressionInTheAcceptedPath()
        {
            var calculation = new BlockCalculationDefinition
            {
                blocks = new[]
                {
                    new CalculationBlock { label = "constant expression", kind = CalculationBlockKind.Expression, constant = 2 },
                    new CalculationBlock { label = "3", number = 3 }
                },
                acceptedPaths = new[] { new BlockAnswerPath { blocks = new[] { 0, 1 }, links = new[] { ArithmeticLink.Add } } },
                expectedResults = new[] { 5d }
            };
            question.steps = new[] { new QuestionStep { type = QuestionStepType.BlockInput,
                prompt = "Use the expression", hint = "Choose its block", calculation = calculation } };
            var quiz = new QuizSession(question, 0);
            Assert.AreEqual(AnswerResult.Wrong, quiz.AnswerBlocks(new BlockSubmission(
                new[] { calculation.blocks.Length + 2, calculation.blocks.Length + 3 }, new[] { ArithmeticLink.Add })));
            Assert.AreEqual(1, quiz.Mistakes);
            Assert.AreEqual(AnswerResult.Completed, quiz.AnswerBlocks(new BlockSubmission(
                new[] { 0, calculation.blocks.Length + 3 }, new[] { ArithmeticLink.Add })));
        }

        [Test]
        public void SaveLoadAndAtomicReplacementPreserveProgress()
        {
            var original = new CheckpointData();
            Assert.IsTrue(CheckpointStore.Save(path, original, out var error), error);
            original.instructorCleared = true;
            original.stages[0].cleared = true;
            original.stages[0].trialAccepted = true;
            original.skillBooks = 1;
            original.deaths = 2;
            Assert.IsTrue(CheckpointStore.Save(path, original, out error), error);
            Assert.IsTrue(File.Exists(path + ".bak"));
            Assert.IsTrue(CheckpointStore.Load(path, out var loaded, out error), error);
            Assert.IsTrue(loaded.instructorCleared);
            Assert.AreEqual(1, loaded.skillBooks);
            Assert.AreEqual(2, loaded.deaths);
        }

        [Test]
        public void ForestChapterTwoFlagsSurviveSaveAndLoad()
        {
            var data = new CheckpointData { rivalDefeated = true, currentStage = 3 };
            for (int i = 0; i < data.stages.Length; i++) { data.stages[i].cleared = true; data.stages[i].trialAccepted = true; }
            data.skillBooks = data.stages.Length;
            data.forest.inVillage = true;
            data.forest.visited = data.forest.questAccepted = data.forest.triangleRelic = true;
            data.forest.secondVisited = data.forest.secondQuestAccepted = data.forest.parallelogramRelic = true;
            data.forest.thirdVisited = data.forest.thirdQuestAccepted = data.forest.circleRelic = true;
            data.forest.villageIndex = 2;
            data.forest.letterWritten = data.forest.outbreakWitnessed = data.forest.guardianAwakened = true;
            data.forest.guardianHelpUsed = true;
            Assert.IsTrue(CheckpointStore.Save(path, data, out var error), error);
            Assert.IsTrue(CheckpointStore.Load(path, out var loaded, out error), error);
            Assert.IsTrue(loaded.forest.outbreakWitnessed);
            Assert.IsTrue(loaded.forest.guardianHelpUsed);
        }

        [TestCase("not json")]
        [TestCase("{}")]
        [TestCase("{\"version\":99,\"skillBooks\":0}")]
        [TestCase("{\"version\":1,\"skillBooks\":-1}")]
        [TestCase("{\"version\":1,\"skillBooks\":1,\"instructorCleared\":false}")]
        public void BadSaveDoesNotReplaceProgress(string json)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, json);
            Assert.IsFalse(CheckpointStore.Load(path, out var loaded, out var error));
            Assert.IsNull(loaded);
            Assert.IsNotEmpty(error);
        }
    }
}
