using UnityEngine;
using UnityEngine.InputSystem;

namespace MathRPG
{
    public sealed partial class TrainingGame
    {
        public RivalSecondEncounter RivalSecond { get; private set; }
        void InitializeRivalSecond() => RivalSecond = new RivalSecondEncounter(this, Resources.Load<RivalSecondDefinition>("MathRPG/RivalSecond"));

        void TickRivalSecondInput(float dt)
        {
            if (Time.unscaledTime >= Effects.FreezeUntil) RivalSecond.Tick(dt);
            if (Mode == TrainingMode.RivalSecondBreakdown)
            { if (GameInput.Pressed(Key.Q)) RivalSecond.FailRune(); return; }
            if (Mode != TrainingMode.RivalSecondChallenge) return;
            if (GameInput.Pressed(Key.Digit1)) RivalSecond.Submit(0);
            else if (GameInput.Pressed(Key.Digit2)) RivalSecond.Submit(1);
            else if (GameInput.Pressed(Key.Digit3)) RivalSecond.Submit(2);
            else if (GameInput.Pressed(Key.Digit4)) RivalSecond.Submit(3);
            if (GameInput.Pressed(Key.H)) RivalSecond.UseHint();
        }

        internal void PrepareRivalSecond()
        {
            PrepareHumanVillage();
            ambientPlayed = true;
            player.transform.position = new Vector3(-1, PlayerMotor.GroundY, 0);
            enemy.ResetEnemy(RivalSecond.Definition.health, RivalSecond.Definition.attackWindup);
            courseView.HideOpponentVisuals();
            humanView.SetRivalSecondMode();
        }

        internal void MoveRivalSecond(float dt)
        {
            float delta = player.transform.position.x - enemy.transform.position.x;
            if (Mathf.Abs(delta) <= 1.8f) return;
            var target = enemy.transform.position;
            target.x = player.transform.position.x - Mathf.Sign(delta) * 1.8f;
            enemy.transform.position = Vector3.MoveTowards(enemy.transform.position, target, RivalSecond.Definition.moveSpeed * dt);
        }

        internal void StrikeRivalSecond(bool low)
        {
            float delta = player.transform.position.x - enemy.transform.position.x;
            Effects.Slash(enemy.transform.position + new Vector3(0, low ? .3f : 1.35f), delta < 0 ? -1 : 1, false);
            if (Mathf.Abs(delta) > RivalSecond.Definition.attackRange || player.Dodges(low) || player.Invulnerable) return;
            player.KnockBack(Mathf.Sign(delta));
            DamagePlayer(RivalSecond.Definition.attackDamage, false);
        }

        internal void ResolveRivalSecondRune()
        {
            int damage = RivalSecond.ReceiveHit(RivalSecond.Definition.runeDamage);
            Effects.Charge(player.transform.position + Vector3.up);
            if (damage > 0) Effects.Impact(enemy.transform.position + Vector3.up, true, hitVisual: EnemyHitVisual);
            UI.FloatText(enemy.transform.position + Vector3.up * 1.5f, damage.ToString(), true);
        }

        internal void StopRivalSecondCast() { castDelay = 0; Quiz = null; }

        internal void ShowRivalSecondEnding()
        {
            ResetArena();
            courseView.SetRegionVisible(false);
            enemy.gameObject.SetActive(false);
            player.gameObject.SetActive(false);
            if (spirit != null) spirit.gameObject.SetActive(false);
            SetMode(TrainingMode.HumanChapterComplete);
        }
    }
}
