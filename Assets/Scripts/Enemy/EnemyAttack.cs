using MathRPG.Combat;
using MathRPG.Data;
using UnityEngine;

namespace MathRPG.Enemy
{
    /// <summary>
    /// 몬스터의 근접 공격 1종. 플레이어의 PlayerAttack과 대칭 — 입력 대신 EnemyAI가 호출한다.
    ///
    /// 공격 타이밍·히트 판정·연출은 전부 플레이어와 같은 시스템을 그대로 쓴다
    /// (AttackTimeline / Hitbox / AttackVisuals). 몬스터 전용으로 다른 건 타이밍 데이터뿐 —
    /// 윈드업을 길게 잡아 "전조 동작(텔레그래프)"으로 삼는다.
    /// </summary>
    [RequireComponent(typeof(AttackTimeline))]
    public sealed class EnemyAttack : MonoBehaviour
    {
        [SerializeField, Tooltip("몬스터 공격 타이밍. 윈드업이 곧 텔레그래프다.")]
        private AttackTimingData attackTiming;

        /// <summary>공격 모션이 재생 중인가 (윈드업~후딜). EnemyAI가 이동을 멈추는 데 쓴다.</summary>
        public bool IsAttacking => _timeline != null && _timeline.IsPlaying;

        private AttackTimeline _timeline;
        private Hitbox _hitbox;

        private void Awake()
        {
            _timeline = GetComponent<AttackTimeline>();
            _hitbox = GetComponent<Hitbox>();
        }

        /// <summary>공격을 시작한다. 이미 공격 중이거나 데이터가 없으면 false.</summary>
        public bool TryAttack(int facing)
        {
            if (attackTiming == null || _timeline.IsPlaying)
            {
                return false;
            }

            if (_hitbox != null)
            {
                _hitbox.Facing = facing;
            }

            _timeline.Play(attackTiming);
            return true;
        }
    }
}
