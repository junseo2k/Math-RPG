using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 전투 HUD. 배치: Docs/Art/ArtSpecification.md 2-2 (1920×1080 기준)
// 씬에 저장된 요소가 있으면 그대로 쓰고, 없으면 실행 시 코드로 만든다
public class HUD : MonoBehaviour
{
    [Header("처음 생성할 때의 크기 (생성 후에는 씬에서 직접 조절)")]
    [SerializeField] Vector2 hpBarSize = new(400f, 28f);
    [SerializeField] Vector2 dashBarSize = new(200f, 14f);
    [SerializeField] Vector2 bossBarSize = new(800f, 24f);

    [Header("스테이지 칸 (SlotRow 위치를 중심으로 배치)")]
    [SerializeField] float slotSmall = 22f;
    [SerializeField] float slotBoss = 34f;
    [SerializeField] float slotGap = 50f;
    [SerializeField] Color slotCleared = new(1f, 0.82f, 0.35f);
    [SerializeField] Color slotUpcoming = new(0.3f, 0.3f, 0.35f);

    [SerializeField] float clockTurn = 1f; // 시각 전진 연출 길이

    [Header("화면 요소 (생성 시 자동 연결)")]
    [SerializeField] RectTransform content;
    [SerializeField] RectTransform hpFill;
    // 곧 들어올 유예 피해만큼 파란색. 없으면 실행 시 HpFill 뒤에 만든다
    [SerializeField] RectTransform hpDeferredFill;
    [SerializeField] Color deferredColor = new(0.35f, 0.6f, 1f);
    [SerializeField] TextMeshProUGUI hpText;
    [SerializeField] TextMeshProUGUI abilityText;
    [SerializeField] RectTransform dashFill;
    [SerializeField] TextMeshProUGUI timeText;
    [SerializeField] Image clockFace;
    [SerializeField] RectTransform clockHand;
    [SerializeField] TextMeshProUGUI plusHour;
    [SerializeField] RectTransform slotRow;
    [SerializeField] RectTransform bossPanel;
    [SerializeField] TextMeshProUGUI bossName;
    [SerializeField] RectTransform bossFill;

    GameFlow flow;
    RoomManager rooms;
    PlayerController player;
    PotionUser potion;
    RewindSystem rewind;
    DeferredDamage deferred;

    int shownElapsed = -1;
    float handFrom;
    float handTo;
    float turnStart;
    float plusStart = -100f;
    readonly List<Image> slots = new();

    void Start()
    {
        flow = FindAnyObjectByType<GameFlow>();
        rooms = FindAnyObjectByType<RoomManager>();
        EnsureBuilt();
        ApplySprites();
        plusHour.gameObject.SetActive(false);
        BuildSlots();
    }

    public void EnsureBuilt()
    {
        if (content) return;
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
        hpFill = GameUI.MakeImage(hpBg.transform, "HpFill", GameUI.Gold).rectTransform;
        GameUI.SetFill(hpFill, 1f);
        hpText = GameUI.MakeText(hpBg.transform, "", 24f, Color.white);
        hpText.name = "HpText";
        GameUI.Fill(hpText.rectTransform);

        abilityText = GameUI.MakeText(content, "", 28f, Color.white, TextAlignmentOptions.Left);
        abilityText.name = "InfoText";
        GameUI.Place(abilityText.rectTransform, new Vector2(0f, 1f), new Vector2(40f, -100f), new Vector2(900f, 40f));

        var dashLabel = GameUI.MakeText(content, "대시", 24f, Color.white, TextAlignmentOptions.Left);
        dashLabel.name = "DashLabel";
        GameUI.Place(dashLabel.rectTransform, new Vector2(0f, 1f), new Vector2(40f, -145f), new Vector2(80f, 30f));
        var dashBg = GameUI.MakeImage(content, "DashBg", GameUI.Dim);
        GameUI.Place(dashBg.rectTransform, new Vector2(0f, 1f), new Vector2(110f, -153f), dashBarSize);
        dashFill = GameUI.MakeImage(dashBg.transform, "DashFill", GameUI.Gold).rectTransform;
        GameUI.SetFill(dashFill, 1f);
    }

    void BuildTopCenter()
    {
        timeText = GameUI.MakeText(content, "", 40f, Color.white);
        timeText.name = "TimeText";
        GameUI.Place(timeText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(600f, 56f));

        // 천문 시계(임시 도형): 원 + 바늘. 그림은 ApplySprites에서
        bool faceArt = AssetLibrary.LoadSprite("ui_clock_face");
        clockFace = GameUI.MakeImage(content, "ClockFace", faceArt ? Color.white : GameUI.Dim);
        GameUI.Place(clockFace.rectTransform, new Vector2(0.5f, 1f), new Vector2(-260f, -48f), new Vector2(56f, 56f));
        clockFace.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        bool handArt = AssetLibrary.LoadSprite("ui_clock_hand");
        clockHand = GameUI.MakeImage(clockFace.transform, "Hand", handArt ? Color.white : GameUI.Gold).rectTransform;
        clockHand.anchorMin = clockHand.anchorMax = new Vector2(0.5f, 0.5f);
        clockHand.pivot = new Vector2(0.5f, 0f);
        clockHand.anchoredPosition = Vector2.zero;
        clockHand.sizeDelta = new Vector2(4f, 24f);

        plusHour = GameUI.MakeText(content, "+1시간", 28f, GameUI.Gold);
        plusHour.name = "PlusHour";
        GameUI.Place(plusHour.rectTransform, new Vector2(0.5f, 1f), new Vector2(-260f, -84f), new Vector2(160f, 36f));

        slotRow = GameUI.MakeRect("SlotRow", content);
        GameUI.Place(slotRow, new Vector2(0.5f, 1f), new Vector2(0f, -100f), Vector2.zero);
    }

