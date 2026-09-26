using UnityEngine;
using UnityEngine.InputSystem;

namespace MathRPG
{
    public sealed partial class TrainingGame
    {
        public HumanVillageEncounter Human { get; private set; }
        HumanVillageView humanView;

        void InitializeHuman()
        {
            var data = definition.humanVillage != null ? definition.humanVillage : Resources.Load<HumanVillageDefinition>("MathRPG/HumanVillage");
            Human = new HumanVillageEncounter(this, data);
            humanView = gameObject.AddComponent<HumanVillageView>();
            humanView.Initialize(enemy);
        }

        void TickHumanInput()
        {
            if (Mode != TrainingMode.HumanChallenge) return;
            if (GameInput.Pressed(Key.Digit1)) Human.Submit(0);
            else if (GameInput.Pressed(Key.Digit2)) Human.Submit(1);
            else if (GameInput.Pressed(Key.Digit3)) Human.Submit(2);
            else if (GameInput.Pressed(Key.Digit4)) Human.Submit(3);
            if (GameInput.Pressed(Key.H)) Human.UseHint();
        }

        void InteractWithHuman()
        {
            if (Human.BossActive) { Notify("ESC에서 마을로 돌아가면 보스전을 중단할 수 있습니다."); return; }
            if (player.transform.position.x <= TrainingCourseView.EntranceX) Human.Leave();
            else if (player.transform.position.x >= TrainingCourseView.ExitX) Human.StartBoss();
            else if (NearInstructor) Human.Talk();
        }

        internal void PrepareHumanVillage()
        {
            ResetArena();
            ambientPlayed = false;
            Ambient = Notice = "";
            questionCursor = 0;
        }

        void ApplyHumanRegion()
        {
            humanView.SetVillage(Human.Progress.villageIndex);
            humanView.SetVisible(Human.InVillage);
            if (!Human.InVillage) return;
            courseView.SetRegionVisible(false);
            enemy.gameObject.SetActive(false);
        }

        internal void PrepareHumanBoss()
        {
            RestoreHumanResources();
            Quiz = null;
            InstructorQuiz = ChestQuiz = false;
            attackCooldown = quickCooldown = runeCooldown = castDelay = 0;
            Combo = 0;
            Ambient = "";
            ambientPlayed = true;
            player.ResetAtCheckpoint();
            player.transform.position = new Vector3(-1, PlayerMotor.GroundY, 0);
            enemy.ResetEnemy(Human.Definition.bossHealth, Human.Definition.attackWindup);
            courseView.HideOpponentVisuals();
            humanView.SetBossMode();
        }

        internal void ResolveHumanRune()
        {
            int damage = Human.Definition.runeDamage;
            Effects.Charge(player.transform.position + Vector3.up);
            if (damage > 0 && enemy.Phase != EnemyPhase.Defeated) Effects.Impact(enemy.transform.position + Vector3.up, true, hitVisual: EnemyHitVisual);
            enemy.ReceiveDamage(damage, enemy.transform.position.x >= player.transform.position.x ? 1 : -1, true);
            UI.FloatText(enemy.transform.position + Vector3.up * 1.5f, damage.ToString(), true);
            Health = Mathf.Min(definition.playerHealth, Health + Human.Definition.correctHeal);
            Notify("흩어진 식이 이어졌다. " + Human.Definition.bossName + "의 흐름을 끊었다.");
        }

        internal void RestoreHumanResources() { Health = definition.playerHealth; Hints = definition.hintCharges; }
        internal void SetHumanHints(int remaining) => Hints = remaining;
        internal void ShowHumanMode(TrainingMode mode) => SetMode(mode);
        internal void SaveHumanProgress() => WriteCheckpoint();

        internal void RecordHumanVictory()
        {
            PrepareHumanVillage();
            WriteCheckpoint();
            SetMode(TrainingMode.HumanAftermath);
        }
    }
}
