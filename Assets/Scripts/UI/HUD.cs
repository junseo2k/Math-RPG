using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 전투 HUD. 배치: Docs/ArtSpecification.md 2-2 (1920×1080 기준)
public class HUD : MonoBehaviour
{
    [SerializeField] Vector2 hpBarSize = new(400f, 28f);
    [SerializeField] Vector2 dashBarSize = new(200f, 14f);
    [SerializeField] float slotSmall = 22f;
    [SerializeField] float slotBoss = 34f;
    [SerializeField] float slotGap = 50f;
    [SerializeField] Vector2 bossBarSize = new(800f, 24f);
    [SerializeField] float clockTurn = 1f; // 시각 전진 연출 길이

    static readonly Color SlotCleared = new(1f, 0.82f, 0.35f);
    static readonly Color SlotUpcoming = new(0.3f, 0.3f, 0.35f);

    GameFlow flow;
    RoomManager rooms;
    PlayerController player;

    RectTransform content;
    RectTransform hpFill;
    TextMeshProUGUI hpText;
    TextMeshProUGUI abilityText;
    RectTransform dashFill;
    TextMeshProUGUI timeText;
    RectTransform clockHand;
    TextMeshProUGUI plusHour;
    int shownElapsed = -1;
    float handFrom;
    float handTo;
    float turnStart;
    float plusStart = -100f;
    readonly List<Image> slots = new();
    RectTransform bossPanel;
    TextMeshProUGUI bossName;
    RectTransform bossFill;

    void Start()
    {
        flow = FindAnyObjectByType<GameFlow>();
        rooms = FindAnyObjectByType<RoomManager>();
        content = GameUI.MakeRect("Content", transform);
        GameUI.Fill(content);
        BuildTopLeft();
        BuildTopCenter();
        BuildBossBar();
    }

    void BuildTopLeft()
    {
        var hpBg = GameUI.MakeImage(content, "HpBg", GameUI.Dim);
        GameUI.Place(hpBg.rectTransform, new Vector2(0f, 1f), new Vector2(40f, -30f), hpBarSize);
        var fill = GameUI.MakeImage(hpBg.transform, "HpFill", GameUI.Gold);
        hpFill = GameUI.Place(fill.rectTransform, new Vector2(0f, 0.5f), Vector2.zero, hpBarSize);
        hpText = GameUI.MakeText(hpBg.transform, "", 24f, Color.white);
        GameUI.Fill(hpText.rectTransform);

        abilityText = GameUI.MakeText(content, "", 28f, Color.white, TextAlignmentOptions.Left);
        GameUI.Place(abilityText.rectTransform, new Vector2(0f, 1f), new Vector2(40f, -100f), new Vector2(600f, 40f));

        var dashLabel = GameUI.MakeText(content, "대시", 24f, Color.white, TextAlignmentOptions.Left);
        GameUI.Place(dashLabel.rectTransform, new Vector2(0f, 1f), new Vector2(40f, -145f), new Vector2(80f, 30f));
        var dashBg = GameUI.MakeImage(content, "DashBg", GameUI.Dim);
        GameUI.Place(dashBg.rectTransform, new Vector2(0f, 1f), new Vector2(110f, -153f), dashBarSize);
        var dashFillImage = GameUI.MakeImage(dashBg.transform, "DashFill", GameUI.Gold);
        dashFill = GameUI.Place(dashFillImage.rectTransform, new Vector2(0f, 0.5f), Vector2.zero, dashBarSize);
    }

    void BuildTopCenter()
    {
        timeText = GameUI.MakeText(content, "", 40f, Color.white);
        GameUI.Place(timeText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(600f, 56f));

        // 천문 시계(임시 도형): 원 + 바늘
        var faceArt = AssetLibrary.LoadSprite("ui_clock_face");
        var face = GameUI.MakeImage(content, "ClockFace", faceArt ? Color.white : GameUI.Dim, faceArt ? faceArt : AccumulationBoss.CircleSprite());
        GameUI.Place(face.rectTransform, new Vector2(0.5f, 1f), new Vector2(-260f, -48f), new Vector2(56f, 56f));
        face.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        var handArt = AssetLibrary.LoadSprite("ui_clock_hand");
        clockHand = GameUI.MakeImage(face.transform, "Hand", handArt ? Color.white : GameUI.Gold, handArt).rectTransform;
        clockHand.anchorMin = clockHand.anchorMax = new Vector2(0.5f, 0.5f);
        clockHand.pivot = new Vector2(0.5f, 0f);
        clockHand.anchoredPosition = Vector2.zero;
        clockHand.sizeDelta = new Vector2(4f, 24f);

        plusHour = GameUI.MakeText(content, "+1시간", 28f, GameUI.Gold);
        GameUI.Place(plusHour.rectTransform, new Vector2(0.5f, 1f), new Vector2(-260f, -84f), new Vector2(160f, 36f));
        plusHour.gameObject.SetActive(false);

        float width = (flow ? flow.StageCount - 1 : 0) * slotGap;
        for (int i = 1; flow && i <= flow.StageCount; i++)
        {
            bool boss = flow.IsBossStageAt(i);
            float size = boss ? slotBoss : slotSmall;
            var slotArt = AssetLibrary.LoadSprite(boss ? "ui_stage_boss" : "ui_stage_room");
            var slot = GameUI.MakeImage(content, $"Slot{i}", SlotUpcoming, slotArt ? slotArt : RoomManager.Square);
            GameUI.Place(slot.rectTransform, new Vector2(0.5f, 1f), new Vector2(-width / 2f + (i - 1) * slotGap, -100f), new Vector2(size, size));
            slot.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            // 보스 방 = 마름모(모양으로 구분)
            if (boss && !slotArt) slot.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            slots.Add(slot);
        }
    }

