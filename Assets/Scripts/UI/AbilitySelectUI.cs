using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// 시작 능력 선택 화면. 규칙: Docs/Art/ArtSpecification.md 2-1
// 카드는 CardTemplate(비활성)을 복제한다. 모양은 씬에서 견본을 고치면 된다
public class AbilitySelectUI : MonoBehaviour
{
    [SerializeField] Vector2 cardSize = new(420f, 600f); // 140×200 × 3. 처음 생성할 때만 사용
    [SerializeField] float cardGap = 72f;                 // 24 × 3
    [SerializeField] float hoverScale = 1.08f;
    [SerializeField] float selectDuration = 0.3f;

    [Header("화면 요소 (생성 시 자동 연결)")]
    [SerializeField] RectTransform content;
    [SerializeField] TextMeshProUGUI subtitle;
    [SerializeField] RectTransform cardTemplate;

    class Card
    {
        public RectTransform rect;
        public Outline outline;
        public CanvasGroup group;
        public Button button;
    }

    GameFlow flow;
    RunState builtFor;
    int builtVersion = -1;
    readonly List<Card> cards = new();
    bool selecting;
    GameObject lastHighlighted;

    void Start()
    {
        flow = FindAnyObjectByType<GameFlow>();
        EnsureBuilt();
        cardTemplate.gameObject.SetActive(false);
    }

