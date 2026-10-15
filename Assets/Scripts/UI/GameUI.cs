using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// 시험판 UI 루트: 캔버스·EventSystem·글꼴과 화면 3종 생성. 배치 기준: Docs/ArtSpecification.md 2장
public class GameUI : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (FindAnyObjectByType<GameUI>()) return;
        new GameObject("GameUI").AddComponent<GameUI>();
    }

    const string FontPath = "Fonts/NotoSansKR-VariableFont_wght SDF";

    public static readonly Color Gold = new(1f, 0.82f, 0.35f);
    public static readonly Color PanelColor = new(0.08f, 0.07f, 0.12f, 0.9f);
    public static readonly Color Dim = new(0.25f, 0.25f, 0.3f);

    public static TMP_FontAsset Font { get; private set; }

    void Awake()
    {
        Font = Resources.Load<TMP_FontAsset>(FontPath);
        if (!Font) Debug.LogWarning($"[GameUI] 글꼴을 불러오지 못함: Resources/{FontPath}");

        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        gameObject.AddComponent<GraphicRaycaster>();

        if (!FindAnyObjectByType<EventSystem>())
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>();
        }

        AddScreen<HUD>("HUD");
        AddScreen<AbilitySelectUI>("AbilitySelect");
        AddScreen<ResultUI>("Result");
        AddScreen<RebirthOverlay>("Rebirth"); // 맨 위에 그림
    }

    void AddScreen<T>(string name) where T : Component
    {
        var rt = MakeRect(name, transform);
        Fill(rt);
        rt.gameObject.AddComponent<T>();
    }

    // ---- 공용 생성 함수 ----

    public static RectTransform MakeRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    public static void Fill(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    // anchor: (0,1)=좌상단, (0.5,0.5)=중앙 등. 피벗도 같은 점
    public static RectTransform Place(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    public static Image MakeImage(Transform parent, string name, Color color, Sprite sprite = null)
    {
        var img = MakeRect(name, parent).gameObject.AddComponent<Image>();
        img.color = color;
        img.sprite = sprite;
        return img;
    }

    public static TextMeshProUGUI MakeText(Transform parent, string text, float size, Color color,
        TextAlignmentOptions align = TextAlignmentOptions.Center)
    {
        var t = MakeRect("Text", parent).gameObject.AddComponent<TextMeshProUGUI>();
        if (Font) t.font = Font;
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.raycastTarget = false;
        return t;
    }

    public static Button MakeButton(Transform parent, string label, Vector2 pos, Vector2 size, Action onClick)
    {
        var img = MakeImage(parent, label, PanelColor);
        Place(img.rectTransform, new Vector2(0.5f, 0.5f), pos, size);
        img.gameObject.AddComponent<Outline>().effectColor = Gold;
        var button = img.gameObject.AddComponent<Button>();
        var colors = button.colors;
        colors.highlightedColor = new Color(1f, 0.95f, 0.8f);
        colors.selectedColor = new Color(1f, 0.95f, 0.8f);
        button.colors = colors;
        button.onClick.AddListener(() => onClick());
        var t = MakeText(img.transform, label, 36f, Gold);
        Fill(t.rectTransform);
        return button;
    }
}
