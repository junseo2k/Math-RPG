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
    /// 히트스톱은 <see cref="HitStop"/>에 요청하며, <b>실제로 뭔가를 맞혔을 때만</b> 건다.
    /// 예전에는 휘두르기만 하면 무조건 걸려서 때렸을 때와 헛쳤을 때가 똑같이 느껴졌다 —
    /// 타격감은 연출의 세기가 아니라 그 둘의 대비에서 나오므로, 헛스윙은 아무 일도
    /// 일어나지 않아야 맞는 순간이 산다.
    /// </summary>
    public sealed class AttackTimeline : MonoBehaviour
    {
        public bool IsPlaying { get; private set; }

        // 이번 스윙이 실제로 뭔가를 맞혔는가, 그리고 그중 가장 큰 피해량은 얼마인가.
        // 여러 대상을 한 번에 맞혀도 히트스톱은 한 번, 가장 센 타격 기준으로만 건다.
        private bool _connectedThisSwing;
        private float _peakDamageThisSwing;

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
            EventBus.Publish(new AttackWindupStartedEvent(gameObject, data.TotalSeconds));

            yield return new WaitForSeconds(data.WindupSeconds);

            // 발행 '전에' 비운다. EventBus.Publish는 동기 호출이라 아래 한 줄이 끝나기 전에
            // Hitbox → Health → DamageDealtEvent → OnDamageDealt까지 전부 실행된다.
            // 즉 Publish가 돌아온 시점에는 맞혔는지 여부가 이미 확정돼 있다.
            _connectedThisSwing = false;
            _peakDamageThisSwing = 0f;

            EventBus.Publish(new AttackHitEvent(gameObject));

            if (_connectedThisSwing)
            {
                HitStop.Request(data.GetHitstopSeconds(_peakDamageThisSwing));
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

        private void OnEnable()
        {
            EventBus.Subscribe<DamageDealtEvent>(OnDamageDealt);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<DamageDealtEvent>(OnDamageDealt);
            IsPlaying = false;
        }

        // "내가 때려서 들어간 피해"만 센다. EventBus는 전역이라 이 필터가 없으면
        // 적이 때린 것에도 내 히트스톱이 걸린다.
        private void OnDamageDealt(DamageDealtEvent e)
        {
            if (e.Source != gameObject)
            {
                return;
            }

            _connectedThisSwing = true;
            _peakDamageThisSwing = Mathf.Max(_peakDamageThisSwing, e.Amount);
        }
    }
}
