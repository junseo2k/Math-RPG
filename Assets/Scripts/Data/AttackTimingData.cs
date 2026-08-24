using UnityEngine;

namespace MathRPG.Data
{
    /// <summary>
    /// 공격 하나의 타이밍을 정의하는 데이터.
    /// 프레임 수가 아니라 "초" 단위로 저장한다 — 최종 애니메이션의 프레임 개수·fps가
    /// 나중에 바뀌어도 이 숫자를 다시 셀 필요가 없다 (0.15초는 12fps든 24fps든 0.15초).
    ///
    /// ※ 여기 기본값들은 밸런스 수치(기획서 7장 미정 수치)가 아니라 타격감 튜닝용 임시값이다.
    ///   러프 테스트 단계에서 실제로 눌러보며 조정할 것.
    /// </summary>
    [CreateAssetMenu(menuName = "MathRPG/Combat/Attack Timing", fileName = "AttackTiming")]
    public sealed class AttackTimingData : ScriptableObject
    {
        [Header("타임라인 (초)")]
        [SerializeField, Min(0f), Tooltip("입력 시점부터 타격이 들어가기까지의 선딜.")]
        private float windupSeconds = 0.15f;

        [SerializeField, Min(0f), Tooltip("히트 판정이 열려있는 구간 길이. 0이면 순간 타격.")]
        private float activeSeconds = 0f;

        [SerializeField, Min(0f), Tooltip("타격 후 다음 입력이 가능해지기까지의 후딜.")]
        private float recoverySeconds = 0.2f;

        [Header("타격감")]
        [SerializeField, Min(0f), Tooltip("타격 순간 게임을 멈추는 길이 (실제 시간 기준, timeScale 무시).")]
        private float hitstopSeconds = 0.05f;

        [SerializeField, Tooltip("이펙트가 임팩트보다 몇 초 먼저(음수)/늦게(양수) 터질지.")]
        private float effectLeadSeconds = 0f;

        public float WindupSeconds => windupSeconds;
        public float ActiveSeconds => activeSeconds;
        public float RecoverySeconds => recoverySeconds;
        public float HitstopSeconds => hitstopSeconds;
        public float EffectLeadSeconds => effectLeadSeconds;
    }
}
