using MathRPG.Core;
using UnityEngine;

namespace MathRPG.Combat
{
    /// <summary>
    /// AttackTimeline이 타이밍의 각 순간마다 발행하는 이벤트들.
    /// Attacker를 들고 있는 이유: EventBus는 전역이라, 플레이어/적이 동시에 공격할 때
    /// 구독자(이펙트, 카메라 흔들림 등)가 "누구의 공격인지" 걸러낼 수 있어야 한다.
    /// </summary>
    public readonly struct AttackWindupStartedEvent : IGameEvent
    {
        public readonly GameObject Attacker;

        /// <summary>
        /// 이 공격 때문에 다음 입력이 막히는 총 시간 (초) = 윈드업 + 액티브 + 후딜.
        /// 공격 쿨다운 바가 이 값으로 길이를 잡는다. 히트스톱으로 실제 경과가 늘어나도
        /// 바와 타임라인 둘 다 스케일된 시간을 쓰므로 어긋나지 않는다.
        /// </summary>
        public readonly float LockSeconds;

        public AttackWindupStartedEvent(GameObject attacker, float lockSeconds)
        {
            Attacker = attacker;
            LockSeconds = lockSeconds;
        }
    }

    public readonly struct AttackHitEvent : IGameEvent
    {
        public readonly GameObject Attacker;
        public AttackHitEvent(GameObject attacker) => Attacker = attacker;
    }

    public readonly struct AttackEffectSpawnEvent : IGameEvent
    {
        public readonly GameObject Attacker;
        public AttackEffectSpawnEvent(GameObject attacker) => Attacker = attacker;
    }

    public readonly struct AttackRecoveryEndedEvent : IGameEvent
    {
        public readonly GameObject Attacker;
        public AttackRecoveryEndedEvent(GameObject attacker) => Attacker = attacker;
    }
}
