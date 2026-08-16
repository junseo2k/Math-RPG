using UnityEngine;
using UnityEngine.UI;

namespace MathRPG.UI
{
    /// <summary>
    /// 메인 메뉴의 최상위 컨트롤러.
    /// Start / Setting / Exit 세 버튼을 받아 각 패널을 열고 닫는다.
    /// </summary>
    public sealed class MainMenuController : MonoBehaviour
    {
        [Header("메인 버튼")]
        [SerializeField] private GameObject mainButtonsRoot;
        [SerializeField] private Button startButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button exitButton;

        [Header("패널")]
        [SerializeField] private SaveSlotPanel saveSlotPanel;
        [SerializeField] private SettingsPanel settingsPanel;

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

            // 패널이 닫히면 메인 버튼을 다시 보여준다.
            if (saveSlotPanel != null)
            {
                saveSlotPanel.Closed += HandlePanelClosed;
            }

            if (settingsPanel != null)
            {
                settingsPanel.Closed += HandlePanelClosed;
            }
        }

        private void Start()
        {
            // 메뉴로 돌아왔을 때 이전 세션이 남아 있지 않도록 정리한다.
            Core.GameSession.End();

            if (saveSlotPanel != null)
            {
                saveSlotPanel.Hide();
            }

            if (settingsPanel != null)
            {
                settingsPanel.Hide();
            }
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

            if (saveSlotPanel != null)
            {
                saveSlotPanel.Closed -= HandlePanelClosed;
            }

            if (settingsPanel != null)
            {
                settingsPanel.Closed -= HandlePanelClosed;
            }
        }

        public void OpenSaveSlots()
        {
            if (saveSlotPanel == null)
            {
                Debug.LogError("[MainMenuController] SaveSlotPanel이 연결되지 않았습니다.");
                return;
            }

            if (settingsPanel != null)
            {
                settingsPanel.Hide();
            }

            saveSlotPanel.Show();

            // 다른 패널을 닫으면 Closed가 발생해 버튼이 다시 켜지므로, 숨기는 것은 마지막에 한다.
            SetMainButtonsVisible(false);
        }

        public void OpenSettings()
        {
            if (settingsPanel == null)
            {
                Debug.LogError("[MainMenuController] SettingsPanel이 연결되지 않았습니다.");
                return;
            }

            if (saveSlotPanel != null)
            {
                saveSlotPanel.Hide();
            }

            settingsPanel.Show();
            SetMainButtonsVisible(false);
        }

        private void HandlePanelClosed()
        {
            SetMainButtonsVisible(true);
        }

        private void SetMainButtonsVisible(bool visible)
        {
            if (mainButtonsRoot != null)
            {
                mainButtonsRoot.SetActive(visible);
            }
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
        public void EditorBind(GameObject buttonsRoot, Button start, Button settings, Button exit,
                               SaveSlotPanel slots, SettingsPanel settingsPanelRef)
        {
            mainButtonsRoot = buttonsRoot;
            startButton = start;
            settingsButton = settings;
            exitButton = exit;
            saveSlotPanel = slots;
            settingsPanel = settingsPanelRef;
        }
#endif
    }
}
