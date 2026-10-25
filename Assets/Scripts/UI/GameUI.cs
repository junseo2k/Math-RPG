using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// 시험판 UI 루트: 캔버스·EventSystem·글꼴과 화면 4종. 배치 기준: Docs/Art/ArtSpecification.md 2장
// 씬에 GameUI가 있으면(메뉴 Tools/UI/씬에 UI 생성) 그것을 쓰고, 없으면 실행 시 코드로 만든다.
public class GameUI : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap() => GameScene.WhenLoaded(() =>
    {
        if (FindAnyObjectByType<GameUI>()) return;
        new GameObject("GameUI").AddComponent<GameUI>();
    });

    const string FontPath = "Fonts/NotoSansKR-VariableFont_wght SDF";

    static readonly Color DefaultGold = new(1f, 0.82f, 0.35f);
    static readonly Color DefaultPanel = new(0.08f, 0.07f, 0.12f, 0.9f);
    static readonly Color DefaultDim = new(0.25f, 0.25f, 0.3f);

    // 실행 중 코드가 칠하는 색(강조 테두리 등). 이미 만들어진 요소의 색은 각 요소에서 고친다
    [SerializeField] Color gold = DefaultGold;
    [SerializeField] Color panelColor = DefaultPanel;
    [SerializeField] Color dim = DefaultDim;

    static GameUI instance;
    static TMP_FontAsset font;

    public static Color Gold => instance ? instance.gold : DefaultGold;
    public static Color PanelColor => instance ? instance.panelColor : DefaultPanel;
    public static Color Dim => instance ? instance.dim : DefaultDim;

    public static TMP_FontAsset Font
    {
        get
        {
            if (font) return font;
            font = Resources.Load<TMP_FontAsset>(FontPath);
            if (!font) Debug.LogWarning($"[GameUI] 글꼴을 불러오지 못함: Resources/{FontPath}");
            return font;
        }
    }

    void Awake()
    {
        instance = this;
        Setup();
    }

    // 빠진 것만 채운다. 에디터 메뉴에서도 호출
    public void Setup()
    {
        AddCanvas(gameObject, 10);
        AddScreen<HUD>("HUD");
        AddScreen<AbilitySelectUI>("AbilitySelect");
        AddScreen<ResultUI>("Result");
        AddScreen<RebirthOverlay>("Rebirth"); // 맨 위에 그림
    }

    void AddScreen<T>(string name) where T : Component
    {
        if (GetComponentInChildren<T>(true)) return;
        var rt = MakeRect(name, transform);
        Fill(rt);
        rt.gameObject.AddComponent<T>();
    }

    // 막대 채움: 배경 크기를 따라가도록 비율을 앵커로 표시
    public static void SetFill(RectTransform fill, float ratio)
    {
        fill.anchorMin = Vector2.zero;
        fill.anchorMax = new Vector2(Mathf.Clamp01(ratio), 1f);
        fill.offsetMin = Vector2.zero;
        fill.offsetMax = Vector2.zero;
    }

    // ---- 공용 생성 함수 ----

    // 빠진 것만 붙이고, 씬에 EventSystem이 없으면 만든다
    public static void AddCanvas(GameObject go, int sortingOrder)
    {
        if (!go.TryGetComponent<Canvas>(out _))
        {
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
        }
        if (!go.TryGetComponent<CanvasScaler>(out _))
        {
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
        }
        if (!go.TryGetComponent<GraphicRaycaster>(out _)) go.AddComponent<GraphicRaycaster>();

        if (!FindAnyObjectByType<EventSystem>())
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>();
        }
    }

    // 제목 + 세로 버튼 목록. 메인 메뉴·일시정지 메뉴가 같이 쓴다
    public static GameObject MakeMenu(Transform parent, string title, params (string label, Action onClick)[] items)
    {
        var rt = MakeRect(title, parent);
        Fill(rt);
        MakeTitle(rt, title);
        for (int i = 0; i < items.Length; i++)
            MakeButton(rt, items[i].label, new Vector2(0f, 80f - i * 120f), new Vector2(420f, 90f), items[i].onClick);
        return rt.gameObject;
    }

    public static TextMeshProUGUI MakeTitle(Transform parent, string title)
    {
        var t = MakeText(parent, title, 72f, Gold);
        Place(t.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 260f), new Vector2(1200f, 120f));
        return t;
    }

    // panels 중 target 하나만 켠다
    public static void ShowOnly(GameObject target, params GameObject[] panels)
    {
        foreach (var p in panels) p.SetActive(p == target);
    }

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
        // 코드로 만든 스프라이트는 씬에 저장되지 않으므로 에디터에서는 비워 두고 실행 시 채운다
        img.sprite = Application.isPlaying ? sprite : null;
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

    // onClick은 생략 가능: 씬에 저장되는 버튼은 실행 시 각 화면이 연결한다
    public static Button MakeButton(Transform parent, string label, Vector2 pos, Vector2 size, Action onClick = null)
    {
        var img = MakeImage(parent, label, PanelColor);
        Place(img.rectTransform, new Vector2(0.5f, 0.5f), pos, size);
        img.gameObject.AddComponent<Outline>().effectColor = Gold;
        var button = img.gameObject.AddComponent<Button>();
        var colors = button.colors;
        colors.highlightedColor = new Color(1f, 0.95f, 0.8f);
        colors.selectedColor = new Color(1f, 0.95f, 0.8f);
        button.colors = colors;
        if (onClick != null) button.onClick.AddListener(() => onClick());
        var t = MakeText(img.transform, label, 36f, Gold);
        Fill(t.rectTransform);
        return button;
    }
}
