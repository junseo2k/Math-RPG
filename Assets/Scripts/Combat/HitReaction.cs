using System.Collections;
using MathRPG.Core;
using UnityEngine;

namespace MathRPG.Combat
{
    /// <summary>
    /// "맞은 쪽"의 타격감 중 <b>그림에만</b> 해당하는 부분. DamageDealtEvent를 구독해서
    /// 스프라이트 플래시 · 스쿼시로 반응한다. AttackVisuals의 피격자 버전이라고 보면 된다.
    ///
    /// 몸이 실제로 밀려나는 넉백은 여기가 아니라 <see cref="KnockbackReceiver"/>가 맡는다.
    /// 예전에는 이 클래스가 Visual의 로컬 오프셋을 밀었다 되돌리는 "가짜 넉백"을 했는데,
    /// 그림만 흔들리고 몸은 제자리라서 때려도 상대가 밀려나지 않았다. 지금 남은
    /// visualRecoil은 그 진짜 넉백 위에 얹는 선택적 움찔거림이며 기본값은 0이다.
    ///
    /// 히트스톱(시간 정지)은 여기서 하지 않는다 — AttackTimeline이 HitStop에 요청한다.
    /// 카메라 흔들림도 여기 없다 — CombatCamera가 같은 이벤트를 따로 구독한다.
    ///
    /// 움직임은 전부 Visual 트랜스폼의 로컬 오프셋이다 — 루트 콜라이더/물리를 건드리지 않아
    /// 이동 충돌과 섞이지 않는다.
    ///
    /// 붙이는 위치: SpriteRenderer가 있는 오브젝트 (보통 적 루트의 자식 "Visual").
    ///
    /// ※ 여기 수치는 전부 타격감 튜닝용 임시값이다.
    /// </summary>
    public sealed class HitReaction : MonoBehaviour
    {
        [Header("연결")]
        [SerializeField, Tooltip("플래시할 대상. 비우면 자신/자식에서 찾는다.")]
        private SpriteRenderer target;

        [SerializeField, Tooltip("이 반응이 거를 기준 오브젝트. 비우면 부모 Hurtbox의 Owner, 없으면 루트.")]
        private GameObject owner;

        [Header("플래시")]
        [SerializeField, Tooltip("피격 순간 물들일 색.")]
        private Color flashColor = Color.white;

        [SerializeField, Min(0f), Tooltip("원래 색으로 돌아오는 시간 (초).")]
        private float flashSeconds = 0.08f;

        [Header("움찔거림")]
        [SerializeField, Tooltip("그림만 맞은 방향으로 밀렸다 돌아오는 거리 (units). " +
                                 "몸이 실제로 밀리는 것은 KnockbackReceiver가 하므로 기본값은 0 — " +
                                 "둘 다 크게 주면 이중으로 겹쳐 떨리는 것처럼 보인다.")]
        private float visualRecoil = 0f;

        [SerializeField, Min(0.01f), Tooltip("플래시 · 스쿼시가 원래대로 돌아오는 데 걸리는 시간 (초).")]
        private float reactionSeconds = 0.16f;

        [SerializeField, Tooltip("피격 순간 눌리는 정도. (x 배수, y 배수) — 1이면 변화 없음.")]
        private Vector2 squash = new Vector2(1.18f, 0.82f);

        [SerializeField, Tooltip("타격의 시간별 세기. 1에서 시작해 0으로.")]
        private AnimationCurve reactionCurve = new AnimationCurve(
            new Keyframe(0f, 1f, 0f, -2f),
            new Keyframe(1f, 0f, -2f, 0f));

        [Header("무적 깜빡임")]
        [SerializeField, Min(0f), Tooltip("무적인 동안 깜빡이는 주기 (초). 0이면 깜빡이지 않는다.")]
        private float blinkInterval = 0.09f;

        [SerializeField, Range(0f, 1f), Tooltip("깜빡임이 '꺼진' 순간의 불투명도. 0이면 완전히 사라진다.")]
        private float blinkAlpha = 0.3f;

        [Header("사망")]
        [SerializeField, Tooltip("체력이 0이 되면 이 색으로 어둡게. 알파 0이면 표시 변화 없음.")]
        private Color deadTint = new Color(0.35f, 0.35f, 0.4f, 1f);

        private Transform _visual;
        private Vector3 _basePosition;
        private Vector3 _baseScale;
        private Color _baseColor;
        private Coroutine _routine;
        private bool _isDead;
        private Health _health;
        private bool _blinkApplied;

        private void Awake()
        {
            if (target == null)
            {
                target = GetComponentInChildren<SpriteRenderer>();
            }

            if (target == null)
            {
                Debug.LogError($"[{nameof(HitReaction)}] SpriteRenderer를 찾지 못했습니다.", this);
                enabled = false;
                return;
            }

            if (owner == null)
            {
                var hurtbox = GetComponentInParent<Hurtbox>();
                owner = hurtbox != null && hurtbox.Owner != null ? hurtbox.Owner : transform.root.gameObject;
            }

            // 무적 여부는 Health가 안다. 없으면(무적을 안 쓰는 대상) 깜빡임도 그냥 꺼진다.
            _health = owner != null ? owner.GetComponent<Health>() : null;

            _visual = target.transform;
            _basePosition = _visual.localPosition;
            _baseScale = _visual.localScale;
            _baseColor = target.color;
        }

