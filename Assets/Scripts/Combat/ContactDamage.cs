using UnityEngine;

namespace MathRPG.Combat
{
    /// <summary>
    /// 몸에 닿기만 해도 들어가는 피해. 몬스터에 붙여 "부딪히면 아프다"를 만든다.
    ///
    /// Hitbox(공격 판정)와는 역할이 다르다 — Hitbox는 <b>휘두른 순간</b> 한 번 검사하는
    /// 능동 공격이고, 이쪽은 <b>닿아 있는 동안</b> 주기적으로 들어가는 수동 피해다.
    /// 그래서 타이밍 데이터(AttackTimeline)와 무관하게 독립적으로 동작한다.
    ///
    /// 판정에 OverlapBox가 아니라 충돌 콜백을 쓰는 이유: 플레이어와 몬스터는 둘 다
    /// 트리거가 아닌 콜라이더라 물리적으로 서로를 밀어낸다. 즉 <b>맞닿아 있을 뿐 겹치지는
    /// 않으므로</b> 겹침 질의로는 잡히다 말다 한다. 접촉을 정확히 아는 것은 물리 엔진이다.
    ///
    /// 피해는 기존 파이프라인(Hurtbox → Health)을 그대로 탄다. 그래서 넉백 · 카메라 흔들림 ·
    /// 데미지 숫자가 자동으로 따라온다 — 여기서 따로 연출을 부르지 않는다.
    /// 다만 히트스톱은 걸리지 않는다 (AttackTimeline만 요청한다). 스쳐서 들어가는
    /// 잔피해까지 화면을 멈추면 이동이 계속 끊겨 답답해지기 때문이다.
    ///
    /// 붙이는 위치: 몬스터 루트 (Collider2D가 있는 곳).
    ///
    /// ※ damage · interval은 기획서 7장 미정 수치가 아니라 M1 타격감 튜닝용 임시값이다.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class ContactDamage : MonoBehaviour
    {
        [Header("연결")]
        [SerializeField, Tooltip("피해를 준 주체로 기록될 오브젝트. 비우면 이 오브젝트.")]
        private GameObject source;

        [SerializeField, Tooltip("이 체력이 0이면 접촉 피해를 주지 않는다(시체는 안 아프다). 비우면 찾는다.")]
        private Health selfHealth;

        [Header("피해 (임시값)")]
        [SerializeField, Min(0f), Tooltip("한 번 닿을 때 주는 피해량. 평타(12)보다 작게 두는 것이 자연스럽다.")]
        private float damage = 6f;

        [SerializeField, Min(0.05f), Tooltip("계속 닿아 있을 때 다음 피해까지의 간격 (초). " +
                                             "짧으면 몬스터에 밀착만 해도 순식간에 죽는다.")]
        private float interval = 0.8f;

        [SerializeField, Tooltip("이 레이어에 있는 대상만 접촉 피해를 입는다.")]
        private LayerMask hittableLayers;

        private float _cooldown;

        private void Awake()
        {
            if (source == null)
            {
                source = gameObject;
            }

            if (selfHealth == null)
            {
                selfHealth = GetComponent<Health>();
            }
        }

        private void OnEnable()
        {
            // 리스폰 직후 밀착 상태였다면 부활하자마자 때리게 된다. 한 박자 쉬고 시작한다.
            _cooldown = interval;
        }

        private void Update()
        {
            // Time.deltaTime(스케일 적용) — 히트스톱으로 멈춘 동안은 쿨다운도 멈춘다.
            if (_cooldown > 0f)
            {
                _cooldown = Mathf.Max(0f, _cooldown - Time.deltaTime);
            }
        }

        // Enter와 Stay를 둘 다 받는다. Stay만 쓰면 닿은 첫 프레임을 놓쳐 한 박자 늦게 아프다.
        private void OnCollisionEnter2D(Collision2D collision) => TryDamage(collision.collider);

        private void OnCollisionStay2D(Collision2D collision) => TryDamage(collision.collider);

        private void TryDamage(Collider2D other)
        {
            if (_cooldown > 0f || other == null)
            {
                return;
            }

            if (selfHealth != null && selfHealth.IsDead)
            {
                return;
            }

            if ((hittableLayers.value & (1 << other.gameObject.layer)) == 0)
            {
                return;
            }

            var hurtbox = other.GetComponentInParent<Hurtbox>();
            if (hurtbox == null || hurtbox.IsDead)
            {
                return;
            }

            // 밀려나는 방향은 "나로부터 멀어지는 쪽". 이게 반대면 맞은 쪽이 몬스터에게
            // 빨려 들어가서 접촉 피해가 연달아 터진다.
            Vector2 self = transform.position;
            Vector2 victim = other.transform.position;
            int direction = victim.x >= self.x ? 1 : -1;

            hurtbox.Receive(new DamageInfo(source, damage, other.ClosestPoint(self), direction));
            _cooldown = interval;
        }
    }
}
