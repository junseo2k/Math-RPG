using UnityEngine;
using UnityEngine.InputSystem;

namespace MathRPG
{
    public sealed partial class TrainingGame
    {
        public ForestEncounter Forest { get; private set; }
        ForestVillageView forestView;

        void InitializeForest()
        {
            var data = definition.forest != null ? definition.forest : Resources.Load<ForestVillageDefinition>("MathRPG/ForestVillage");
            var second = definition.secondForest != null ? definition.secondForest : Resources.Load<ForestVillageDefinition>("MathRPG/SecondForestVillage");
            var third = definition.thirdForest != null ? definition.thirdForest : Resources.Load<ForestVillageDefinition>("MathRPG/ThirdForestVillage");
            Forest = new ForestEncounter(this, data, second, third);
            forestView = gameObject.AddComponent<ForestVillageView>();
            forestView.Initialize(enemy);
        }

        void TickForestInput()
        {
            if (Mode == TrainingMode.Playing && Guardian != null && Guardian.GuardianHelpAvailable && GameInput.Pressed(Key.R))
            { Guardian.UseGuardianHelp(); return; }
            if (Mode != TrainingMode.ForestChallenge) return;
            Forest.TickChallenge(Time.unscaledDeltaTime);
            if (Mode != TrainingMode.ForestChallenge) return;
            if (GameInput.Pressed(Key.Digit1)) Forest.Submit(0);
            else if (GameInput.Pressed(Key.Digit2)) Forest.Submit(1);
            else if (GameInput.Pressed(Key.Digit3)) Forest.Submit(2);
            else if (GameInput.Pressed(Key.Digit4)) Forest.Submit(3);
            if (GameInput.Pressed(Key.H)) Forest.UseHint();
        }

        void InteractWithForest()
        {
            if (Forest.BossActive) { Notify("ESC에서 마을로 돌아가면 보스전을 중단할 수 있습니다."); return; }
            if (player.transform.position.x <= TrainingCourseView.EntranceX) Forest.Leave();
            else if (player.transform.position.x >= TrainingCourseView.ExitX) Forest.StartBoss();
            else if (NearInstructor) Forest.Talk();
        }

        internal void PrepareForestVillage()
        {
            ResetArena();
            ambientPlayed = false;
            Ambient = Notice = "";
            questionCursor = 0;
        }

        void ApplyForestRegion()
        {
            climaxView.SetVillage(Forest.InVillage && Progress.forest.villageIndex == 2 && Progress.forest.guardianAwakened);
            forestView.SetVisible(false, Progress.forest.CurrentRelic);
            courseView.SetRegionVisible(true);
            if (!Forest.InVillage) return;
            courseView.SetRegionVisible(false);
            forestView.SetVillage(Progress.forest.villageIndex);
            forestView.SetVisible(true, Progress.forest.CurrentRelic);
            enemy.gameObject.SetActive(false);
        }

        internal void PrepareForestBoss()
        {
            RestoreForestResources();
            Quiz = null;
            InstructorQuiz = ChestQuiz = false;
            attackCooldown = quickCooldown = runeCooldown = castDelay = 0;
            Combo = 0;
            Ambient = "";
            ambientPlayed = true;
            player.ResetAtCheckpoint();
            player.transform.position = new Vector3(-1, PlayerMotor.GroundY, 0);
            enemy.ResetEnemy(Forest.Definition.bossHealth, Forest.Definition.attackWindup);
            courseView.HideOpponentVisuals();
            forestView.SetBossMode(true);
        }

        internal void SetForestBossVisible(bool visible) => forestView.SetBossMode(visible);

        internal void ResolveForestRune()
        {
            int damage = Forest.Definition.runeDamage;
            int direction = enemy.transform.position.x >= player.transform.position.x ? 1 : -1;
            Effects.Charge(player.transform.position + Vector3.up);
            if (damage > 0 && enemy.Phase != EnemyPhase.Defeated) Effects.Impact(enemy.transform.position + Vector3.up, true, hitVisual: EnemyHitVisual);
            enemy.ReceiveDamage(damage, direction, true);
            UI.FloatText(enemy.transform.position + Vector3.up * 1.5f, damage.ToString(), true);
            Health = Mathf.Min(definition.playerHealth, Health + Forest.Definition.correctHeal);
            Notify("계산의 흐름이 이어졌다. 장갑 마법이 상대의 힘을 끊는다.");
        }

        internal void RestoreForestResources()
        { Health = definition.playerHealth; Hints = definition.hintCharges; }
        internal void SetForestHints(int remaining) => Hints = remaining;
        internal void ShowForestMode(TrainingMode mode) => SetMode(mode);
        internal void SaveForestProgress() => WriteCheckpoint();

        internal void RecordForestVictory()
        {
            castDelay = 0;
            RestoreForestResources();
            forestView.SetVisible(true, true);
            forestView.SetBossMode(false);
            WriteCheckpoint();
            SetMode(TrainingMode.ForestRelic);
        }
    }
}
