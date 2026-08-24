using MathRPG.Combat;
using MathRPG.Data;
using UnityEngine;

namespace MathRPG.Player
{
    /// <summary>
    /// 평타 입력을 받아 AttackTimeline 재생을 시작시킨다 (기획서 5-1: 평타는 문제 없이 즉시 발동).
    ///
    /// 이 클래스가 하는 일은 딱 하나 — "버튼이 눌렸다"를 "타임라인을 재생하라"로 옮기는 것.
    /// 타이밍은 AttackTimingData가, 그림은 AttackVisuals가 각각 따로 담당한다.
    /// 마나를 소모하고 수학 문제를 띄우는 스킬은 별개 컴포넌트로 M2에서 추가한다.
    ///
    /// 연타 처리는 AttackTimeline.Play가 IsPlaying으로 막아준다 —
    /// 후딜이 끝나기 전에는 다음 공격이 나가지 않는다.
    /// </summary>
    [RequireComponent(typeof(AttackTimeline))]
    public sealed class PlayerAttack : MonoBehaviour
    {
        [Header("입력")]
        [SerializeField, Tooltip("PlayerLocomotion과 같은 InputReader 에셋을 지정할 것.")]
        private InputReader input;

        [Header("타이밍")]
        [SerializeField, Tooltip("평타 타이밍 데이터. Assets/Scripts/Combat/AttackTiming.asset")]
        private AttackTimingData basicAttackTiming;

        /// <summary>공격 중인가. 이동 제한 · 회피 판정이 참조할 수 있도록 열어둔다.</summary>
        public bool IsAttacking => _timeline != null && _timeline.IsPlaying;

        private AttackTimeline _timeline;

        private void Awake()
        {
            _timeline = GetComponent<AttackTimeline>();
        }

        private void OnEnable()
        {
            if (input == null)
            {
                Debug.LogError($"[{nameof(PlayerAttack)}] InputReader가 할당되지 않았습니다. 인스펙터에서 지정하세요.", this);
                return;
            }

            // EnableGameplay / DisableGameplay는 호출하지 않는다 — PlayerLocomotion이 이미
            // 담당하고 있고, 여기서 Disable을 부르면 공격을 끄면서 이동까지 같이 꺼진다.
            input.AttackPressed += OnAttackPressed;
        }

        private void OnDisable()
        {
            if (input == null)
            {
                return;
            }

            input.AttackPressed -= OnAttackPressed;
        }

        private void OnAttackPressed()
        {
            if (basicAttackTiming == null)
            {
                Debug.LogError($"[{nameof(PlayerAttack)}] 평타 타이밍 데이터가 비어 있습니다.", this);
                return;
            }

            _timeline.Play(basicAttackTiming);
        }
    }
}
