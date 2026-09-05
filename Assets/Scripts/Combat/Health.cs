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
    /// <b>무적 프레임도 여기서 처리한다.</b> 피해가 들어오는 통로가 ApplyDamage 하나뿐이라
    /// (평타 Hitbox든 접촉 피해든 전부 Hurtbox를 거쳐 여기로 온다) 한 군데만 막으면 되고,
    /// 새 피해원이 생겨도 자동으로 무적이 적용된다. 각 공격 쪽에 따로 넣으면 빠뜨리기 쉽다.
    ///
    /// ※ maxHealth는 기획서 7장 미정 수치가 아니라 M1 타격감 검증용 임시값이다.
    /// </summary>
    public sealed class Health : MonoBehaviour, IDamageable
    {
        [SerializeField, Min(1f), Tooltip("최대 체력. 타격감 검증용 임시값.")]
        private float maxHealth = 100f;

        [SerializeField, Tooltip("때린 주체와 이 오브젝트가 같으면 무시한다 (자해 방지).")]
        private bool ignoreSelfDamage = true;

        [SerializeField, Min(0f), Tooltip("피해를 받은 뒤 무적이 유지되는 시간 (초). 0이면 무적 없음. " +
                                          "몬스터에는 보통 0을 준다 — 여기에 값을 주면 플레이어의 연타가 씹힌다.")]
        private float invulnerableSeconds = 0f;

        public float Max => maxHealth;
        public float Current { get; private set; }
        public bool IsDead => Current <= 0f;

        /// <summary>지금 무적인가. 피격 연출(깜빡임)이 참조한다.</summary>
        public bool IsInvulnerable => invulnerableSeconds > 0f && Time.time < _invulnerableUntil;

        /// <summary>무적이 끝나기까지 남은 시간 (초). 무적이 아니면 0.</summary>
        public float InvulnerableRemaining => Mathf.Max(0f, _invulnerableUntil - Time.time);

        // 스케일된 Time.time을 쓴다 — 히트스톱으로 화면이 멈춘 동안은 무적 시간도 멈춰야
        // 실제 체감 시간과 어긋나지 않는다 (프로젝트의 다른 타이머들과 같은 기준).
        private float _invulnerableUntil;

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

            // 무적 중에는 아무 일도 없었던 것으로 한다 — 이벤트도 발행하지 않는다.
            // 여기서 이벤트를 내보내면 피해는 0인데 넉백 · 카메라 흔들림 · 데미지 숫자만
            // 계속 터져서, 무적인데도 계속 맞는 것처럼 보인다.
            if (IsInvulnerable)
            {
                return;
            }

            if (invulnerableSeconds > 0f)
            {
                _invulnerableUntil = Time.time + invulnerableSeconds;
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
            // 리스폰·회복으로 상태를 되돌릴 때 이전 무적도 함께 푼다.
            // 남겨두면 되살아난 직후 잠깐 무적인 채로 시작해 판정이 헷갈린다.
            _invulnerableUntil = 0f;

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