    public void EnsureBuilt()
    {
        if (content) return;
        content = GameUI.MakeRect("Content", transform);
        GameUI.Fill(content);

        var dim = GameUI.MakeImage(content, "Dim", new Color(0f, 0f, 0f, 0.55f));
        GameUI.Fill(dim.rectTransform);

        var title = GameUI.MakeText(content, "특성을 하나 선택하세요", 48f, GameUI.Gold);
        title.name = "Title";
        GameUI.Place(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 400f), new Vector2(1200f, 80f));
        subtitle = GameUI.MakeText(content, "", 30f, Color.white);
        subtitle.name = "Subtitle";
        GameUI.Place(subtitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 340f), new Vector2(1400f, 50f));

        BuildCardTemplate();
    }

    void BuildCardTemplate()
    {
        bool cardArt = AssetLibrary.LoadSprite("ui_card");
        var bg = GameUI.MakeImage(content, "CardTemplate", cardArt ? Color.white : GameUI.PanelColor);
        cardTemplate = GameUI.Place(bg.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), cardSize);
        var outline = bg.gameObject.AddComponent<Outline>();
        outline.effectDistance = new Vector2(6f, 6f);
        outline.effectColor = GameUI.Dim;
        bg.gameObject.AddComponent<CanvasGroup>();
        bg.gameObject.AddComponent<Button>().transition = Selectable.Transition.None;

        // 종류: 역행 전용 = 마름모, 공용 = 원. 그림은 카드마다 실행 시 지정
        var icon = GameUI.MakeImage(bg.transform, "Icon", GameUI.Gold);
        GameUI.Place(icon.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(90f, 90f));

        CardText(bg.transform, "Kind", "공용", 28f, GameUI.Gold, TextAlignmentOptions.Center, new Vector2(0f, -190f), new Vector2(360f, 40f));
        CardText(bg.transform, "Title", "특성 이름", 44f, Color.white, TextAlignmentOptions.Center, new Vector2(0f, -240f), new Vector2(380f, 60f));
        CardText(bg.transform, "Desc", "설명", 26f, new Color(0.85f, 0.85f, 0.9f), TextAlignmentOptions.Top, new Vector2(0f, -320f), new Vector2(360f, 240f));

        // 레벨 변화 표시 (CombatDecisions 9장)
        var badge = GameUI.MakeImage(bg.transform, "Badge", new Color(0.25f, 0.25f, 0.35f));
        GameUI.Place(badge.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(260f, 44f));
        var badgeText = GameUI.MakeText(badge.transform, "Lv", 26f, Color.white);
        badgeText.name = "BadgeText";
        GameUI.Fill(badgeText.rectTransform);
    }

    static void CardText(Transform parent, string name, string text, float size, Color color, TextAlignmentOptions align, Vector2 pos, Vector2 box)
    {
        var t = GameUI.MakeText(parent, text, size, color, align);
        t.name = name;
        GameUI.Place(t.rectTransform, new Vector2(0.5f, 1f), pos, box);
    }

    // 시험판 A: 시작 능력 대신 스테이지 클리어 특성 보상 선택에 사용 (CombatDecisions 9장)
    void Update()
    {
        bool show = flow && flow.State == GameState.RewardSelect;
        content.gameObject.SetActive(show || selecting);
        if (!show) return;

        // 새 제시(판 교체 또는 offerVersion 증가)마다 카드 다시 생성
        if (builtFor != flow.Run || builtVersion != flow.Run.offerVersion) Build();
        if (!selecting) Highlight();
    }

    void Build()
    {
        builtFor = flow.Run;
        builtVersion = flow.Run.offerVersion;
        foreach (var card in cards) Destroy(card.rect.gameObject);
        cards.Clear();

        subtitle.text = flow.Run.rewardMessage;

        var offers = flow.Offers;
        float width = cardTemplate.rect.width;
        float totalWidth = offers.Count * width + (offers.Count - 1) * cardGap;
        for (int i = 0; i < offers.Count; i++)
        {
            float x = -totalWidth / 2f + width / 2f + i * (width + cardGap);
            cards.Add(MakeCard(offers[i], i, x));
        }

        if (cards.Count > 0 && EventSystem.current) EventSystem.current.SetSelectedGameObject(cards[0].button.gameObject);
    }

    Card MakeCard(RewardOffer def, int index, float x)
    {
        var rect = Instantiate(cardTemplate, cardTemplate.parent);
        rect.name = $"Card_{def.trait}";
        rect.gameObject.SetActive(true);
        rect.anchoredPosition = new Vector2(x, cardTemplate.anchoredPosition.y);

        var bg = rect.GetComponent<Image>();
        if (!bg.sprite) bg.sprite = AssetLibrary.LoadSprite("ui_card");

        bool active = def.rewindOnly;
        var iconArt = AssetLibrary.LoadSprite(active ? "ui_icon_active" : "ui_icon_passive");
        var icon = rect.Find("Icon").GetComponent<Image>();
        icon.sprite = iconArt ? iconArt : active ? RoomManager.Square : AccumulationBoss.CircleSprite();
        if (iconArt) icon.color = Color.white;
        icon.rectTransform.localRotation = active && !iconArt ? Quaternion.Euler(0f, 0f, 45f) : Quaternion.identity;

        SetText(rect, "Kind", active ? "역행 전용" : "공용");
        SetText(rect, "Title", def.title);
        SetText(rect, "Desc", def.description);
        SetText(rect, "Badge/BadgeText", def.badge);

        var button = rect.GetComponent<Button>();
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => { if (!selecting) StartCoroutine(Select(index)); });

        return new Card
        {
            rect = rect,
            outline = rect.GetComponent<Outline>(),
            group = rect.GetComponent<CanvasGroup>(),
            button = button,
        };
    }

    static void SetText(Transform card, string path, string text)
    {
        var t = card.Find(path);
        if (t) t.GetComponent<TextMeshProUGUI>().text = text;
    }

    // 마우스를 올리거나 키보드로 고른 카드: 확대 + 금색 테두리
    void Highlight()
    {
        var es = EventSystem.current;
        if (!es) return;
        var mouse = Mouse.current;
        foreach (var card in cards)
        {
            bool hovered = mouse != null &&
                RectTransformUtility.RectangleContainsScreenPoint(card.rect, mouse.position.ReadValue());
            if (hovered) es.SetSelectedGameObject(card.button.gameObject);
        }
        var selected = es.currentSelectedGameObject;
        if (selected != lastHighlighted)
        {
            if (lastHighlighted) SoundPlayer.Play("sfx_ui_hover");
            lastHighlighted = selected;
        }
        foreach (var card in cards)
        {
            bool on = selected == card.button.gameObject;
            card.rect.localScale = Vector3.one * (on ? hoverScale : 1f);
            card.outline.effectColor = on ? GameUI.Gold : GameUI.Dim;
        }
    }

    // 선택 연출: 고른 카드는 빛나고 나머지는 어두워짐 → 진행
    IEnumerator Select(int index)
    {
        selecting = true;
        SoundPlayer.Play(flow.Offers[index].rewindOnly ? "sfx_ui_select_active" : "sfx_ui_select_passive");
        for (int i = 0; i < cards.Count; i++)
        {
            bool chosen = i == index;
            cards[i].group.alpha = chosen ? 1f : 0.3f;
            cards[i].outline.effectColor = chosen ? Color.white : GameUI.Dim;
            cards[i].rect.localScale = Vector3.one * (chosen ? hoverScale * 1.04f : 1f);
        }
        yield return new WaitForSeconds(selectDuration);
        selecting = false;
        flow.ChooseReward(index);
    }
}
