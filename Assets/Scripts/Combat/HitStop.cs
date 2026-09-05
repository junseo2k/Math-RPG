using UnityEngine;
using UnityEngine.SceneManagement;

namespace MathRPG.Combat
{
    /// <summary>
    /// 타격 순간 게임 전체를 잠깐 멈추는 전역 서비스 (Time.timeScale = 0).
    ///
    /// 원래 AttackTimeline이 직접 timeScale을 만졌지만, 그 구조에는 치명적인 문제가 있었다 —
    /// 휘두르기만 하면 뭘 맞혔는지와 무관하게 화면이 멈춰서, <b>때렸을 때와 헛쳤을 때가
    /// 똑같이 느껴졌다.</b> 타격감은 연출의 세기가 아니라 "맞음 / 안 맞음"의 대비에서 나오므로,
    /// 이제 히트스톱은 실제로 피해가 들어갔을 때만 걸린다 (AttackTimeline이 판단해 요청).
    ///
    /// 씬에 배치할 필요가 없다. 첫 요청 때 <see cref="HitStopRunner"/>를 스스로 만든다.
    /// 시간을 실제로 세는 것은 그쪽이고, 여기는 상태와 규칙만 들고 있다.
    /// </summary>
    public static class HitStop
    {
        /// <summary>이보다 짧은 요청은 무시한다. 한 프레임도 안 되는 정지는 보이지 않는다.</summary>
        private const float MinSeconds = 0.005f;

        private static HitStopRunner _runner;
        private static float _remaining;
        private static float _restoreTimeScale = 1f;

        /// <summary>지금 히트스톱으로 멈춰 있는가.</summary>
        public static bool IsFrozen { get; private set; }

        /// <summary>
        /// 히트스톱을 요청한다. 이미 멈춰 있으면 <b>더 긴 쪽으로 연장</b>한다 —
        /// 더하면 한 스윙이 여럿을 맞혔을 때 정지가 누적돼 화면이 끊긴 것처럼 보인다.
        /// </summary>
        public static void Request(float seconds)
        {
            if (seconds < MinSeconds)
            {
                return;
            }

            // 다른 시스템(일시정지 메뉴 등)이 이미 시간을 멈춰둔 상태라면 건드리지 않는다.
            // 여기서 얼렸다가 우리가 1로 되돌리면 그쪽 일시정지가 풀려버린다.
            if (!IsFrozen && Time.timeScale <= 0f)
            {
                return;
            }

            _remaining = Mathf.Max(_remaining, seconds);

            EnsureRunner();
            _runner.Begin();
        }

        /// <summary>진행 중인 히트스톱을 즉시 끝내고 시간을 되돌린다. (씬 전환·일시정지 등)</summary>
        public static void Cancel()
        {
            _remaining = 0f;
            EndFreeze();
        }

        // ------------------------------------------------- HitStopRunner가 호출하는 부분

        internal static void BeginFreeze()
        {
            if (IsFrozen)
            {
                return;
            }

            _restoreTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            IsFrozen = true;
        }

        /// <summary>남은 시간을 줄이고, 아직 얼어 있어야 하면 true를 돌려준다.</summary>
        /// <param name="unscaledDelta">
        /// 반드시 <c>Time.unscaledDeltaTime</c>이어야 한다 — 스케일된 시간을 쓰면
        /// 타이머 자신이 얼어서 영원히 안 풀린다.
        /// </param>
        internal static bool TickFreeze(float unscaledDelta)
        {
            _remaining -= unscaledDelta;
            return _remaining > 0f;
        }

        internal static void EndFreeze()
        {
            _remaining = 0f;

            if (!IsFrozen)
            {
                return;
            }

            Time.timeScale = _restoreTimeScale;
            IsFrozen = false;
        }

        // -------------------------------------------------

        private static void EnsureRunner()
        {
            if (_runner != null)
            {
                return;
            }

            var go = new GameObject("~HitStop");

            // hideFlags는 DontDestroyOnLoad '뒤에' 준다 — DontSave가 먼저 켜져 있으면
            // 에디터에서 DontDestroyOnLoad가 경고를 뱉는다.
            Object.DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.HideInHierarchy;

            _runner = go.AddComponent<HitStopRunner>();

            // 씬이 바뀌는 순간 얼어 있으면 새 씬이 timeScale = 0으로 시작한다.
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Cancel();

        // 도메인 리로드가 꺼져 있으면(Enter Play Mode Options) static 값이 이전 플레이 세션의
        // 것으로 남는다. 특히 IsFrozen이 true로 남으면 다음 플레이가 멈춘 채로 시작한다.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlayModeStart()
        {
            _runner = null;
            _remaining = 0f;
            _restoreTimeScale = 1f;
            IsFrozen = false;
        }
    }
}
