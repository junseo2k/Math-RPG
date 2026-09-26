using System;
using System.Collections.Generic;

namespace MathRPG
{
    public enum AnswerResult { Ignored, Wrong, Advanced, Completed }

    // Pure state machine: the UI never owns answer correctness or progression.
    public sealed class QuizSession
    {
        readonly QuestionDefinition question;
        readonly HashSet<int> eliminated = new HashSet<int>();
        public int StepIndex { get; private set; }
        public int Mistakes { get; private set; }
        public int HintsRemaining { get; private set; }
        public bool HintVisible { get; private set; }
        public bool Completed { get; private set; }
        public BlockCalculationResult LastCalculation { get; private set; }
        public QuestionStep Step => question.steps[StepIndex];
        public int StepCount => question.steps.Length;
        public string Title => question.title;
        public bool IsEliminated(int index) => eliminated.Contains(index);

        public QuizSession(QuestionDefinition definition, int hints, bool mastered = false)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (!definition.IsValid(out var reason)) throw new ArgumentException(reason);
            question = definition;
            HintsRemaining = Math.Max(0, hints);
            StepIndex = mastered ? definition.steps.Length - 1 : 0;
        }

        public AnswerResult Answer(int index)
        {
            if (Completed || Step.type != QuestionStepType.MultipleChoice || index < 0 || index >= Step.choices.Length || eliminated.Contains(index))
                return AnswerResult.Ignored;
            if (index != Step.correctIndex)
            {
                eliminated.Add(index);
                Mistakes++;
                return AnswerResult.Wrong;
            }
            return Advance();
        }

        public AnswerResult AnswerBlocks(BlockSubmission submission)
        {
            if (Completed || Step.type != QuestionStepType.BlockInput) return AnswerResult.Ignored;
            var calculation = Step.calculation;
            if (submission == null || submission.Blocks == null ||
                submission.Blocks.Length != calculation.SlotCount || Array.Exists(submission.Blocks, index => index == -1) ||
                (calculation.function == CalculationFunction.Arithmetic && (submission.Links == null ||
                    submission.Links.Length != calculation.SlotCount - 1 || Array.Exists(submission.Links, link => (int)link == -1))))
                return AnswerResult.Ignored;
            LastCalculation = BlockCalculator.Calculate(calculation, submission);
            if (!LastCalculation.Executed || !calculation.AcceptsPath(submission) || !calculation.ResultsMatch(LastCalculation.Values))
            {
                Mistakes++;
                return AnswerResult.Wrong;
            }
            return Advance();
        }

        AnswerResult Advance()
        {
            if (StepIndex == question.steps.Length - 1)
            { Completed = true; return AnswerResult.Completed; }
            StepIndex++;
            eliminated.Clear();
            HintVisible = false;
            return AnswerResult.Advanced;
        }

        public bool UseHint()
        {
            if (Completed || HintVisible || HintsRemaining <= 0) return false;
            HintsRemaining--;
            HintVisible = true;
            return true;
        }
    }
}
