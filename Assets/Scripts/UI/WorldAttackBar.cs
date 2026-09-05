using MathRPG.Combat;
using MathRPG.Core;
using UnityEngine;

namespace MathRPG.UI
{
    /// <summary>
    /// 체력바 아래에 붙는 공격 쿨다운 바. <b>바가 떠 있는 동안은 다음 공격이 나가지 않는다.</b>
    ///
    /// 새로운 제약을 만드는 게 아니라 <b>이미 있던 제약을 보이게 만드는 것</b>이다 —
    /// AttackTimeline은 원래 재생 중(윈드업~후딜)에는 Play를 무시해서 연타를 막고 있었는데,
    /// 그게 화면에 전혀 드러나지 않아 "눌렀는데 안 나갔다"로만 느껴졌다. 이 바가 그 구간을
    /// 눈에 보이게 해주면 입력이 씹힌 게 아니라 아직 후딜이라는 걸 알 수 있다.
    ///
    /// 길이는 AttackWindupStartedEvent가 실어 보내는 LockSeconds를 쓴다 — 공격마다 타이밍
    /// 데이터가 다를 수 있으므로(평타 / 큰 스킬) 바가 그 값을 직접 참조하지 않고 통보받는다.
    ///
    /// 시간은 Time.deltaTime(스케일 적용)으로 센다. AttackTimeline도 WaitForSeconds(스케일 적용)를
    /// 쓰므로 히트스톱으로 화면이 멈추면 바도 같이 멈춰 둘이 어긋나지 않는다.
    ///
    /// 붙이는 위치: 캐릭터의 자식 오브젝트. 렌더러를 껐다 켜는 방식이라 이 오브젝트 자체는
    /// 항상 활성 상태여야 한다 (비활성이면 Update가 돌지 않아 바가 영영 안 사라진다).
    ///
    /// ※ 여기 수치는 기획서 7장 미정 수치가 아니라 M1 검증용 표시 튜닝값이다.
    /// </summary>
    public sealed class WorldAttackBar : MonoBehaviour
    {
        [Header("연결")]
        [SerializeField, Tooltip("이 바가 표시할 공격자. 비우면 부모의 AttackTimeline을 찾는다.")]
        private GameObject owner;

        [SerializeField, Tooltip("줄어드는 막대. 왼쪽 끝을 기준으로 가로 길이가 변한다.")]
        private Transform fill;

        [SerializeField, Tooltip("막대 렌더러. 쉬는 동안 꺼둔다. 비우면 fill에서 찾는다.")]
        private SpriteRenderer fillRenderer;

        [SerializeField, Tooltip("막대 배경 렌더러. 쉬는 동안 함께 꺼둔다. 비워도 된다.")]
        private SpriteRenderer backRenderer;

        [Header("모양")]
        [SerializeField, Min(0.05f), Tooltip("쿨다운이 막 시작됐을 때 막대의 가로 길이 (units).")]
        private float width = 1.2f;

        [SerializeField, Tooltip("막대 색.")]
        private Color barColor = new Color(1f, 0.78f, 0.28f, 1f);

        /// <summary>지금 공격 쿨다운 중인가 (= 바가 떠 있는가).</summary>
        public bool IsCoolingDown => _remaining > 0f;

        private float _remaining;
        private float _duration;
        private float _fillHeight = 1f;

        private void Awake()
        {
            if (owner == null)
            {
                var timeline = GetComponentInParent<AttackTimeline>();
                owner = timeline != null ? timeline.gameObject : transform.root.gameObject;
            }

            if (fill == null)
            {
                Debug.LogError($"[{nameof(WorldAttackBar)}] fill이 비어 있습니다. 인스펙터에서 지정하세요.", this);
                enabled = false;
                return;
            }

            if (fillRenderer == null)
            {
                fillRenderer = fill.GetComponent<SpriteRenderer>();
            }

            _fillHeight = fill.localScale.y;
            SetVisible(false);
        }

        private void OnEnable()
        {
            EventBus.Subscribe<AttackWindupStartedEvent>(OnWindupStarted);
            EventBus.Subscribe<AttackRecoveryEndedEvent>(OnRecoveryEnded);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<AttackWindupStartedEvent>(OnWindupStarted);
            EventBus.Unsubscribe<AttackRecoveryEndedEvent>(OnRecoveryEnded);

            // 쿨다운 도중 꺼지면 바가 뜬 채로 굳는다.
            _remaining = 0f;
            SetVisible(false);
        }

        // EventBus는 전역이라 다른 캐릭터의 공격도 들어온다. 이 필터가 없으면
        // 몬스터가 휘두를 때 플레이어 바가 찬다.
        private void OnWindupStarted(AttackWindupStartedEvent e)
        {
            if (e.Attacker != owner || e.LockSeconds <= 0f)
            {
                return;
            }

            _duration = e.LockSeconds;
            _remaining = _duration;

            SetVisible(true);
            Apply();
        }

        // 타임라인이 끝났다고 알려주면 남은 시간과 무관하게 즉시 닫는다.
        // 바가 조금 일찍/늦게 비는 오차보다, 실제로 공격 가능한 시점과 어긋나는 쪽이 나쁘다.
        private void OnRecoveryEnded(AttackRecoveryEndedEvent e)
        {
            if (e.Attacker != owner)
            {
                return;
            }

            _remaining = 0f;
            SetVisible(false);
        }

        private void Update()
        {
            if (_remaining <= 0f)
            {
                return;
            }

            _remaining = Mathf.Max(0f, _remaining - Time.deltaTime);

            if (_remaining <= 0f)
            {
                SetVisible(false);
                return;
            }

            Apply();
        }

        private void Apply()
        {
            float ratio = _duration > 0f ? Mathf.Clamp01(_remaining / _duration) : 0f;

            // 왼쪽 끝을 고정한 채 줄어들게 — 체력바와 같은 방식.
            fill.localScale = new Vector3(width * ratio, _fillHeight, 1f);
            fill.localPosition = new Vector3(width * (ratio - 1f) * 0.5f, 0f, 0f);

            if (fillRenderer != null)
            {
                fillRenderer.color = barColor;
            }
        }

        private void SetVisible(bool visible)
        {
            if (fillRenderer != null)
            {
                fillRenderer.enabled = visible;
            }

            if (backRenderer != null)
            {
                backRenderer.enabled = visible;
            }
        }
    }
}
