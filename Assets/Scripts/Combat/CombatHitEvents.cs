using MathRPG.Core;
using UnityEngine;

namespace MathRPG.Combat
{
    /// <summary>
    /// 피격이 실제로 성사됐을 때 발행된다 (Hitbox → Hurtbox 전달 성공).
    /// 타격감 연출(HitReaction), 카메라, 데미지 표시 등이 구독한다.
    /// AttackHitEvent와는 다르다 — 저건 "휘둘렀다", 이건 "맞았다".
    /// </summary>
    public readonly struct DamageDealtEvent : IGameEvent
    {
        public readonly GameObject Source;
        public readonly GameObject Victim;
        public readonly float Amount;
        public readonly Vector2 HitPoint;
        public readonly int HitDirection;

        public DamageDealtEvent(GameObject source, GameObject victim, float amount, Vector2 hitPoint, int hitDirection)
        {
            Source = source;
            Victim = victim;
            Amount = amount;
            HitPoint = hitPoint;
            HitDirection = hitDirection;
        }
    }

    /// <summary>대상의 체력이 0이 됐을 때 발행된다.</summary>
    public readonly struct CharacterDiedEvent : IGameEvent
    {
        public readonly GameObject Victim;
        public CharacterDiedEvent(GameObject victim) => Victim = victim;
    }

    /// <summary>체력이 바뀔 때마다 발행된다 (피격·회복·리스폰 포함). UI 체력바 등이 구독.</summary>
    public readonly struct HealthChangedEvent : IGameEvent
    {
        public readonly GameObject Owner;
        public readonly float Current;
        public readonly float Max;

        public HealthChangedEvent(GameObject owner, float current, float max)
        {
            Owner = owner;
            Current = current;
            Max = max;
        }
    }
}
