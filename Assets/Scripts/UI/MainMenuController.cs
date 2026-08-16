using MathRPG.Core;
using UnityEngine;
using UnityEngine.UI;

namespace MathRPG.UI
{
    /// <summary>
    /// 메인 메뉴 씬의 컨트롤러. Start / Setting / Exit 세 버튼만 담당한다.
    ///
    /// 슬롯 선택과 설정은 각각 독립된 씬이므로 여기서는 씬 전환만 하고,
    /// 그 화면들의 내부 사정은 알지 못한다.
    /// </summary>
    public sealed class MainMenuController : MonoBehaviour
    {
        [Header("버튼")]
        [SerializeField] private Button startButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button exitButton;

        private void Awake()
        {
            if (startButton != null)
            {
                startButton.onClick.AddListener(OpenSaveSlots);
            }

            if (settingsButton != null)
            {
                settingsButton.onClick.AddListener(OpenSettings);
            }

            if (exitButton != null)
            {
                exitButton.onClick.AddListener(QuitGame);
            }
        }

        private void Start()
        {
            // 메뉴로 돌아왔다면 이전 세션이 남아 있지 않도록 정리한다.
            GameSession.End();
        }

        private void OnDestroy()
        {
            if (startButton != null)
            {
                startButton.onClick.RemoveListener(OpenSaveSlots);
            }

            if (settingsButton != null)
            {
                settingsButton.onClick.RemoveListener(OpenSettings);
            }

            if (exitButton != null)
            {
                exitButton.onClick.RemoveListener(QuitGame);
            }
        }

        public void OpenSaveSlots()
        {
            MenuNavigation.GoTo(SceneNames.SaveSlots, SceneNames.MainMenu);
        }

        public void OpenSettings()
        {
            MenuNavigation.GoTo(SceneNames.Settings, SceneNames.MainMenu);
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            // 에디터에서는 Application.Quit()이 아무 일도 하지 않으므로 플레이 모드를 끈다.
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

#if UNITY_EDITOR
        public void EditorBind(Button start, Button settings, Button exit)
        {
            startButton = start;
            settingsButton = settings;
            exitButton = exit;
        }
#endif
    }
}
