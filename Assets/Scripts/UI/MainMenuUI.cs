using UnityEngine;
using UnityEngine.SceneManagement;

// 메인 메뉴 씬: 시작 / 설정 / 종료. 화면은 실행 시 코드로 만든다. 씬 생성: 메뉴 Tools/Scenes/메인 메뉴 씬 생성
public class MainMenuUI : MonoBehaviour
{
    public const string SceneName = "MainMenu";

    [SerializeField] string title = "시험판 A";
    [SerializeField] string gameScene = "VerticalSlice";
    [SerializeField] Color background = new(0.05f, 0.04f, 0.08f);

    GameObject main, settings, quit;

    void Awake()
    {
        if (!Application.CanStreamedLevelBeLoaded(gameScene))
            Debug.LogError($"[MainMenuUI] 게임 씬 '{gameScene}'이 빌드 목록에 없습니다.");
        GameUI.AddCanvas(gameObject, 0);
        GameUI.Fill(GameUI.MakeImage(transform, "Background", background).rectTransform);
        main = GameUI.MakeMenu(transform, title,
            ("시작", () => SceneManager.LoadScene(gameScene)),
            ("설정", () => Show(settings)),
            ("종료", () => Show(quit)));
        settings = MenuPanels.BuildSettings(transform, () => Show(main));
        quit = MenuPanels.BuildQuitConfirm(transform, () => Show(main));
        Show(main);
    }

    void Show(GameObject panel) => GameUI.ShowOnly(panel, main, settings, quit);
}
