using UnityEngine;

namespace MathRPG.Core
{
    /// <summary>
    /// 게임 전역 진입점. M0 단계에서는 애플리케이션 설정과 수명 관리만 담당한다.
    ///
    /// 의도적으로 비어 있음 — 전투/문제/진행 로직은 각 시스템이 소유하고,
    /// GameManager는 그것들을 직접 참조하지 않는다 (CLAUDE.md 2-2 "거대 스크립트 금지").
    /// 시스템 간 통신이 필요하면 <see cref="EventBus"/>를 쓴다.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("애플리케이션 설정")]
        [SerializeField, Tooltip("목표 프레임레이트. 히트스톱 등 타격감 연출 검증을 위해 고정값을 권장한다.")]
        private int targetFrameRate = 60;

        [SerializeField, Tooltip("수직 동기화 사용 여부. 켜면 targetFrameRate가 무시될 수 있다.")]
        private bool useVSync = false;

        [SerializeField, Tooltip("씬 전환 시에도 이 오브젝트를 유지할지 여부.")]
        private bool persistAcrossScenes = true;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            if (persistAcrossScenes)
            {
                DontDestroyOnLoad(gameObject);
            }

            QualitySettings.vSyncCount = useVSync ? 1 : 0;
            Application.targetFrameRate = targetFrameRate;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
