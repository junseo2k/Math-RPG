using UnityEngine;
using UnityEngine.SceneManagement;

namespace MathRPG.Core
{
    /// <summary>
    /// 씬 이름을 문자열 리터럴로 흩뿌리지 않기 위한 상수 모음.
    /// 씬을 추가하면 여기에 등록하고 Build Settings에도 넣어야 한다.
    /// </summary>
    public static class SceneNames
    {
        public const string MainMenu = "MainMenu";
        public const string SaveSlots = "SaveSlots";
        public const string Settings = "Settings";

        /// <summary>M1 액션 프로토타입 검증용 씬. 정식 게임 씬이 생기면 교체된다.</summary>
        public const string CombatSandbox = "CombatSandbox";
    }

    /// <summary>씬 전환 진입점. 로딩 화면이 필요해지면 여기만 비동기로 바꾼다.</summary>
    public static class SceneLoader
    {
        public static void Load(string sceneName)
        {
            SceneManager.LoadScene(sceneName);
        }
    }

    /// <summary>
    /// 메뉴 화면 사이의 이동을 담당한다.
    ///
    /// 설정 화면은 메인 메뉴 말고 게임 중 일시정지에서도 열릴 수 있으므로,
    /// "어디서 왔는지"를 기억해 뒤로 가기가 항상 올바른 곳으로 돌아가게 한다.
    /// 씬을 넘나드는 값이라 정적 상태로 둔다.
    /// </summary>
    public static class MenuNavigation
    {
        private const string DefaultReturnScene = SceneNames.MainMenu;

        /// <summary>뒤로 가기를 눌렀을 때 돌아갈 씬.</summary>
        public static string ReturnScene { get; private set; } = DefaultReturnScene;

        /// <summary>돌아올 곳을 기억하며 다른 화면으로 이동한다.</summary>
        public static void GoTo(string sceneName, string returnScene)
        {
            ReturnScene = string.IsNullOrEmpty(returnScene) ? DefaultReturnScene : returnScene;
            SceneLoader.Load(sceneName);
        }

        /// <summary>현재 씬에서 돌아올 곳을 기억하며 이동한다.</summary>
        public static void GoToFromCurrent(string sceneName)
        {
            GoTo(sceneName, SceneManager.GetActiveScene().name);
        }

        /// <summary>기억해 둔 곳으로 돌아간다.</summary>
        public static void GoBack()
        {
            SceneLoader.Load(ReturnScene);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlayModeStart()
        {
            ReturnScene = DefaultReturnScene;
        }
    }
}
