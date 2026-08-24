using System.Collections;
using MathRPG.Core;
using MathRPG.Data;
using UnityEngine;

namespace MathRPG.Combat
{
    /// <summary>
    /// 공격 하나의 타이밍(윈드업 → 히트 → 히트스톱 → 후딜)을 재생하고,
    /// 각 순간마다 EventBus로 이벤트를 발행한다.
    ///
    /// 이 컴포넌트는 그림 · 이펙트 · 사운드를 전혀 모른다. 그런 반응은 이 이벤트들을
    /// 구독하는 다른 스크립트(지금은 임시 트윈, 나중엔 Animator)가 담당한다.
    /// 그래서 최종 그림/애니메이션으로 교체돼도 이 타이밍 로직은 안 바뀌고,
    /// AttackTimingData의 초 값만 다시 맞추면 된다.
    ///
    /// 히트스톱: 타격 순간 Time.timeScale을 0으로 만들어 물리 · 이동 · Animator를
    /// 한 번에 얼린다. 얼음을 "푸는" 타이머만 Time.unscaledDeltaTime을 써야 한다 —
    /// 그렇지 않으면 타이머 자신도 얼어서 영원히 안 풀린다.
    /// </summary>
    public sealed class AttackTimeline : MonoBehaviour
    {
        public bool IsPlaying { get; private set; }

        private bool _ownsFreeze;

        /// <summary>새 공격 타이밍 재생을 시작한다. 이미 재생 중이면 무시한다.</summary>
        public void Play(AttackTimingData data)
        {
            if (IsPlaying || data == null)
            {
                return;
            }

            IsPlaying = true;
            StartCoroutine(RunSequence(data));

            float effectDelay = Mathf.Max(0f, data.WindupSeconds + data.EffectLeadSeconds);
            StartCoroutine(RunEffectSpawn(effectDelay));
        }

        private IEnumerator RunSequence(AttackTimingData data)
        {
            EventBus.Publish(new AttackWindupStartedEvent(gameObject));

            yield return new WaitForSeconds(data.WindupSeconds);

            EventBus.Publish(new AttackHitEvent(gameObject));
            if (data.HitstopSeconds > 0f)
            {
                StartCoroutine(RunHitStop(data.HitstopSeconds));
            }

            if (data.ActiveSeconds > 0f)
            {
                yield return new WaitForSeconds(data.ActiveSeconds);
            }

            yield return new WaitForSeconds(data.RecoverySeconds);

            EventBus.Publish(new AttackRecoveryEndedEvent(gameObject));
            IsPlaying = false;
        }

        private IEnumerator RunEffectSpawn(float delaySeconds)
        {
            yield return new WaitForSeconds(delaySeconds);
            EventBus.Publish(new AttackEffectSpawnEvent(gameObject));
        }

        private IEnumerator RunHitStop(float durationSeconds)
        {
            _ownsFreeze = true;
            Time.timeScale = 0f;

            float elapsed = 0f;
            while (elapsed < durationSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            Time.timeScale = 1f;
            _ownsFreeze = false;
        }

        private void OnDisable()
        {
            // 히트스톱 도중 이 오브젝트가 비활성화/파괴되면 코루틴이 강제 종료되어
            // timeScale이 0에 갇힐 수 있다. 우리가 건 얼림일 때만 복구한다
            // (다른 시스템이 별도로 일시정지를 걸어둔 상태를 실수로 풀지 않기 위해).
            if (_ownsFreeze)
            {
                Time.timeScale = 1f;
                _ownsFreeze = false;
            }

            IsPlaying = false;
        }
    }
}
