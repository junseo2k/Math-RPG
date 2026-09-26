using UnityEngine;
using UnityEngine.InputSystem;

namespace MathRPG
{
    public sealed partial class TrainingGame
    {
        public RivalEncounter Rival { get; private set; }

        void InitializeRival()
        {
            var data = definition.rival != null ? definition.rival : Resources.Load<RivalDuelDefinition>("MathRPG/RivalDuel");
            Rival = new RivalEncounter(this, data);
        }

        void TickRivalInput(float dt)
        {
            if (Mode != TrainingMode.RivalChallenge) return;
            Rival.TickChallenge(dt);
            if (GameInput.Pressed(Key.Digit1)) Rival.Submit(0);
            else if (GameInput.Pressed(Key.Digit2)) Rival.Submit(1);
            else if (GameInput.Pressed(Key.Digit3)) Rival.Submit(2);
            else if (GameInput.Pressed(Key.Digit4)) Rival.Submit(3);
            if (GameInput.Pressed(Key.H)) Rival.UseHint();
        }

        internal void PrepareRivalEncounter()
        {
            Trials.ResetSession();
            Health = definition.playerHealth;
            Hints = definition.hintCharges;
            Quiz = null;
            InstructorQuiz = ChestQuiz = false;
            attackCooldown = quickCooldown = runeCooldown = castDelay = 0;
            Combo = 0;
            Ambient = Notice = "";
            ambientPlayed = true;
            player.ResetAtCheckpoint();
            player.transform.position = new Vector3(-2, PlayerMotor.GroundY, 0);
            enemy.ResetEnemy(Rival.Definition.health, Rival.Definition.openingWindup);
            courseView.SetRival();
        }

        internal void ShowRivalMode(TrainingMode mode) => SetMode(mode);
        internal void SetRivalHints(int remaining) => Hints = remaining;

        internal void ResolveRivalExchange(int playerPower, int rivalPower)
        {
            int direction = enemy.transform.position.x >= player.transform.position.x ? 1 : -1;
            if (playerPower > 0)
            {
                enemy.ReceiveDamage(playerPower, direction, true);
                Effects.Slash(player.transform.position + Vector3.up, direction, true);
                Effects.Impact(enemy.transform.position + Vector3.up, true, hitVisual: EnemyHitVisual);
                UI.FloatText(enemy.transform.position + Vector3.up * 1.5f, playerPower.ToString(), true);
                Health = Mathf.Min(definition.playerHealth, Health + 4);
            }
            if (rivalPower > 0)
            {
                Effects.Slash(enemy.transform.position + Vector3.up, -direction, true, Rival.Definition.rivalPowerMultiplier);
                DamagePlayer(rivalPower, true);
                UI.FloatText(player.transform.position + Vector3.up * 1.5f, rivalPower.ToString(), false);
            }
        }

        internal void RecordRivalVictory()
        {
            Progress.rivalDefeated = true;
            castDelay = 0;
            WriteCheckpoint();
            SetMode(TrainingMode.ChapterClear);
        }

        public void LeaveRivalEncounter()
        {
            if (!Rival.InEncounter || Mode == TrainingMode.GameOver) return;
            ResetArena();
            SetMode(TrainingMode.Playing);
            Notify(Progress.rivalDefeated ? "라이벌과의 결투를 마쳤습니다. 챕터1 완료." : "첨탑으로 돌아왔습니다. 오른쪽 문에서 다시 도전할 수 있습니다.", 4);
        }
    }
}
