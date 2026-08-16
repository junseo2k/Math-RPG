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

        public static void LoadMainMenu()
        {
            Load(SceneNames.MainMenu);
        }
    }
}
