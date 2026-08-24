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
        public AttackWindupStartedEvent(GameObject attacker) => Attacker = attacker;
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