    void BuildBossBar()
    {
        bossPanel = GameUI.MakeRect("BossPanel", content);
        GameUI.Place(bossPanel, new Vector2(0.5f, 0f), new Vector2(0f, 50f), new Vector2(bossBarSize.x, 80f));
        bossName = GameUI.MakeText(bossPanel, "", 32f, Color.white);
        bossName.name = "BossName";
        GameUI.Place(bossName.rectTransform, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(bossBarSize.x, 44f));
        var bg = GameUI.MakeImage(bossPanel, "BossBg", GameUI.Dim);
        GameUI.Place(bg.rectTransform, new Vector2(0.5f, 0f), Vector2.zero, bossBarSize);
        bossFill = GameUI.MakeImage(bg.transform, "BossFill", new Color(0.85f, 0.3f, 0.3f)).rectTransform;
        GameUI.SetFill(bossFill, 1f);
    }

    // 직접 넣은 스프라이트는 두고, 비어 있으면 그림 또는 임시 도형으로
    void ApplySprites()
    {
        if (!clockFace.sprite)
        {
            var art = AssetLibrary.LoadSprite("ui_clock_face");
            clockFace.sprite = art ? art : AccumulationBoss.CircleSprite();
        }
        var hand = clockHand.GetComponent<Image>();
        if (hand && !hand.sprite) hand.sprite = AssetLibrary.LoadSprite("ui_clock_hand");
    }

    // 스테이지 수는 실행 중에만 알 수 있어 칸은 매번 만든다
    void BuildSlots()
    {
        float width = (flow ? flow.StageCount - 1 : 0) * slotGap;
        for (int i = 1; flow && i <= flow.StageCount; i++)
        {
            bool boss = flow.IsBossStageAt(i);
            float size = boss ? slotBoss : slotSmall;
            var slotArt = AssetLibrary.LoadSprite(boss ? "ui_stage_boss" : "ui_stage_room");
            var slot = GameUI.MakeImage(slotRow, $"Slot{i}", slotUpcoming, slotArt ? slotArt : RoomManager.Square);
            GameUI.Place(slot.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-width / 2f + (i - 1) * slotGap, 0f), new Vector2(size, size));
            // 보스 방 = 마름모(모양으로 구분)
            if (boss && !slotArt) slot.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            slots.Add(slot);
        }
    }

    void Update()
    {
        bool show = flow && (flow.State == GameState.Playing || flow.State == GameState.Dead);
        content.gameObject.SetActive(show);
        if (!show) return;
        if (!player) player = FindAnyObjectByType<PlayerController>();

        UpdateHp();
        abilityText.text = InfoText();
        if (player) GameUI.SetFill(dashFill, player.DashReadyRatio);

        timeText.text = $"{flow.Persistent.day}일째   {flow.TimeText}";
        UpdateClock();
        for (int i = 0; i < slots.Count; i++)
        {
            int stage = i + 1;
            slots[i].color = stage < flow.Stage ? slotCleared : stage == flow.Stage ? Color.white : slotUpcoming;
        }

        var boss = rooms ? rooms.CurrentBoss : null;
        bool bossAlive = boss && !boss.IsDead && flow.IsBossStage;
        bossPanel.gameObject.SetActive(bossAlive);
        if (bossAlive)
        {
            bossName.text = rooms.CurrentBossName;
            GameUI.SetFill(bossFill, (float)boss.Hp / boss.MaxHp);
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

    // 재화·포션·보호막·역행 쿨타임 한 줄 (최종 UI 전 임시)
    string InfoText()
    {
        if (!player) return "";
        if (!potion) potion = player.GetComponent<PotionUser>();
        if (!rewind) rewind = FindAnyObjectByType<RewindSystem>();
        string rewindText = !rewind ? "-" : rewind.CooldownRemaining <= 0f ? "준비" : $"{rewind.CooldownRemaining:0.0}초";
        string rewindKey = rewind ? rewind.RewindKeyName : "-";
        string shield = player.Health.Shield > 0 ? $"  보호막 {player.Health.Shield}" : "";
        return $"{flow.Run.gold}G  기억 조각 {flow.Persistent.memoryShards}  포션[Q] {(potion ? potion.Count : 0)}  역행[{rewindKey}] {rewindText}{shield}";
    }

    // HP 100 규모라 칸 대신 바 + 숫자. 유예 피해는 HP 끝에서 파란 구간으로 (금색 = 유예 적용 후 남을 HP)
    void UpdateHp()
    {
        if (!player) return;
        var health = player.Health;
        if (!deferred) deferred = player.GetComponent<DeferredDamage>();
        if (!hpDeferredFill)
        {
            hpDeferredFill = GameUI.MakeImage(hpFill.parent, "HpDeferred", deferredColor).rectTransform;
            hpDeferredFill.SetSiblingIndex(hpFill.GetSiblingIndex());
        }
        int pending = deferred ? Mathf.Min(deferred.Total, health.Hp) : 0;
        float max = Mathf.Max(health.MaxHp, 1);
        GameUI.SetFill(hpDeferredFill, health.Hp / max);
        GameUI.SetFill(hpFill, (health.Hp - pending) / max);
        string hex = ColorUtility.ToHtmlStringRGB(deferredColor);
        hpText.text = pending > 0 ? $"{health.Hp} / {health.MaxHp}  <color=#{hex}>-{pending}</color>" : $"{health.Hp} / {health.MaxHp}";
    }
}
