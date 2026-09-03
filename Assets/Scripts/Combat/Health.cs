using MathRPG.Core;
using UnityEngine;

namespace MathRPG.Combat
{
    /// <summary>
    /// 체력을 들고 있는 컴포넌트. 플레이어 · 적 공용.
    ///
    /// 피격 판정(Hurtbox)과 분리돼 있다 — Hurtbox는 "어디를 때리면 맞는가"(콜라이더)를,
    /// Health는 "맞으면 얼마나 닳는가"(수치)를 담당한다. 나중에 부위별 약점(head/body)이
    /// 생겨도 Hurtbox만 여러 개 두고 Health 하나를 가리키면 된다.
    ///
    /// ※ maxHealth는 기획서 7장 미정 수치가 아니라 M1 타격감 검증용 임시값이다.
    /// </summary>
    public sealed class Health : MonoBehaviour, IDamageable
    {
        [SerializeField, Min(1f), Tooltip("최대 체력. 타격감 검증용 임시값.")]
        private float maxHealth = 100f;

        [SerializeField, Tooltip("때린 주체와 이 오브젝트가 같으면 무시한다 (자해 방지).")]
        private bool ignoreSelfDamage = true;

        public float Max => maxHealth;
        public float Current { get; private set; }
        public bool IsDead => Current <= 0f;

        private void Awake()
        {
            Current = maxHealth;
        }

        private void OnEnable()
        {
            // 리스폰(오브젝트 재활성화) 시 체력바가 즉시 갱신되도록 한 번 알린다.
            Publish();
        }

        public void ApplyDamage(in DamageInfo info)
        {
            if (IsDead)
            {
                return;
            }

            if (ignoreSelfDamage && info.Source == gameObject)
            {
                return;
            }

            Current = Mathf.Max(0f, Current - Mathf.Abs(info.Amount));

            EventBus.Publish(new DamageDealtEvent(
                info.Source, gameObject, info.Amount, info.HitPoint, info.HitDirection));
            Publish();

            if (IsDead)
            {
                EventBus.Publish(new CharacterDiedEvent(gameObject));
            }
        }

        /// <summary>체력을 특정 값으로 되돌린다. 음수/초과는 범위 안으로 잘린다. (리스폰·회복)</summary>
        public void SetHealth(float value)
        {
            Current = Mathf.Clamp(value, 0f, maxHealth);
            Publish();
        }

        /// <summary>가득 채운다. 더미 적 리스폰에 쓴다.</summary>
        public void Revive() => SetHealth(maxHealth);

        private void Publish()
        {
            EventBus.Publish(new HealthChangedEvent(gameObject, Current, maxHealth));
        }
    }
}
