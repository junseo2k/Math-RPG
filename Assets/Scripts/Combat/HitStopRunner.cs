using System.Collections;
using UnityEngine;

namespace MathRPG.Combat
{
    /// <summary>
    /// <see cref="HitStop"/>의 코루틴을 돌려주는 숨은 실행체. 직접 씬에 붙이지 않는다 —
    /// HitStop이 첫 요청 때 DontDestroyOnLoad 오브젝트에 자동으로 붙인다.
    ///
    /// 정지 규칙(얼마나, 언제, 겹치면 어떻게)은 전부 HitStop이 들고 있고,
    /// 여기는 "매 프레임 실제 시간을 흘려보내는" 일만 한다.
    /// </summary>
    [AddComponentMenu("")] // 인스펙터의 Add Component 목록에서 숨긴다.
    public sealed class HitStopRunner : MonoBehaviour
    {
        private Coroutine _routine;

        /// <summary>얼림을 시작한다. 이미 돌고 있으면 아무것도 하지 않는다(HitStop이 시간을 연장해준다).</summary>
        internal void Begin()
        {
            if (_routine == null)
            {
                _routine = StartCoroutine(FreezeRoutine());
            }
        }

        private IEnumerator FreezeRoutine()
        {
            HitStop.BeginFreeze();

            // 얼어 있는 동안 Time.deltaTime은 0이다. 반드시 unscaled를 써야 풀린다.
            while (HitStop.TickFreeze(Time.unscaledDeltaTime))
            {
                yield return null;
            }

            HitStop.EndFreeze();
            _routine = null;
        }

        // 플레이 종료·오브젝트 파괴로 코루틴이 강제 종료되면 timeScale이 0에 갇힌다.
        // 마지막 안전장치.
        private void OnDisable()
        {
            _routine = null;
            HitStop.Cancel();
        }
    }
}