        private void OnEnable()
        {
            EventBus.Subscribe<DamageDealtEvent>(OnDamageDealt);
            EventBus.Subscribe<CharacterDiedEvent>(OnDied);
            EventBus.Subscribe<HealthChangedEvent>(OnHealthChanged);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<DamageDealtEvent>(OnDamageDealt);
            EventBus.Unsubscribe<CharacterDiedEvent>(OnDied);
            EventBus.Unsubscribe<HealthChangedEvent>(OnHealthChanged);

            _routine = null;
            _blinkApplied = false;
            RestorePose();
        }

        private bool IsMine(GameObject victim) => victim == owner;

        private void OnDamageDealt(DamageDealtEvent e)
        {
            if (!IsMine(e.Victim) || _isDead)
            {
                return;
            }

            if (_routine != null)
            {
                StopCoroutine(_routine);
            }

            _routine = StartCoroutine(ReactRoutine(e.HitDirection));
        }

        private void OnDied(CharacterDiedEvent e)
        {
            if (!IsMine(e.Victim))
            {
                return;
            }

            _isDead = true;
            if (deadTint.a > 0f)
            {
                target.color = deadTint;
            }
        }

        private void OnHealthChanged(HealthChangedEvent e)
        {
            if (!IsMine(e.Owner))
            {
                return;
            }

            // 리스폰(체력이 최대로 복구)되면 사망 표시를 해제한다.
            if (_isDead && e.Current >= e.Max)
            {
                _isDead = false;
                StopActiveRoutine();
                RestorePose();
            }
        }

        private IEnumerator ReactRoutine(int hitDirection)
        {
            int dir = hitDirection < 0 ? -1 : 1;
            bool useFlash = flashColor.a > 0f;
            if (useFlash)
            {
                target.color = flashColor;
            }

            float elapsed = 0f;
            float flashElapsed = 0f;

            while (elapsed < reactionSeconds)
            {
                // Time.deltaTime(스케일 적용) — 히트스톱 동안 이 트윈도 같이 얼려야
                // "탁 멈췄다"가 산다.
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / reactionSeconds);
                float amount = reactionCurve.Evaluate(t);

                _visual.localPosition = _basePosition + new Vector3(visualRecoil * dir * amount, 0f, 0f);
                _visual.localScale = new Vector3(
                    _baseScale.x * Mathf.LerpUnclamped(1f, squash.x, amount),
                    _baseScale.y * Mathf.LerpUnclamped(1f, squash.y, amount),
                    _baseScale.z);

                if (useFlash)
                {
                    flashElapsed += Time.deltaTime;
                    float f = flashSeconds > 0f ? Mathf.Clamp01(flashElapsed / flashSeconds) : 1f;
                    target.color = Color.Lerp(flashColor, CurrentRestColor(), f);
                }

                yield return null;
            }

            RestorePose();
            _routine = null;
        }

        /// <summary>
        /// 무적 깜빡임. LateUpdate에서 <b>알파만</b> 건드린다 —
        /// RGB는 피격 플래시(ReactRoutine)와 사망 틴트가 쓰고 있어서, 같은 채널을 두 곳에서
        /// 쓰면 서로 덮어써 깜빡임이 씹히거나 플래시가 사라진다. 채널을 나눠 쓰면 둘이 겹쳐도
        /// 각자 제 역할을 한다. LateUpdate인 것도 같은 이유 — 코루틴이 색을 쓴 뒤에 얹어야 한다.
        /// </summary>
        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            bool blink = blinkInterval > 0f && _health != null && _health.IsInvulnerable && !_isDead;

            if (!blink)
            {
                // 무적이 끝난 프레임에 한 번만 되돌린다. 매 프레임 쓰면 다른 알파 연출을 막는다.
                if (_blinkApplied)
                {
                    SetAlpha(CurrentRestColor().a);
                    _blinkApplied = false;
                }

                return;
            }

            // 주기의 앞 절반은 켜짐, 뒷 절반은 흐려짐.
            bool visiblePhase = Mathf.Repeat(Time.time, blinkInterval * 2f) < blinkInterval;
            SetAlpha(visiblePhase ? CurrentRestColor().a : CurrentRestColor().a * blinkAlpha);
            _blinkApplied = true;
        }

        private void SetAlpha(float alpha)
        {
            Color c = target.color;
            c.a = alpha;
            target.color = c;
        }

        private Color CurrentRestColor() => _isDead && deadTint.a > 0f ? deadTint : _baseColor;

        private void StopActiveRoutine()
        {
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }
        }

        private void RestorePose()
        {
            if (target == null)
            {
                return;
            }

            _visual.localPosition = _basePosition;
            _visual.localScale = _baseScale;
            target.color = CurrentRestColor();
        }
    }
}
