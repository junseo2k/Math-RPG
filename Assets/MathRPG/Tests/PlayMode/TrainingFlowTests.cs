using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MathRPG.Tests
{
    public class TrainingFlowTests
    {
        TrainingGame game;
        string path;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return SceneManager.LoadSceneAsync("TrainingPrototype");
            yield return null;
            game = UnityEngine.Object.FindFirstObjectByType<TrainingGame>();
            Assert.IsNotNull(game);
            path = Path.GetFullPath(Path.Combine(Application.dataPath, "../Temp/MathRPGTests/" + Guid.NewGuid() + ".json"));
            game.CheckpointPath = path;
            while (game.Mode == TrainingMode.Introduction) game.AdvanceIntro();
            Assert.AreEqual(TrainingMode.Playing, game.Mode);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var file in new[] { path, path + ".bak", path + ".tmp" }) if (file != null && File.Exists(file)) File.Delete(file);
        }

        [UnityTest]
        public IEnumerator NormalAttackAndQuickSkillNeedNoQuestion()
        {
            game.player.transform.position = new Vector3(3.5f, PlayerMotor.GroundY);
            int hp = game.enemy.Health;
            game.Attack();
            Assert.Less(game.enemy.Health, hp);
            hp = game.enemy.Health;
            game.QuickSkill();
            Assert.Less(game.enemy.Health, hp);
            Assert.AreEqual(TrainingMode.Playing, game.Mode);
            Assert.IsNull(game.Quiz);
            yield return null;
        }

        [UnityTest]
        public IEnumerator QuizFreezesEnemyThenCastsAndChargesHintOnlyOnce()
        {
            game.player.transform.position = new Vector3(1, PlayerMotor.GroundY);
            game.BeginRune();
            Assert.AreEqual(TrainingMode.Quiz, game.Mode);
            var position = game.enemy.transform.position;
            yield return new WaitForSeconds(.15f);
            Assert.AreEqual(position, game.enemy.transform.position);
            int health = game.Health;
            int wrong = (game.Quiz.Step.correctIndex + 1) % game.Quiz.Step.choices.Length;
            game.SubmitAnswer(wrong);
            game.SubmitAnswer(wrong);
            Assert.AreEqual(health - game.definition.wrongAnswerDamage, game.Health);
            game.UseHint(); game.UseHint();
            Assert.AreEqual(game.definition.hintCharges - 1, game.Hints);
            int enemyHealth = game.enemy.Health;
            while (game.Mode == TrainingMode.Quiz) game.SubmitAnswer(game.Quiz.Step.correctIndex);
            yield return new WaitForSeconds(.55f);
            Assert.Less(game.enemy.Health, enemyHealth);
            Assert.Greater(game.RuneCooldown, 0);
        }

        [UnityTest]
        public IEnumerator InstructorRewardIsUniqueAndSurvivesLoad()
        {
            game.player.transform.position = new Vector3(-8, PlayerMotor.GroundY);
            game.BeginExam();
            while (game.Mode == TrainingMode.Quiz) game.SubmitAnswer(game.Quiz.Step.correctIndex);
            Assert.AreEqual(TrainingMode.Reward, game.Mode);
            Assert.AreEqual(1, game.Progress.skillBooks);
            Assert.IsTrue(File.Exists(path));
            game.Resume();
            game.BeginExam();
            Assert.AreEqual(1, game.Progress.skillBooks);
            game.LoadCheckpoint();
            Assert.AreEqual(1, game.Progress.skillBooks);
            Assert.IsTrue(game.Progress.instructorCleared);
            game.player.transform.position = new Vector3(1, PlayerMotor.GroundY);
            game.BeginRune();
            Assert.AreEqual(game.Quiz.StepCount - 1, game.Quiz.StepIndex);
            yield return null;
        }

        [UnityTest]
        public IEnumerator DeathAndRewindRestoreArenaAndHintResource()
        {
            game.DamagePlayer(1000, false);
            Assert.AreEqual(TrainingMode.GameOver, game.Mode);
            Assert.AreEqual(1, game.Progress.deaths);
            game.Rewind();
            Assert.AreEqual(TrainingMode.Playing, game.Mode);
            Assert.AreEqual(game.definition.playerHealth, game.Health);
            Assert.AreEqual(game.definition.hintCharges, game.Hints);
            Assert.AreEqual(-6, game.player.transform.position.x);
            Assert.AreEqual(game.definition.enemyHealth, game.enemy.Health);
            yield return null;
        }

        [UnityTest]
        public IEnumerator JumpAndCrouchAvoidDifferentHeightsAndLandOnFloor()
        {
            game.Pause();
            game.player.Tick(.016f, 0, false, true);
            Assert.IsTrue(game.player.Dodges(false));
            Assert.IsFalse(game.player.Dodges(true));
            game.player.Tick(.1f, 0, true, false);
            game.player.Tick(.1f, 0, false, false);
            Assert.IsTrue(game.player.Dodges(true));
            Assert.IsFalse(game.player.Dodges(false));
            for (int i = 0; i < 120; i++) game.player.Tick(1f / 60, 0, false, false);
            Assert.IsTrue(game.player.Grounded);
            Assert.AreEqual(PlayerMotor.GroundY, game.player.transform.position.y);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SafeCheckpointRestoresResourcesAndRejectsCombatSave()
        {
            game.DamagePlayer(10, false);
            game.SaveCheckpoint();
            Assert.IsTrue(File.Exists(path));
            Assert.AreEqual(game.definition.playerHealth, game.Health);
            game.player.transform.position = new Vector3(4, PlayerMotor.GroundY);
            Assert.IsFalse(game.CanSave);
            yield return null;
        }
    }
}
