using UnityEngine;

namespace MathRPG.Combat
{
    /// <summary>
    /// 한 번의 타격에 담기는 정보. Hitbox가 만들어 Hurtbox로 넘긴다.
    ///
    /// ※ Amount(피해량)는 기획서 7장 미정 수치가 아니라 M1 타격감 검증용 임시값이다.
    ///   실제 밸런스(스킬 배율·챕터별 커브 등)는 M2 이후 SkillData에서 다룬다.
    /// </summary>
    public readonly struct DamageInfo
    {
        /// <summary>때린 주체 (플레이어/적의 루트 GameObject).</summary>
        public readonly GameObject Source;

        /// <summary>피해량.</summary>
        public readonly float Amount;

        /// <summary>월드 공간 타격 지점 — 이펙트·데미지 표시 위치로 쓴다.</summary>
        public readonly Vector2 HitPoint;

        /// <summary>때린 방향. +1이면 오른쪽으로 미는 타격, -1이면 왼쪽.</summary>
        public readonly int HitDirection;

        public DamageInfo(GameObject source, float amount, Vector2 hitPoint, int hitDirection)
        {
            Source = source;
            Amount = amount;
            HitPoint = hitPoint;
            HitDirection = hitDirection < 0 ? -1 : 1;
        }
    }

    /// <summary>피해를 받을 수 있는 대상. Hurtbox가 이 인터페이스를 통해 Health로 전달한다.</summary>
    public interface IDamageable
    {
        /// <summary>이미 죽었는가. 죽은 대상은 Hitbox가 다시 맞히지 않는다.</summary>
        bool IsDead { get; }

        void ApplyDamage(in DamageInfo info);
    }
}