    void BuildBossBar()
    {
        bossPanel = GameUI.MakeRect("BossPanel", content);
        GameUI.Place(bossPanel, new Vector2(0.5f, 0f), new Vector2(0f, 50f), new Vector2(bossBarSize.x, 80f));
        bossName = GameUI.MakeText(bossPanel, "", 32f, Color.white);
        GameUI.Place(bossName.rectTransform, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(bossBarSize.x, 44f));
        var bg = GameUI.MakeImage(bossPanel, "BossBg", GameUI.Dim);
        GameUI.Place(bg.rectTransform, new Vector2(0.5f, 0f), Vector2.zero, bossBarSize);
        var fill = GameUI.MakeImage(bg.transform, "BossFill", new Color(0.85f, 0.3f, 0.3f));
        bossFill = GameUI.Place(fill.rectTransform, new Vector2(0f, 0.5f), Vector2.zero, bossBarSize);
    }

    void Update()
    {
        bool show = flow && (flow.State == GameState.Playing || flow.State == GameState.Dead);
        content.gameObject.SetActive(show);
        if (!show) return;
        if (!player) player = FindAnyObjectByType<PlayerController>();

        UpdateHp();
        abilityText.text = flow.SelectedAbility != null ? $"능력: {flow.SelectedAbility.displayName}" : "";
        if (player) dashFill.sizeDelta = new Vector2(dashBarSize.x * player.DashReadyRatio, dashBarSize.y);

        timeText.text = $"{flow.Persistent.day}일째   {flow.TimeText}";
        UpdateClock();
        for (int i = 0; i < slots.Count; i++)
        {
            int stage = i + 1;
            slots[i].color = stage < flow.Stage ? SlotCleared : stage == flow.Stage ? Color.white : SlotUpcoming;
        }

        var boss = rooms ? rooms.CurrentBoss : null;
        bool bossAlive = boss && !boss.IsDead && flow.IsBossStage;
        bossPanel.gameObject.SetActive(bossAlive);
        if (bossAlive)
        {
            bossName.text = rooms.CurrentBossName;
            bossFill.sizeDelta = new Vector2(bossBarSize.x * boss.Hp / boss.MaxHp, bossBarSize.y);
        }
    }

    // 보스 처치로 시각이 전진하면 바늘이 돌고 "+1시간" 표시. 재탄생 등으로 되돌아가면 즉시 맞춤
    void UpdateClock()
    {
        int elapsed = flow.Run.elapsedHours;
        if (elapsed != shownElapsed)
        {
            float target = -(flow.Hour % 12) * 30f;
            bool advanced = shownElapsed >= 0 && elapsed > shownElapsed;
            handFrom = advanced ? clockHand.localEulerAngles.z : target;
            handTo = target;
            turnStart = Time.time;
            if (advanced)
            {
                plusStart = Time.time;
                SoundPlayer.Play("sfx_clock_advance");
            }
            shownElapsed = elapsed;
        }
        float k = clockTurn > 0f ? Mathf.Clamp01((Time.time - turnStart) / clockTurn) : 1f;
        clockHand.localRotation = Quaternion.Euler(0f, 0f, Mathf.LerpAngle(handFrom, handTo, k));

        float since = Time.time - plusStart;
        bool showPlus = since < clockTurn;
        plusHour.gameObject.SetActive(showPlus);
        if (showPlus) plusHour.alpha = 1f - since / clockTurn;
    }

    // HP 100 규모라 칸 대신 바 + 숫자
    void UpdateHp()
    {
        if (!player) return;
        var health = player.Health;
        hpFill.sizeDelta = new Vector2(hpBarSize.x * health.Hp / Mathf.Max(health.MaxHp, 1), hpBarSize.y);
        hpText.text = $"{health.Hp} / {health.MaxHp}";
    }
}
