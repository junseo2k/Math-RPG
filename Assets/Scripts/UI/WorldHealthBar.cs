using MathRPG.Combat;
using MathRPG.Core;
using UnityEngine;

namespace MathRPG.UI
{
    /// <summary>
    /// 캐릭터 머리 위에 붙는 월드 공간 체력바. 플레이어 · 몬스터 공용.
    ///
    /// Canvas를 쓰지 않고 SpriteRenderer 두 장(배경 · 채움)으로 만든다. 캐릭터의 자식으로
    /// 두므로 따라다니는 로직이 필요 없고, 몬스터가 몇 마리로 늘어나도 각자 하나씩
    /// 달아주기만 하면 된다.
    ///
    /// HealthChangedEvent를 구독하되 owner가 일치할 때만 반응한다 — EventBus는 전역이라
    /// 다른 캐릭터의 체력 변화도 함께 들어오기 때문이다.
    ///
    /// ※ 여기 수치는 기획서 7장 미정 수치가 아니라 M1 검증용 표시 튜닝값이다.
    /// </summary>
    public sealed class WorldHealthBar : MonoBehaviour
    {
        [Header("연결")]
        [SerializeField, Tooltip("이 체력바가 표시할 캐릭터. 비우면 부모에서 Health를 찾는다.")]
        private GameObject owner;

        [SerializeField, Tooltip("줄어드는 막대. 왼쪽 끝을 기준으로 가로 길이가 변한다.")]
        private Transform fill;

        [SerializeField, Tooltip("막대 색을 바꿀 렌더러. 비우면 fill에서 찾는다.")]
        private SpriteRenderer fillRenderer;

        [Header("모양")]
        [SerializeField, Min(0.05f), Tooltip("체력이 가득일 때 막대의 가로 길이 (units).")]
        private float width = 1.2f;

        [SerializeField, Tooltip("체력이 가득일 때 색.")]
        private Color fullColor = new Color(0.36f, 0.84f, 0.45f, 1f);

        [SerializeField, Tooltip("체력이 바닥일 때 색.")]
        private Color emptyColor = new Color(0.92f, 0.31f, 0.31f, 1f);

        [Header("연출")]
        [SerializeField, Min(0f), Tooltip("막대가 실제 체력을 따라가는 속도 (비율/초). 0이면 즉시 반영.")]
        private float followSpeed = 2.5f;

        private Health _health;
        private float _target = 1f;
        private float _displayed = 1f;
        private float _fillHeight = 1f;
        private bool _started;

        private void Awake()
        {
            if (owner == null)
            {
                _health = GetComponentInParent<Health>();
                if (_health != null)
                {
                    owner = _health.gameObject;
                }
            }
            else
            {
                _health = owner.GetComponent<Health>();
            }

            if (fill == null || _health == null)
            {
                Debug.LogError($"[{nameof(WorldHealthBar)}] fill 또는 Health를 찾지 못했습니다. 인스펙터에서 지정하세요.", this);
                enabled = false;
                return;
            }

            if (fillRenderer == null)
            {
                fillRenderer = fill.GetComponent<SpriteRenderer>();
            }

            _fillHeight = fill.localScale.y;
        }

        // 초기값은 Start에서 읽는다 — OnEnable 시점에는 Health.Awake가 아직 안 돌았을 수 있어
        // 체력이 0으로 보인다. 구독만 OnEnable에서 하고, 값 동기화는 모든 Awake 이후로 미룬다.
        private void Start()
        {
            _started = true;
            SyncFromHealth();
        }

        private void OnEnable()
        {
            EventBus.Subscribe<HealthChangedEvent>(OnHealthChanged);

            if (_started)
            {
                SyncFromHealth();
            }
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<HealthChangedEvent>(OnHealthChanged);
        }

        private void Update()
        {
            if (Mathf.Approximately(_displayed, _target))
            {
                return;
            }

            _displayed = followSpeed <= 0f
                ? _target
                : Mathf.MoveTowards(_displayed, _target, followSpeed * Time.deltaTime);

            Apply();
        }

        private void OnHealthChanged(HealthChangedEvent evt)
        {
            if (evt.Owner != owner)
            {
                return;
            }

            _target = evt.Max > 0f ? Mathf.Clamp01(evt.Current / evt.Max) : 0f;
        }

        /// <summary>현재 체력을 즉시 반영한다 (연출 없이).</summary>
        private void SyncFromHealth()
        {
            _target = _health.Max > 0f ? Mathf.Clamp01(_health.Current / _health.Max) : 0f;
            _displayed = _target;
            Apply();
        }

        private void Apply()
        {
            // 왼쪽 끝을 고정한 채 줄어들게 — 스프라이트 피벗을 건드리지 않고 위치로 맞춘다.
            fill.localScale = new Vector3(width * _displayed, _fillHeight, 1f);
            fill.localPosition = new Vector3(width * (_displayed - 1f) * 0.5f, 0f, 0f);

            if (fillRenderer != null)
            {
                fillRenderer.color = Color.Lerp(emptyColor, fullColor, _displayed);
            }
        }
    }
}
