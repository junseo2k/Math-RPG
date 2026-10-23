using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// 시간 역행: 매 물리 프레임 전장 상태 기록 → 역행 키(기본 우클릭)로 역행 거리 전 상태 복원. 규칙: Docs/SliceA/CombatDecisions.md 3·4·5·8장
// 입력을 다른 스크립트보다 먼저 읽는다(같은 프레임 사망보다 역행 입력 우선)
[DefaultExecutionOrder(-100)]
public class RewindSystem : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (FindAnyObjectByType<RewindSystem>()) return;
        new GameObject("RewindSystem").AddComponent<RewindSystem>();
    }

    [SerializeField] float rewindSeconds = 2f;
    [SerializeField] float cooldown = 20f;
    [SerializeField] float freezeTime = 0.3f;
    [SerializeField] float postRewindInvincible = 0.2f;

    [Header("조작")]
    [SerializeField] InputAction rewindKey = new("Rewind", InputActionType.Button, "<Mouse>/rightButton");

    [Header("디버그")]
    [SerializeField] bool showDebug = true;
    [SerializeField] bool showGhost; // F10

    // 정지 연출 중. 플레이어 입력 차단에 쓴다
    public static bool IsFrozen { get; private set; }

    public float CooldownRemaining => Mathf.Max(readyAt - Time.time, 0f);
    // HUD 표시용 키 이름
    public string RewindKeyName => rewindKey.GetBindingDisplayString();
    public float RecordedSeconds => frames.Count > 1 ? frames[^1].time - frames[0].time : 0f;
    // 특성 "시간 절약"
    public float CooldownMultiplier { get; set; } = 1f;

    // 역행으로 취소된 구간의 정보 (스냅샷 밖에서 따로 기록, CombatDecisions 3장)
    public struct RewindInfo
    {
        public int taken;   // 플레이어가 받은 피해 합
        public int dealt;   // 적에게 준 피해 합
        // 플레이어가 적중시킨 휘두름: 복원 지점 기준 시각, 판정 중심·크기, 피해
        public List<(float offset, Vector2 center, Vector2 size, int damage)> hits;
        public SliceBoss boss;
        // 보스가 결정된 공격을 이어가는 중(예고·실행)으로 복원됨
        public bool bossContinuing;
    }

    public event System.Action<RewindInfo> Rewound;

    struct Frame
    {
        public float time;
        public List<(IRewindable target, object state)> entries;
    }

    readonly List<Frame> frames = new();
    readonly List<(float time, bool player, int amount)> damageLog = new();
    readonly List<(float time, Vector2 center, Vector2 size, int damage)> hitLog = new();
    GameFlow flow;
    RoomManager rooms;
    PlayerController player;
    float readyAt;
    float freezeEnd;
    string lastResult = "-";
    SpriteRenderer ghost;

    void Start()
    {
        flow = FindAnyObjectByType<GameFlow>();
        rooms = FindAnyObjectByType<RoomManager>();
        Health.AnyDamaged += OnAnyDamaged;
        PlayerAttack.HitLanded += OnHitLanded;
        if (!flow) return;
        flow.StageChanged += OnStageChanged;
        flow.RunReset += OnRunReset;
    }

    void OnEnable() => rewindKey.Enable();

    void OnDisable() => rewindKey.Disable();

    void OnDestroy()
    {
        if (IsFrozen) EndFreeze();
        Health.AnyDamaged -= OnAnyDamaged;
        PlayerAttack.HitLanded -= OnHitLanded;
        if (!flow) return;
        flow.StageChanged -= OnStageChanged;
        flow.RunReset -= OnRunReset;
    }

    // 스테이지를 넘어서는 역행은 없음
    void OnStageChanged(int _) => ClearRecords();

    void OnRunReset()
    {
        ClearRecords();
        readyAt = 0f;
        lastResult = "-";
    }

    void ClearRecords()
    {
        frames.Clear();
        damageLog.Clear();
        hitLog.Clear();
    }

    void OnAnyDamaged(Health health, int amount)
    {
        if (!flow || flow.State != GameState.Playing || !Player) return;
        damageLog.Add((Time.time, health == Player.Health, amount));
    }

    void OnHitLanded(Vector2 center, Vector2 size, int damage)
    {
        if (flow && flow.State == GameState.Playing) hitLog.Add((Time.time, center, size, damage));
    }

    public void ReduceCooldown(float seconds) => readyAt -= seconds;

    void FixedUpdate()
    {
        if (!flow || flow.State != GameState.Playing) return;
        var frame = new Frame { time = Time.time, entries = new List<(IRewindable, object)>(Rewindables.All.Count) };
        foreach (var r in Rewindables.All) frame.entries.Add((r, r.Capture()));
        frames.Add(frame);

        // 역행 거리 이전 기록은 1개만 남긴다
        float target = Time.time - rewindSeconds;
        while (frames.Count > 1 && frames[1].time <= target) frames.RemoveAt(0);
        float oldest = frames[0].time;
        damageLog.RemoveAll(e => e.time < oldest);
        hitLog.RemoveAll(e => e.time < oldest);
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb != null && kb.f10Key.wasPressedThisFrame) showGhost = !showGhost;
        UpdateGhost();

        // 정지 연출 중 입력은 무시하고 버퍼에 남기지 않음
        if (IsFrozen)
        {
            if (Time.unscaledTime >= freezeEnd) EndFreeze();
            return;
        }
        if (rewindKey.WasPressedThisFrame()) TryRewind();
    }

    bool CanRewind(out string reason)
    {
        reason = null;
        if (!flow) reason = "흐름 없음";
        else if (flow.State != GameState.Playing && !flow.DiedThisFrame) reason = "전투 중 아님";
        else if (!Player || (Player.Health.IsDead && !flow.DiedThisFrame)) reason = "사망 확정";
        else if (rooms && rooms.CombatConfirmed) reason = "전투 종료 확정";
        else if (Time.time < readyAt) reason = $"쿨타임 {CooldownRemaining:0.0}초";
        else if (frames.Count == 0) reason = "기록 없음";
        return reason == null;
    }

    void TryRewind()
    {
        if (!CanRewind(out string reason))
        {
            lastResult = $"사용 불가: {reason}";
            SoundPlayer.Play("sfx_rewind_denied");
            return;
        }

        int index = TargetIndex();
        var boss = FindAnyObjectByType<SliceBoss>();
        int decisionBefore = boss ? boss.DecisionId : 0;
        int hpBefore = Player.Health.Hp;

        if (flow.DiedThisFrame) flow.CancelDeath();
        foreach (var (target, state) in frames[index].entries)
        {
            // 방 재생성 등으로 파괴된 대상은 건너뜀
            if (target is Object o && !o) continue;
            target.Restore(state);
        }
        frames.RemoveRange(index + 1, frames.Count - index - 1);

        // 쿨타임은 복원 대상이 아님
        readyAt = Time.time + cooldown * CooldownMultiplier;
        Player.Health.GrantInvincible(postRewindInvincible);
        int lastHitAgo = LastPlayerDamageAgo(out float ago) ? Mathf.RoundToInt(ago * 100f) : -1;
        lastResult = $"HP {hpBefore}→{Player.Health.Hp}  복원 지점 {Time.time - frames[index].time:0.00}초 전" +
                     $"  마지막 피격 {(lastHitAgo < 0 ? "기록 없음" : $"{lastHitAgo / 100f:0.00}초 전")}  {Describe(boss, decisionBefore)}";
        SoundPlayer.Play("sfx_rewind");
        StartFreeze();
        Rewound?.Invoke(CollectCancelled(frames[index].time, boss));
    }

    // 디버그: 기록 구간 안에서 플레이어가 마지막으로 맞은 시각
    bool LastPlayerDamageAgo(out float ago)
    {
        ago = 0f;
        for (int i = damageLog.Count - 1; i >= 0; i--)
            if (damageLog[i].player)
            {
                ago = Time.time - damageLog[i].time;
                return true;
            }
        return false;
    }

    // 복원 지점 이후의 피해·적중 기록을 모아 넘기고 지운다
    RewindInfo CollectCancelled(float since, SliceBoss boss)
    {
        var info = new RewindInfo { hits = new(), boss = boss };
        foreach (var (time, player, amount) in damageLog)
        {
            if (time <= since) continue;
            if (player) info.taken += amount;
            else info.dealt += amount;
        }
        foreach (var (time, center, size, damage) in hitLog)
            if (time > since) info.hits.Add((time - since, center, size, damage));
        damageLog.RemoveAll(e => e.time > since);
        hitLog.RemoveAll(e => e.time > since);
        info.bossContinuing = boss && boss.CurrentPhase is SliceBoss.Phase.Telegraph or SliceBoss.Phase.Active;
        return info;
    }

    // 역행 거리 전 시점의 마지막 기록. 기록이 짧으면 가장 오래된 기록
    int TargetIndex()
    {
        float target = Time.time - rewindSeconds;
        int index = 0;
        for (int i = 0; i < frames.Count; i++)
        {
            if (frames[i].time > target) break;
            index = i;
        }
        return index;
    }

    static string Describe(SliceBoss boss, int decisionBefore)
    {
        if (!boss) return "복원";
        int after = boss.DecisionId;
        if (after == decisionBefore) return $"보스 결정 #{after} 유지: 이어감";
        string cancelled = boss.CancelledPattern >= 0 ? $", 취소 {boss.CancelledPatternName} 제외" : "";
        string next = boss.CurrentPhase == SliceBoss.Phase.Idle ? "재선택 예정" : $"#{after} 이어감";
        return $"보스 결정 #{decisionBefore} → #{after}{cancelled}: {next}";
    }

    PlayerController Player => player ? player : player = FindAnyObjectByType<PlayerController>();

    void StartFreeze()
    {
        IsFrozen = true;
        freezeEnd = Time.unscaledTime + freezeTime;
        Time.timeScale = 0f;
    }

    void EndFreeze()
    {
        IsFrozen = false;
        Time.timeScale = 1f;
    }

    // 디버그: 지금 역행하면 플레이어가 돌아갈 위치
    void UpdateGhost()
    {
        bool show = showGhost && frames.Count > 0 && Player;
        if (show && !ghost)
        {
            ghost = new GameObject("RewindGhost").AddComponent<SpriteRenderer>();
            ghost.sprite = RoomManager.Square;
            ghost.color = new Color(0.5f, 0.9f, 1f, 0.35f);
            ghost.sortingOrder = 4;
        }
        if (!ghost) return;
        ghost.enabled = show;
        if (!show) return;
        foreach (var (target, state) in frames[TargetIndex()].entries)
            if (ReferenceEquals(target, Player))
            {
                ghost.transform.position = ((PlayerController.Snapshot)state).position;
                ghost.transform.localScale = Player.transform.localScale;
            }
    }

    void OnGUI()
    {
        if (!showDebug) return;
        string ready = Time.time >= readyAt ? "준비" : $"{CooldownRemaining:0.0}초";
        GUI.Label(new Rect(10, Screen.height - 105, 700, 20),
            $"역행[{RewindKeyName}] {ready}  기록 {RecordedSeconds:0.00}초  잔상[F10] {(showGhost ? "켬" : "끔")}  최근: {lastResult}");
    }
}
