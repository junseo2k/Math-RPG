using UnityEngine;
using UnityEngine.InputSystem;

namespace MathRPG
{
    public sealed partial class TrainingGame
    {
        public HumanClimaxEncounter HumanClimax { get; private set; }
        HumanClimaxAttackPattern humanClimaxAttack;
        public string HumanClimaxWarning => HumanClimax.Active && humanClimaxAttack != null && humanClimaxAttack.WindingUp
            ? (humanClimaxAttack.LowSweep ? "낮게 범람한다 — SPACE 점프" : "높게 휩쓴다 — S 숙이기") : "";

        void InitializeHumanClimax()
            => HumanClimax = new HumanClimaxEncounter(this, Resources.Load<HumanClimaxDefinition>("MathRPG/HumanClimax"));

        void TickHumanClimaxInput()
        {
            if (Mode != TrainingMode.HumanClimaxChallenge) return;
            if (GameInput.Pressed(Key.Digit1)) HumanClimax.Submit(0);
            else if (GameInput.Pressed(Key.Digit2)) HumanClimax.Submit(1);
            else if (GameInput.Pressed(Key.Digit3)) HumanClimax.Submit(2);
            else if (GameInput.Pressed(Key.Digit4)) HumanClimax.Submit(3);
            if (GameInput.Pressed(Key.H)) HumanClimax.UseHint();
        }

        internal void PrepareHumanClimaxBoss()
        {
            PrepareHumanVillage();
            ambientPlayed = true;
            player.transform.position = new Vector3(-1, PlayerMotor.GroundY, 0);
            enemy.ResetEnemy(HumanClimax.Definition.bossHealth, HumanClimax.Definition.attackWindup);
            courseView.HideOpponentVisuals();
            humanView.SetClimaxMode();
            humanClimaxAttack = new HumanClimaxAttackPattern(HumanClimax.Definition);
        }

        internal void ShiftHumanClimaxPattern() => humanClimaxAttack.ChangePattern();

        void TickHumanClimaxAttack(float dt)
        {
            if (humanClimaxAttack == null || !humanClimaxAttack.Tick(dt)) return;
            bool low = humanClimaxAttack.LowSweep;
            float distance = Mathf.Abs(player.transform.position.x - enemy.transform.position.x);
            Effects.Slash(enemy.transform.position + new Vector3(0, low ? .3f : 1.35f), player.transform.position.x < enemy.transform.position.x ? -1 : 1, false);
            if (distance > HumanClimax.Definition.attackRange || player.Dodges(low) || player.Invulnerable) return;
            player.KnockBack(Mathf.Sign(player.transform.position.x - enemy.transform.position.x));
            DamagePlayer(HumanClimax.AttackDamage, false);
        }

        internal void ResolveHumanClimaxRune(bool sealBroken)
        {
            var data = HumanClimax.Definition;
            int damage = data.runeDamage + (sealBroken ? data.sealBreakDamage : 0);
            Effects.Charge(player.transform.position + Vector3.up);
            if (damage > 0 && enemy.Phase != EnemyPhase.Defeated) Effects.Impact(enemy.transform.position + Vector3.up, true, hitVisual: EnemyHitVisual);
            enemy.ReceiveDamage(damage, enemy.transform.position.x >= player.transform.position.x ? 1 : -1, true);
            UI.FloatText(enemy.transform.position + Vector3.up * 1.5f, damage.ToString(), true);
            Health = Mathf.Min(definition.playerHealth, Health + data.correctHeal);
            Notify(sealBroken ? "세 흐름이 이어졌다. 핵을 드러내 추가 타격!" : "흐름 하나를 풀어냈다.");
        }

        internal void RecordHumanClimaxVictory()
        {
            PrepareHumanVillage();
            WriteCheckpoint();
            SetMode(TrainingMode.HumanClimaxVictory);
        }
    }
}
