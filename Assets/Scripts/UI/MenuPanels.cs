using System;
using TMPro;
using UnityEngine;

// 메인 메뉴와 일시정지 메뉴가 같이 쓰는 설정 창·종료 확인 창
public static class MenuPanels
{
    const string VolumeKey = "volume";
    const float VolumeStep = 0.1f;
    static readonly Vector2 Center = new(0.5f, 0.5f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void ApplySaved() => AudioListener.volume = PlayerPrefs.GetFloat(VolumeKey, 1f);

    public static GameObject BuildSettings(Transform parent, Action onBack)
    {
        var rt = GameUI.MakeRect("Settings", parent);
        GameUI.Fill(rt);
        GameUI.MakeTitle(rt, "설정");

        var volume = GameUI.MakeText(rt, "", 40f, Color.white);
        GameUI.Place(volume.rectTransform, Center, new Vector2(0f, 80f), new Vector2(500f, 90f));
        void RefreshVolume() => volume.text = $"볼륨 {Mathf.RoundToInt(AudioListener.volume * 100f)}%";
        void ChangeVolume(float delta)
        {
            AudioListener.volume = Mathf.Clamp01(Mathf.Round((AudioListener.volume + delta) * 10f) / 10f);
            PlayerPrefs.SetFloat(VolumeKey, AudioListener.volume);
            RefreshVolume();
        }
        GameUI.MakeButton(rt, "-", new Vector2(-330f, 80f), new Vector2(110f, 90f), () => ChangeVolume(-VolumeStep));
        GameUI.MakeButton(rt, "+", new Vector2(330f, 80f), new Vector2(110f, 90f), () => ChangeVolume(VolumeStep));
        RefreshVolume();

        // Screen.fullScreen은 다음 프레임에 바뀌므로 표시용 값을 따로 들고 있는다
        bool fullScreen = Screen.fullScreen;
        var full = GameUI.MakeButton(rt, "", new Vector2(0f, -40f), new Vector2(560f, 90f));
        var fullLabel = full.GetComponentInChildren<TextMeshProUGUI>();
        void RefreshFull() => fullLabel.text = $"전체화면: {(fullScreen ? "켬" : "끔")}";
        full.onClick.AddListener(() =>
        {
            fullScreen = !fullScreen;
            Screen.fullScreen = fullScreen;
            RefreshFull();
        });
        RefreshFull();

        GameUI.MakeButton(rt, "뒤로", new Vector2(0f, -160f), new Vector2(420f, 90f), onBack);
        return rt.gameObject;
    }

    public static GameObject BuildQuitConfirm(Transform parent, Action onCancel)
    {
        var rt = GameUI.MakeRect("QuitConfirm", parent);
        GameUI.Fill(rt);
        var box = GameUI.MakeImage(rt, "Box", GameUI.PanelColor);
        GameUI.Place(box.rectTransform, Center, Vector2.zero, new Vector2(760f, 360f));
        var text = GameUI.MakeText(box.transform, "게임을 종료할까요?", 48f, Color.white);
        GameUI.Place(text.rectTransform, Center, new Vector2(0f, 70f), new Vector2(700f, 100f));
        GameUI.MakeButton(box.transform, "종료", new Vector2(-150f, -80f), new Vector2(240f, 90f), Quit);
        GameUI.MakeButton(box.transform, "취소", new Vector2(150f, -80f), new Vector2(240f, 90f), onCancel);
        return rt.gameObject;
    }

    static void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
