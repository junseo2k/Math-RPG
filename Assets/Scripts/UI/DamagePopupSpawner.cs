using System.Collections.Generic;
using MathRPG.Combat;
using MathRPG.Core;
using UnityEngine;

namespace MathRPG.UI
{
    /// <summary>
    /// DamageDealtEvent를 구독해 피격 지점에 데미지 숫자를 띄운다.
    ///
    /// 씬에 비활성 상태로 둔 template을 복제해 재사용한다. 매번 만들고 지우면 전투 중
    /// GC가 튀고, 템플릿을 씬에 두면 폰트·크기·연출 곡선을 인스펙터에서 그대로
    /// 튜닝할 수 있다 (CLAUDE.md 2-4).
    ///
    /// 누가 때렸는지가 아니라 "누가 맞았는지"로 색을 가른다 — 내가 맞은 피해가
    /// 한눈에 구분돼야 하기 때문이다.
    /// </summary>
    public sealed class DamagePopupSpawner : MonoBehaviour
    {
        [Header("연결")]
        [SerializeField, Tooltip("복제해 쓸 원본. 비활성 상태로 씬에 둔다.")]
        private DamagePopup template;

        [SerializeField, Tooltip("이 대상이 맞았을 때 색을 다르게 쓴다 (보통 플레이어).")]
        private GameObject highlightVictim;

        [Header("색")]
        [SerializeField, Tooltip("highlightVictim이 맞았을 때 숫자 색.")]
        private Color victimColor = new Color(1f, 0.42f, 0.42f, 1f);

        [SerializeField, Tooltip("그 외 대상이 맞았을 때 숫자 색.")]
        private Color defaultColor = new Color(1f, 0.94f, 0.62f, 1f);

        [Header("연타 겹침 방지")]
        [SerializeField, Min(0f), Tooltip("이 시간 안에 같은 대상이 또 맞으면 숫자를 위로 어긋나게 띄운다 (초).")]
        private float stackWindowSeconds = 0.7f;

        [SerializeField, Min(0f), Tooltip("어긋나게 띄울 때 한 단계당 높이 (units).")]
        private float stackStep = 0.45f;

        [SerializeField, Min(1), Tooltip("몇 단계까지 쌓아 올릴지. 넘으면 처음 높이로 돌아간다.")]
        private int stackMaxSteps = 3;

        [Header("풀")]
        [SerializeField, Min(1), Tooltip("동시에 떠 있을 수 있는 숫자의 최대 개수. 넘치면 새 숫자는 생략된다.")]
        private int poolSize = 12;

        private readonly List<DamagePopup> _pool = new List<DamagePopup>();

        // 대상별 "직전에 숫자를 띄운 시각 · 쌓인 단계".
        private readonly Dictionary<GameObject, StackState> _stacks = new Dictionary<GameObject, StackState>();

        private struct StackState
        {
            public float LastTime;
            public int Step;
        }

        private void Awake()
        {
            if (template == null)
            {
                Debug.LogError($"[{nameof(DamagePopupSpawner)}] template이 비어 있습니다.", this);
                enabled = false;
                return;
            }

            // 원본은 절대 재생되지 않아야 한다.
            template.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            EventBus.Subscribe<DamageDealtEvent>(OnDamageDealt);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<DamageDealtEvent>(OnDamageDealt);
        }

        private void OnDamageDealt(DamageDealtEvent evt)
        {
            DamagePopup popup = Rent();
            if (popup == null)
            {
                return;
            }

            bool isHighlighted = highlightVictim != null && evt.Victim == highlightVictim;
            Vector3 position = evt.HitPoint + Vector2.up * (stackStep * NextStackStep(evt.Victim));

            popup.Play(position, evt.Amount, isHighlighted ? victimColor : defaultColor, evt.HitDirection);
        }

        /// <summary>
        /// 같은 대상을 연속으로 때렸을 때 숫자가 같은 자리에 겹치지 않도록 단계를 하나 올린다.
        /// 평타 주기(약 0.35초)가 숫자 수명(0.6초)보다 짧아서, 연타하면 앞 숫자가 아직 떠 있는
        /// 채로 다음 숫자가 뜬다 — 어긋나게 띄우지 않으면 한 대에 두 번 맞은 것처럼 보인다.
        /// </summary>
        private int NextStackStep(GameObject victim)
        {
            if (victim == null)
            {
                return 0;
            }

            int step = 0;
            if (_stacks.TryGetValue(victim, out StackState state)
                && Time.time - state.LastTime < stackWindowSeconds)
            {
                step = (state.Step + 1) % stackMaxSteps;
            }

            _stacks[victim] = new StackState { LastTime = Time.time, Step = step };
            return step;
        }

        /// <summary>노는 숫자를 하나 꺼낸다. 풀이 꽉 찼으면 null.</summary>
        private DamagePopup Rent()
        {
            for (int i = 0; i < _pool.Count; i++)
            {
                if (!_pool[i].IsBusy)
                {
                    return _pool[i];
                }
            }

            if (_pool.Count >= poolSize)
            {
                return null;
            }

            DamagePopup copy = Instantiate(template, transform);
            copy.name = $"DamagePopup_{_pool.Count}";
            copy.gameObject.SetActive(false);
            _pool.Add(copy);

            return copy;
        }
    }
}
