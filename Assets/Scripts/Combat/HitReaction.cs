using System.Collections;
using MathRPG.Core;
using UnityEngine;

namespace MathRPG.Combat
{
    /// <summary>
    /// "맞은 쪽"의 타격감. DamageDealtEvent를 구독해서 스프라이트 플래시 · 넉백 펀치 ·
    /// 스쿼시로 반응한다. AttackVisuals의 피격자 버전이라고 보면 된다.
    ///
    /// 히트스톱(시간 정지)은 여기서 하지 않는다 — AttackTimeline이 전역으로 이미 건다.
    /// 카메라 흔들림도 여기 없다 (이번 M1 단계 범위에서 제외).
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

        [Header("넉백 펀치")]
        [SerializeField, Tooltip("맞은 방향으로 밀리는 거리 (units).")]
        private float knockbackDistance = 0.35f;

        [SerializeField, Min(0.01f), Tooltip("밀렸다가 제자리로 돌아오는 시간 (초).")]
        private float knockbackSeconds = 0.16f;

        [SerializeField, Tooltip("피격 순간 눌리는 정도. (x 배수, y 배수) — 1이면 변화 없음.")]
        private Vector2 squash = new Vector2(1.18f, 0.82f);

        [SerializeField, Tooltip("타격의 시간별 세기. 1에서 시작해 0으로.")]
        private AnimationCurve reactionCurve = new AnimationCurve(
            new Keyframe(0f, 1f, 0f, -2f),
            new Keyframe(1f, 0f, -2f, 0f));

        [Header("사망")]
        [SerializeField, Tooltip("체력이 0이 되면 이 색으로 어둡게. 알파 0이면 표시 변화 없음.")]
        private Color deadTint = new Color(0.35f, 0.35f, 0.4f, 1f);

        private Transform _visual;
        private Vector3 _basePosition;
        private Vector3 _baseScale;
        private Color _baseColor;
        private Coroutine _routine;
        private bool _isDead;

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

            while (elapsed < knockbackSeconds)
            {
                // Time.deltaTime(스케일 적용) — 히트스톱 동안 이 트윈도 같이 얼려야
                // "탁 멈췄다"가 산다.
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / knockbackSeconds);
                float amount = reactionCurve.Evaluate(t);

                _visual.localPosition = _basePosition + new Vector3(knockbackDistance * dir * amount, 0f, 0f);
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
