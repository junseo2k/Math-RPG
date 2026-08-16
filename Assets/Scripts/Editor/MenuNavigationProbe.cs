using MathRPG.Core;
using MathRPG.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MathRPG.EditorTools
{
    /// <summary>
    /// 플레이 중 메뉴 화면 사이의 이동을 실제로 시켜보는 진단 도구.
    ///
    /// 씬 전환은 다음 프레임에 일어나므로, 호출한 뒤 별도로 활성 씬을 확인해야 한다.
    /// 버튼 클릭을 자동화할 수 없어서 컨트롤러의 공개 메서드를 직접 호출한다 —
    /// 버튼이 연결하는 것과 같은 메서드다.
    /// </summary>
    public static class MenuNavigationProbe
    {
        [MenuItem("MathRPG/Diagnostics/Play - Report Active Scene", priority = 94)]
        public static void ReportActiveScene()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[MenuNavigationProbe] 플레이 모드에서 실행하세요.");
                return;
            }

            Debug.Log("[MenuNavigationProbe] 활성 씬: " + SceneManager.GetActiveScene().name +
                      " · 뒤로 가면 갈 곳: " + MenuNavigation.ReturnScene);
        }

        [MenuItem("MathRPG/Diagnostics/Play - Press Start", priority = 95)]
        public static void PressStart()
        {
            Invoke(controller => controller.OpenSaveSlots(), "Start");
        }

        [MenuItem("MathRPG/Diagnostics/Play - Press Setting", priority = 96)]
        public static void PressSetting()
        {
            Invoke(controller => controller.OpenSettings(), "Setting");
        }

        [MenuItem("MathRPG/Diagnostics/Play - Press Back", priority = 97)]
        public static void PressBack()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[MenuNavigationProbe] 플레이 모드에서 실행하세요.");
                return;
            }

            var slots = Object.FindFirstObjectByType<SaveSlotScreen>(FindObjectsInactive.Include);
            if (slots != null)
            {
                slots.GoBack();
                Debug.Log("[MenuNavigationProbe] 슬롯 화면에서 뒤로 → " + MenuNavigation.ReturnScene);
                return;
            }

            var settings = Object.FindFirstObjectByType<SettingsScreen>(FindObjectsInactive.Include);
            if (settings != null)
            {
                settings.GoBack();
                Debug.Log("[MenuNavigationProbe] 설정 화면에서 뒤로 → " + MenuNavigation.ReturnScene);
                return;
            }

            Debug.LogWarning("[MenuNavigationProbe] 뒤로 갈 수 있는 화면이 아닙니다.");
        }

        private static void Invoke(System.Action<MainMenuController> action, string buttonName)
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[MenuNavigationProbe] 플레이 모드에서 실행하세요.");
                return;
            }

            var controller = Object.FindFirstObjectByType<MainMenuController>(FindObjectsInactive.Include);
            if (controller == null)
            {
                Debug.LogWarning("[MenuNavigationProbe] MainMenuController가 없습니다. 메인 메뉴 씬에서 실행하세요.");
                return;
            }

            action.Invoke(controller);
            Debug.Log("[MenuNavigationProbe] " + buttonName + " 눌림 — 다음 프레임에 씬이 바뀝니다.");
        }
    }
}
