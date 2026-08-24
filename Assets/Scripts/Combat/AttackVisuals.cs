using System.Collections;
using MathRPG.Core;
using UnityEngine;

namespace MathRPG.Combat
{
    /// <summary>
    /// AttackTimeline이 발행하는 이벤트를 구독해서 "보이는 것"만 담당한다.
    /// 스프라이트 교체 · 찌르기(러지) · 스케일 펀치 · 히트 플래시 · 이펙트 생성.
    ///
    /// 타이밍 계산은 전혀 하지 않는다 — 언제 무슨 일이 일어나는지는 AttackTimeline과
    /// AttackTimingData가 정하고, 이 클래스는 통보받은 순간에 반응만 한다.
    /// 나중에 Animator로 갈아탈 때 이 파일만 통째로 교체하면 되고,
    /// 전투 로직 · 타이밍 데이터는 손댈 필요가 없다.
    ///
    /// 붙이는 위치: SpriteRenderer가 있는 오브젝트 (보통 플레이어 루트의 자식 "Visual").
    /// AttackTimeline은 루트에 그대로 두고, 이 컴포넌트가 부모를 타고 올라가 찾는다.
    ///
    /// ※ 여기 수치는 기획서 7장 미정 수치가 아니라 타격감 튜닝용 임시값이다.
    ///   러프 테스트 단계에서 실제로 눌러보며 조정할 것.
    /// </summary>
    public sealed class AttackVisuals : MonoBehaviour
    {
        [Header("연결")]
        [SerializeField, Tooltip("스프라이트를 바꿀 대상. 비우면 자기 자신/자식에서 찾는다.")]
        private SpriteRenderer target;

        [SerializeField, Tooltip("이 비주얼이 반응할 공격자. 비우면 부모의 AttackTimeline을 찾는다.")]
        private GameObject attacker;

        [Header("스프라이트 (비우면 그 단계에서 교체하지 않음)")]
        [SerializeField, Tooltip("평상시 그림. 비우면 시작할 때 붙어 있던 스프라이트를 자동으로 기억한다.")]
        private Sprite idleSprite;

        [SerializeField, Tooltip("선딜(윈드업) 중 그림 — 칼 치켜든 자세.")]
        private Sprite windupSprite;

        [SerializeField, Tooltip("타격 순간 그림 — 휘두른 자세. 히트스톱 동안 이 그림이 멈춰 보인다.")]
        private Sprite hitSprite;

        [Header("윈드업 트윈")]
        [SerializeField, Tooltip("선딜 중 뒤로 빼는 거리 (units). 0이면 움직이지 않는다.")]
        private float windupPullback = 0.12f;

        [SerializeField, Min(0f), Tooltip("뒤로 다 빠지는 데 걸리는 시간 (초).")]
        private float windupTweenSeconds = 0.1f;

        [Header("히트 트윈")]
        [SerializeField, Tooltip("타격 순간 앞으로 찔러 나가는 거리 (units).")]
        private float hitLunge = 0.25f;

        [SerializeField, Tooltip("타격 순간 부풀리는 스케일 배수. 1이면 스케일 변화 없음.")]
        private float hitPunchScale = 1.15f;

        [SerializeField, Min(0f), Tooltip("러지 · 펀치가 원위치로 돌아오는 데 걸리는 시간 (초).")]
        private float hitTweenSeconds = 0.18f;

        [SerializeField, Tooltip("타격 세기의 시간별 모양. 1에서 시작해 0으로 떨어질수록 '탁' 하고 꽂힌다.")]
        private AnimationCurve hitCurve = new AnimationCurve(
            new Keyframe(0f, 1f, 0f, -1.5f),
            new Keyframe(1f, 0f, -1.5f, 0f));

        [Header("히트 플래시")]
        [SerializeField, Tooltip("타격 순간 스프라이트를 물들일 색. 알파를 0으로 두면 플래시 없음.")]
        private Color flashColor = Color.white;

        [SerializeField, Min(0f), Tooltip("원래 색으로 돌아오는 시간 (초).")]
        private float flashSeconds = 0.06f;

        [Header("이펙트")]
        [SerializeField, Tooltip("AttackEffectSpawnEvent 때 생성할 프리팹. 비우면 아무것도 생기지 않는다.")]
        private GameObject effectPrefab;

        [SerializeField, Tooltip("이펙트가 생길 위치. 비우면 이 오브젝트 위치.")]
        private Transform effectAnchor;

        [SerializeField, Min(0f), Tooltip("생성된 이펙트를 자동 파괴하기까지의 시간 (초). 0이면 파괴하지 않는다.")]
        private float effectLifetime = 1f;

        private Transform _visual;
        private Vector3 _basePosition;
        private Vector3 _baseScale;
        private Color _baseColor;
        private Coroutine _tween;

        private void Awake()
        {
            if (target == null)
            {
                target = GetComponentInChildren<SpriteRenderer>();
            }

            if (target == null)
            {
                Debug.LogError($"[{nameof(AttackVisuals)}] SpriteRenderer를 찾지 못했습니다. 인스펙터에서 지정하세요.", this);
                enabled = false;
                return;
            }

            if (attacker == null)
            {
                var timeline = GetComponentInParent<AttackTimeline>();
                attacker = timeline != null ? timeline.gameObject : transform.root.gameObject;
            }

            _visual = target.transform;
            _basePosition = _visual.localPosition;
            _baseScale = _visual.localScale;
            _baseColor = target.color;

            if (idleSprite == null)
            {
                idleSprite = target.sprite;
            }
        }

