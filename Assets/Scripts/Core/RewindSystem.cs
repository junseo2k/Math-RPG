using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// 시간 역행: 매 물리 프레임 전장 상태 기록 → S로 역행 거리 전 상태 복원. 규칙: Docs/CombatDecisions.md 3·4·5·8장
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

    [Header("디버그")]
    [SerializeField] bool showDebug = true;
    [SerializeField] bool showGhost; // F10

    // 정지 연출 중. 플레이어 입력 차단에 쓴다
    public static bool IsFrozen { get; private set; }

    public float CooldownRemaining => Mathf.Max(readyAt - Time.time, 0f);
    public float RecordedSeconds => frames.Count > 1 ? frames[^1].time - frames[0].time : 0f;

    struct Frame
    {
        public float time;
        public List<(IRewindable target, object state)> entries;
    }

    readonly List<Frame> frames = new();
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
        if (!flow) return;
        flow.StageChanged += OnStageChanged;
        flow.RunReset += OnRunReset;
    }

    void OnDestroy()
    {
        if (IsFrozen) EndFreeze();
        if (!flow) return;
        flow.StageChanged -= OnStageChanged;
        flow.RunReset -= OnRunReset;
    }

    // 스테이지를 넘어서는 역행은 없음
    void OnStageChanged(int _) => frames.Clear();

    void OnRunReset()
    {
        frames.Clear();
        readyAt = 0f;
        lastResult = "-";
    }

    void FixedUpdate()
    {
        if (!flow || flow.State != GameState.Playing) return;
        var frame = new Frame { time = Time.time, entries = new List<(IRewindable, object)>(Rewindables.All.Count) };
        foreach (var r in Rewindables.All) frame.entries.Add((r, r.Capture()));
        frames.Add(frame);

        // 역행 거리 이전 기록은 1개만 남긴다
        float target = Time.time - rewindSeconds;
        while (frames.Count > 1 && frames[1].time <= target) frames.RemoveAt(0);
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
        if (kb != null && kb.sKey.wasPressedThisFrame) TryRewind();
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

        if (flow.DiedThisFrame) flow.CancelDeath();
        foreach (var (target, state) in frames[index].entries)
        {
            // 방 재생성 등으로 파괴된 대상은 건너뜀
            if (target is Object o && !o) continue;
            target.Restore(state);
        }
        frames.RemoveRange(index + 1, frames.Count - index - 1);

        // 쿨타임은 복원 대상이 아님
        readyAt = Time.time + cooldown;
        Player.Health.GrantInvincible(postRewindInvincible);
        lastResult = Describe(boss, decisionBefore);
        SoundPlayer.Play("sfx_rewind");
        StartFreeze();
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
            $"역행[S] {ready}  기록 {RecordedSeconds:0.00}초  잔상[F10] {(showGhost ? "켬" : "끔")}  최근: {lastResult}");
    }
}
