using UnityEngine;
using UnityEngine.InputSystem;

namespace MathRPG
{
    public sealed partial class TrainingGame
    {
        public RivalSecondRematchEncounter SecondRematch { get; private set; }
        RivalSecondRematchView secondRematchView;
        public bool InSecondRematch => Progress.chapterFour.secondRematch != RematchProgress.None;

        void InitializeSecondRematch()
        {
            SecondRematch = new RivalSecondRematchEncounter(this, Resources.Load<RivalSecondRematchDefinition>("MathRPG/RivalSecondRematch"));
            secondRematchView = gameObject.AddComponent<RivalSecondRematchView>();
            secondRematchView.Initialize(this);
        }

        internal bool SaveSecondRematch(RematchProgress next)
        {
            var previous = Progress.chapterFour.secondRematch;
            Progress.chapterFour.secondRematch = next;
            if (CheckpointStore.Save(SavePath, Progress, out var error)) return true;
            Progress.chapterFour.secondRematch = previous;
            Notify("기억을 남기지 못했습니다: " + error, 8);
            UI.RefreshOverlay(Notice);
            return false;
        }

        internal void PrepareSecondRematch()
        {
            ResetArena();
            courseView.SetRegionVisible(false);
            courseView.HideOpponentVisuals();
            if (instructor != null) instructor.gameObject.SetActive(false);
            player.transform.position = new Vector3(-3, PlayerMotor.GroundY, 0);
            enemy.gameObject.SetActive(true);
            enemy.transform.position = new Vector3(3, PlayerMotor.GroundY, 0);
            ambientPlayed = true;
            Ambient = Notice = "";
            secondRematchView.Show();
        }

        internal void ShowSecondRematchMode(TrainingMode mode) => SetMode(mode);
        internal void SetSecondRematchHints(int value) => Hints = value;
        internal void HideSecondRematchOpponent()
        {
            enemy.gameObject.SetActive(false);
            secondRematchView.HideOpponent();
        }

        void TickSecondRematchInput()
        {
            if (Mode != TrainingMode.SecondRematchChallenge) return;
            if (GameInput.Pressed(Key.Digit1)) SecondRematch.Submit(0);
            else if (GameInput.Pressed(Key.Digit2)) SecondRematch.Submit(1);
            else if (GameInput.Pressed(Key.Digit3)) SecondRematch.Submit(2);
            else if (GameInput.Pressed(Key.Digit4)) SecondRematch.Submit(3);
            if (GameInput.Pressed(Key.H)) SecondRematch.UseHint();
        }

        void ClampSecondRematchPlayer()
        {
            if (!SecondRematch.Active) return;
            var point = player.transform.position;
            point.x = Mathf.Clamp(point.x, -SecondRematch.Definition.arenaHalfWidth, SecondRematch.Definition.arenaHalfWidth);
            player.transform.position = point;
        }

        internal void MoveSecondRematch(float dt)
        {
            float delta = player.transform.position.x - enemy.transform.position.x;
            if (Mathf.Abs(delta) <= 1.8f) return;
            var target = enemy.transform.position;
            target.x = Mathf.Clamp(player.transform.position.x - Mathf.Sign(delta) * 1.8f,
                -SecondRematch.Definition.arenaHalfWidth, SecondRematch.Definition.arenaHalfWidth);
            enemy.transform.position = Vector3.MoveTowards(enemy.transform.position, target, SecondRematch.Definition.moveSpeed * dt);
        }

        internal void StrikeSecondRematch(bool low)
        {
            float delta = player.transform.position.x - enemy.transform.position.x;
            Effects.Slash(enemy.transform.position + new Vector3(0, low ? .3f : 1.35f), delta < 0 ? -1 : 1, false);
            if (Mathf.Abs(delta) > SecondRematch.Definition.attackRange || player.Dodges(low) || player.Invulnerable) return;
            SecondRematch.SetDeathCause(low ? "라이벌2의 낮은 검격" : "라이벌2의 높은 검격");
            player.KnockBack(Mathf.Sign(delta));
            DamagePlayer(SecondRematch.Definition.swordDamage + SecondRematch.Rage * SecondRematch.Definition.rageDamage, false);
        }

        internal void ResolveSecondRematchHazard()
        {
            Effects.Impact(new Vector3(SecondRematch.HazardX, PlayerMotor.GroundY + .2f, 0), true);
            if (Mathf.Abs(player.transform.position.x - SecondRematch.HazardX) > SecondRematch.HazardRadius) return;
            SecondRematch.SetDeathCause(SecondRematch.LethalHazard ? "넓은 즉사 장판 안에 머물렀다" : "좁은 룬 장판의 피해");
            DamagePlayer(SecondRematch.LethalHazard ? Health : SecondRematch.Definition.narrowHazardDamage, false);
        }

        internal void ResolveSecondRematchRune()
        {
            Effects.Charge(player.transform.position + Vector3.up);
            int damage = SecondRematch.ReceiveHit(SecondRematch.Definition.runeDamage);
            if (damage > 0) Effects.Impact(enemy.transform.position + Vector3.up, true, hitVisual: EnemyHitVisual);
            UI.FloatText(enemy.transform.position + Vector3.up * 1.5f, damage.ToString(), true);
        }

        internal void ShowSecondRematchMisfire()
        {
            Effects.Charge(enemy.transform.position + Vector3.up);
            UI.FloatText(enemy.transform.position + Vector3.up * 1.8f, "발동 실패", false);
        }
    }
}
