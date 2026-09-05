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
        [SerializeField, Min(0f), Tooltip("기준 피해량으로 맞혔을 때 게임을 멈추는 길이 " +
                                          "(실제 시간 기준, timeScale 무시). 헛치면 멈추지 않는다.")]
        private float hitstopSeconds = 0.06f;

        [SerializeField, Min(0f), Tooltip("위 정지 길이가 그대로 적용되는 기준 피해량. " +
                                          "0이면 피해량과 무관하게 항상 같은 길이로 멈춘다.")]
        private float hitstopReferenceDamage = 12f;

        [SerializeField, Tooltip("피해량에 따른 정지 길이 배수의 하한 · 상한 (x = 최소, y = 최대). " +
                                 "약한 타격이 사라지지도, 센 타격이 화면을 얼려버리지도 않게 잘라준다.")]
        private Vector2 hitstopScaleRange = new Vector2(0.6f, 2f);

        [SerializeField, Tooltip("이펙트가 임팩트보다 몇 초 먼저(음수)/늦게(양수) 터질지.")]
        private float effectLeadSeconds = 0f;

        public float WindupSeconds => windupSeconds;
        public float ActiveSeconds => activeSeconds;
        public float RecoverySeconds => recoverySeconds;
        public float EffectLeadSeconds => effectLeadSeconds;

        /// <summary>
        /// 입력이 막히는 총 시간 (초). AttackTimeline.IsPlaying이 참인 구간의 길이와 같다.
        /// 공격 쿨다운 바가 이 값으로 길이를 잡는다.
        /// </summary>
        public float TotalSeconds => windupSeconds + activeSeconds + recoverySeconds;

        /// <summary>
        /// 실제로 들어간 피해량에 맞춰 히트스톱 길이를 구한다.
        /// 센 타격이 더 오래 멈춰야 "묵직함"의 차이가 손에 잡힌다.
        /// hitstopReferenceDamage가 0이면 스케일을 끄고 기본값을 그대로 쓴다.
        /// </summary>
        public float GetHitstopSeconds(float damage)
        {
            if (hitstopSeconds <= 0f || hitstopReferenceDamage <= 0f)
            {
                return hitstopSeconds;
            }

            float scale = Mathf.Clamp(damage / hitstopReferenceDamage,
                                      hitstopScaleRange.x, hitstopScaleRange.y);
            return hitstopSeconds * scale;
        }
    }
}