        // EventBus는 static이라 구독이 씬 로드를 넘어 살아남는다.
        // OnEnable / OnDisable 짝을 반드시 맞춰야, 파괴된 오브젝트의 핸들러가 호출되는 사고가 안 난다.
        private void OnEnable()
        {
            EventBus.Subscribe<AttackWindupStartedEvent>(OnWindupStarted);
            EventBus.Subscribe<AttackHitEvent>(OnHit);
            EventBus.Subscribe<AttackEffectSpawnEvent>(OnEffectSpawn);
            EventBus.Subscribe<AttackRecoveryEndedEvent>(OnRecoveryEnded);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<AttackWindupStartedEvent>(OnWindupStarted);
            EventBus.Unsubscribe<AttackHitEvent>(OnHit);
            EventBus.Unsubscribe<AttackEffectSpawnEvent>(OnEffectSpawn);
            EventBus.Unsubscribe<AttackRecoveryEndedEvent>(OnRecoveryEnded);

            // 트윈 도중 꺼지면 스프라이트가 튀어나간 자세로 굳는다. 원위치로 돌려놓고 나간다.
            _tween = null;
            ResetPose();
        }

        // EventBus는 전역이라 플레이어 · 적의 공격 이벤트가 전부 여기로 들어온다.
        // 이 필터가 없으면 적이 때릴 때 내 그림이 같이 움직인다.
        private bool IsMine(GameObject eventAttacker) => eventAttacker == attacker;

        private void OnWindupStarted(AttackWindupStartedEvent e)
        {
            if (!IsMine(e.Attacker))
            {
                return;
            }

            SetSprite(windupSprite);
            StartTween(WindupRoutine());
        }

        private void OnHit(AttackHitEvent e)
        {
            if (!IsMine(e.Attacker))
            {
                return;
            }

            SetSprite(hitSprite);
            StartTween(HitRoutine());
        }

        private void OnEffectSpawn(AttackEffectSpawnEvent e)
        {
            if (!IsMine(e.Attacker) || effectPrefab == null)
            {
                return;
            }

            Transform anchor = effectAnchor != null ? effectAnchor : transform;
            GameObject spawned = Instantiate(effectPrefab, anchor.position, anchor.rotation);

            if (effectLifetime > 0f)
            {
                Destroy(spawned, effectLifetime);
            }
        }

        private void OnRecoveryEnded(AttackRecoveryEndedEvent e)
        {
            if (!IsMine(e.Attacker))
            {
                return;
            }

            SetSprite(idleSprite);
            StopTween();
            ResetPose();
        }

        private void SetSprite(Sprite sprite)
        {
            // 비어 있는 슬롯은 "이 단계에선 그림을 바꾸지 않는다"는 뜻.
            // 그림이 아직 한 장뿐이어도 트윈만으로 동작을 확인할 수 있게 하기 위한 것.
            if (sprite != null)
            {
                target.sprite = sprite;
            }
        }

        // 새 단계가 시작되면 이전 트윈은 버린다. 이게 없으면 연타 시 두 코루틴이
        // 같은 localPosition을 동시에 써서 그림이 떨린다.
        private void StartTween(IEnumerator routine)
        {
            StopTween();
            _tween = StartCoroutine(routine);
        }

        private void StopTween()
        {
            if (_tween != null)
            {
                StopCoroutine(_tween);
                _tween = null;
            }
        }

        private void ResetPose()
        {
            if (target == null)
            {
                return;
            }

            _visual.localPosition = _basePosition;
            _visual.localScale = _baseScale;
            target.color = _baseColor;
        }

        private IEnumerator WindupRoutine()
        {
            float elapsed = 0f;
            while (elapsed < windupTweenSeconds)
            {
                // Time.deltaTime(스케일 적용)을 쓴다 — 히트스톱으로 timeScale이 0이 되면
                // 이 트윈도 같이 얼어야 "게임 전체가 멈춘" 느낌이 난다.
                // unscaledDeltaTime을 쓰면 멈춘 화면에서 그림만 혼자 움직여 효과가 죽는다.
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / windupTweenSeconds);
                float eased = Mathf.SmoothStep(0f, 1f, t);

                // 로컬 X 오프셋이라, 부모를 localScale.x = -1로 뒤집으면 방향도 같이 뒤집힌다.
                _visual.localPosition = _basePosition + new Vector3(-windupPullback * eased, 0f, 0f);
                yield return null;
            }

            _tween = null;
        }

        private IEnumerator HitRoutine()
        {
            bool useFlash = flashColor.a > 0f;
            if (useFlash)
            {
                target.color = flashColor;
            }

            float flashElapsed = 0f;
            float elapsed = 0f;

            while (elapsed < hitTweenSeconds)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / hitTweenSeconds);
                float amount = hitCurve.Evaluate(t);

                _visual.localPosition = _basePosition + new Vector3(hitLunge * amount, 0f, 0f);
                _visual.localScale = _baseScale * Mathf.LerpUnclamped(1f, hitPunchScale, amount);

                if (useFlash)
                {
                    // 플래시는 트윈보다 짧게 끝나므로 별도 타이머로 돌린다.
                    flashElapsed += Time.deltaTime;
                    float f = flashSeconds > 0f ? Mathf.Clamp01(flashElapsed / flashSeconds) : 1f;
                    target.color = Color.Lerp(flashColor, _baseColor, f);
                }

                yield return null;
            }

            ResetPose();
            _tween = null;
        }
    }
}
