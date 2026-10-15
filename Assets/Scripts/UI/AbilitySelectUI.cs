using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// 시작 능력 선택 화면. 규칙: Docs/ArtSpecification.md 2-1, Docs/VerticalSliceSpec.md F2
public class AbilitySelectUI : MonoBehaviour
{
    [SerializeField] Vector2 cardSize = new(420f, 600f); // 140×200 × 3
    [SerializeField] float cardGap = 72f;                 // 24 × 3
    [SerializeField] float hoverScale = 1.08f;
    [SerializeField] float selectDuration = 0.3f;

    class Card
    {
        public RectTransform rect;
        public Outline outline;
        public CanvasGroup group;
        public Button button;
    }

    GameFlow flow;
    RectTransform content;
    RunState builtFor;
    readonly List<Card> cards = new();
    bool selecting;

    void Start()
    {
        flow = FindAnyObjectByType<GameFlow>();
        content = GameUI.MakeRect("Content", transform);
        GameUI.Fill(content);
    }

    void Update()
    {
        bool show = flow && flow.State == GameState.AbilitySelect;
        content.gameObject.SetActive(show || selecting);
        if (!show) return;

        // 새 판(RunState 교체)마다 카드 다시 생성
        if (builtFor != flow.Run) Build();
        if (!selecting) Highlight();
    }

    void Build()
    {
        builtFor = flow.Run;
        cards.Clear();
        foreach (Transform child in content) Destroy(child.gameObject);

        var dim = GameUI.MakeImage(content, "Dim", new Color(0f, 0f, 0f, 0.55f));
        GameUI.Fill(dim.rectTransform);

        var title = GameUI.MakeText(content, "시작 능력을 하나 선택하세요", 48f, GameUI.Gold);
        GameUI.Place(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 400f), new Vector2(1200f, 80f));

        var choices = flow.Choices;
        float totalWidth = choices.Count * cardSize.x + (choices.Count - 1) * cardGap;
        for (int i = 0; i < choices.Count; i++)
        {
            float x = -totalWidth / 2f + cardSize.x / 2f + i * (cardSize.x + cardGap);
            cards.Add(MakeCard(choices[i], i, x));
        }

        if (cards.Count > 0 && EventSystem.current) EventSystem.current.SetSelectedGameObject(cards[0].button.gameObject);
    }

    Card MakeCard(StartAbilityDef def, int index, float x)
    {
        var bg = GameUI.MakeImage(content, $"Card_{def.id}", GameUI.PanelColor);
        var rect = GameUI.Place(bg.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(x, -20f), cardSize);
        var outline = bg.gameObject.AddComponent<Outline>();
        outline.effectDistance = new Vector2(6f, 6f);
        outline.effectColor = GameUI.Dim;
        var group = bg.gameObject.AddComponent<CanvasGroup>();
        var button = bg.gameObject.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.onClick.AddListener(() => { if (!selecting) StartCoroutine(Select(index)); });

        // 종류: 액티브 = 마름모(검 끝), 패시브 = 원. 모양 + 글자로 구분
        bool active = def.kind == AbilityKind.Active;
        var icon = GameUI.MakeImage(bg.transform, "Icon", GameUI.Gold, active ? RoomManager.Square : AccumulationBoss.CircleSprite());
        GameUI.Place(icon.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(90f, 90f));
        if (active) icon.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);

        var kind = GameUI.MakeText(bg.transform, active ? "액티브" : "패시브", 28f, GameUI.Gold);
        GameUI.Place(kind.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -190f), new Vector2(360f, 40f));

        var title = GameUI.MakeText(bg.transform, def.displayName, 44f, Color.white);
        GameUI.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -240f), new Vector2(380f, 60f));

        var desc = GameUI.MakeText(bg.transform, def.description, 26f, new Color(0.85f, 0.85f, 0.9f), TextAlignmentOptions.Top);
        GameUI.Place(desc.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -320f), new Vector2(360f, 240f));

        if (def.temporary)
        {
            var badge = GameUI.MakeImage(bg.transform, "TempBadge", new Color(0.55f, 0.2f, 0.2f));
            GameUI.Place(badge.rectTransform, new Vector2(1f, 1f), new Vector2(-12f, -12f), new Vector2(100f, 40f));
            var badgeText = GameUI.MakeText(badge.transform, "[임시]", 24f, Color.white);
            GameUI.Fill(badgeText.rectTransform);
        }

        return new Card { rect = rect, outline = outline, group = group, button = button };
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
        for (int i = 0; i < cards.Count; i++)
        {
            bool chosen = i == index;
            cards[i].group.alpha = chosen ? 1f : 0.3f;
            cards[i].outline.effectColor = chosen ? Color.white : GameUI.Dim;
            cards[i].rect.localScale = Vector3.one * (chosen ? hoverScale * 1.04f : 1f);
        }
        yield return new WaitForSeconds(selectDuration);
        selecting = false;
        flow.SelectAbility(index);
    }
}
