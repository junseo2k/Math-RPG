using UnityEngine;

namespace MathRPG
{
    public enum EnemyPhase { Approach, Windup, Recovery, Defeated }

    public sealed class TrainingEnemy : MonoBehaviour
    {
        public Transform visual;
        public SpriteRenderer warning;
        public SpriteRenderer healthFill;
        public bool faceTarget;
        public EnemyPhase Phase { get; private set; }
        public bool LowSweep { get; private set; }
        public int Health { get; private set; }
        public int MaxHealth { get; private set; }
        public float TelegraphProgress => Phase == EnemyPhase.Windup ? 1 - timer / windup : 0;
        public int AttackSerial { get; private set; }
        float timer, windup, stagger;
        int pattern, facing = 1;
        bool[] lowAttackPattern;
        float recoveryDuration = 1.25f;
        Vector3 home;

        void Awake() { home = transform.position; }

        public void ResetEnemy(int hp, float windupSeconds, int patternOffset = 0)
        {
            if (home == Vector3.zero) home = transform.position;
            transform.position = home;
            Health = MaxHealth = hp;
            windup = windupSeconds;
            lowAttackPattern = null;
            recoveryDuration = 1.25f;
            Phase = EnemyPhase.Approach;
            timer = .7f;
            stagger = 0;
            pattern = patternOffset;
            AttackSerial = 0;
            gameObject.SetActive(true);
            RefreshVisuals();
        }

        // Returns true on the single active frame of an attack.
        public bool Tick(float dt, PlayerMotor player)
        {
            if (Phase == EnemyPhase.Defeated) return false;
            if (stagger > 0) { stagger -= dt; RefreshVisuals(); return false; }
            timer -= dt;
            float distance = Mathf.Abs(player.transform.position.x - transform.position.x);
            if (distance > .1f) facing = player.transform.position.x < transform.position.x ? -1 : 1;
            if (Phase == EnemyPhase.Approach)
            {
                if (distance > 2.1f)
                    transform.position += Vector3.right * (Mathf.Sign(player.transform.position.x - transform.position.x) * dt * 2.25f);
                if (distance <= 2.7f && timer <= 0)
                {
                    LowSweep = lowAttackPattern == null ? pattern % 2 == 0 : lowAttackPattern[pattern % lowAttackPattern.Length];
                    pattern++;
                    Phase = EnemyPhase.Windup;
                    timer = windup;
                }
            }
            else if (Phase == EnemyPhase.Windup && timer <= 0)
            {
                Phase = EnemyPhase.Recovery;
                timer = recoveryDuration;
                AttackSerial++;
                RefreshVisuals();
                return true;
            }
            else if (Phase == EnemyPhase.Recovery && timer <= 0)
            { Phase = EnemyPhase.Approach; timer = .25f; }
            RefreshVisuals();
            return false;
        }

        public bool ReceiveDamage(int amount, int direction, bool heavy)
        {
            if (Phase == EnemyPhase.Defeated) return false;
            Health = Mathf.Max(0, Health - Mathf.Max(0, amount));
            transform.position = new Vector3(Mathf.Clamp(transform.position.x + direction * (heavy ? .7f : .22f), -9.5f, 9.5f), PlayerMotor.GroundY, 0);
            stagger = heavy ? .4f : .08f;
            if (heavy) { Phase = EnemyPhase.Recovery; timer = .75f; }
            if (Health == 0) Phase = EnemyPhase.Defeated;
            RefreshVisuals();
            return Health == 0;
        }

        public void ConfigurePattern(bool[] lowAttacks, float windupSeconds, float recoverySeconds)
        {
            lowAttackPattern = lowAttacks != null && lowAttacks.Length > 0 ? lowAttacks : null;
            windup = Mathf.Max(.2f, windupSeconds);
            recoveryDuration = Mathf.Max(.1f, recoverySeconds);
            pattern = 0;
            if (Health > 0)
            {
                Phase = EnemyPhase.Recovery;
                timer = Mathf.Max(.9f, recoveryDuration);
                stagger = 0;
            }
            RefreshVisuals();
        }

        void RefreshVisuals()
        {
            if (visual != null)
            {
                visual.gameObject.SetActive(Health > 0);
                float squash = Phase == EnemyPhase.Windup ? 1 - TelegraphProgress * .18f : 1;
                visual.localScale = new Vector3((faceTarget ? facing : 1) / squash, squash, 1);
            }
            if (warning != null)
            {
                warning.enabled = Phase == EnemyPhase.Windup;
                warning.transform.localPosition = new Vector3(0, LowSweep ? .24f : 1.35f, 0);
                warning.transform.localScale = new Vector3(6.4f, LowSweep ? .2f : .26f, 1);
                warning.color = LowSweep ? new Color(1, .59f, .22f, .4f + TelegraphProgress * .5f) : new Color(1, .28f, .45f, .4f + TelegraphProgress * .5f);
            }
            if (healthFill != null)
            {
                healthFill.transform.parent.gameObject.SetActive(Health > 0);
                float fraction = MaxHealth <= 0 ? 0 : (float)Health / MaxHealth;
                healthFill.transform.localScale = new Vector3(1.6f * fraction, .065f, 1);
                healthFill.transform.localPosition = new Vector3(-.8f + .8f * fraction, 0, 0);
            }
        }
    }
}
