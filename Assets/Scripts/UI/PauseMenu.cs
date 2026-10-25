using UnityEngine;
using UnityEngine.InputSystem;

// Esc 일시정지 메뉴: 계속 / 설정 / 종료. 열려 있는 동안 시간을 멈춘다(TimeControl). 플레이어 입력도 그걸 보고 막힌다
public class PauseMenu : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap() => GameScene.WhenLoaded(() =>
    {
        if (FindAnyObjectByType<PauseMenu>()) return;
        new GameObject("PauseMenu").AddComponent<PauseMenu>();
    });

    [SerializeField] Color dim = new(0f, 0f, 0f, 0.6f);

    GameObject root, main, settings, quit;
    bool paused;

    void Awake()
    {
        GameUI.AddCanvas(gameObject, 100);
        var bg = GameUI.MakeImage(transform, "Pause", dim);
        GameUI.Fill(bg.rectTransform);
        root = bg.gameObject;
        main = GameUI.MakeMenu(root.transform, "일시정지",
            ("계속", Resume),
            ("설정", () => Show(settings)),
            ("종료", () => Show(quit)));
        settings = MenuPanels.BuildSettings(root.transform, () => Show(main));
        quit = MenuPanels.BuildQuitConfirm(root.transform, () => Show(main));
        root.SetActive(false);
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null || !kb.escapeKey.wasPressedThisFrame) return;
        if (paused) Resume();
        else Pause();
    }

    void Pause()
    {
        paused = true;
        TimeControl.Stop(this);
        root.SetActive(true);
        Show(main);
    }

    void Resume()
    {
        paused = false;
        TimeControl.Release(this);
        root.SetActive(false);
    }

    void OnDestroy()
    {
        if (paused) TimeControl.Release(this);
    }

    void Show(GameObject panel) => GameUI.ShowOnly(panel, main, settings, quit);
}
