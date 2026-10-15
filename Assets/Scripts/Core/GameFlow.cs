using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GameState { AbilitySelect, Playing, Dead, Cleared }

[RequireComponent(typeof(StartAbilityPool))]
public class GameFlow : MonoBehaviour
{
    // 씬에 GameFlow가 없으면 플레이 시작 시 자동 생성
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (FindAnyObjectByType<GameFlow>()) return;
        var go = new GameObject("GameFlow");
        go.AddComponent<GameFlow>();
        go.AddComponent<FlowDebugUI>(); // ponytail: 임시 디버그 UI, 시험판 UI 생기면 이 줄 삭제
    }

    [SerializeField] StartAbilityPool abilityPool;
    [SerializeField] int abilityChoiceCount = 3;
    [SerializeField] int roomsPerBoss = 2;
    [SerializeField] int bossCount = 3;
    [SerializeField] int startHour = 18;
    [SerializeField] int nightEndHour = 6;
    [SerializeField] int hoursPerBoss = 1;
    [SerializeField] float rebirthDelay = 2f;

    public GameState State { get; private set; }
    public RunState Run { get; private set; } = new();
    public PersistentState Persistent { get; private set; } = new();

    public int Stage => Run.stage;
    public StartAbilityDef SelectedAbility => Run.selectedAbility;
    public IReadOnlyList<StartAbilityDef> Choices => Run.choices;
    public int Hour => (startHour + Run.elapsedHours) % 24;
    public string TimeText => $"{Hour:00}:00";
    public int StageCount => (roomsPerBoss + 1) * bossCount;
    public bool IsBossStage => IsBossStageAt(Stage);
    public bool IsBossStageAt(int stage) => stage > 0 && stage % (roomsPerBoss + 1) == 0;
    public int BossIndex => Stage / (roomsPerBoss + 1) - 1;

    int NightLength => (nightEndHour - startHour + 24) % 24;

    public event Action<GameState> StateChanged;
    public event Action<int> StageChanged;
    public event Action<StartAbilityDef> AbilitySelected;
    public event Action RunReset;

    void Awake()
    {
        if (!abilityPool) abilityPool = GetComponent<StartAbilityPool>();
        if (!abilityPool) abilityPool = gameObject.AddComponent<StartAbilityPool>();
    }

    void Start() => BeginRun();

    void BeginRun()
    {
        Run = new RunState { choices = abilityPool.Draw(abilityChoiceCount) };
        RunReset?.Invoke();
        SetState(GameState.AbilitySelect);
    }

    public void SelectAbility(int index)
    {
        if (State != GameState.AbilitySelect || index < 0 || index >= Run.choices.Count) return;
        Run.selectedAbility = Run.choices[index];
        AbilitySelected?.Invoke(Run.selectedAbility);
        EnterStage(1);
        SetState(GameState.Playing);
    }

    // 일반 방: 적 전멸 시, 보스 방: 보스 처치 시 호출
    public void ClearStage()
    {
        if (State != GameState.Playing) return;
        if (IsBossStage) AdvanceTime(hoursPerBoss);
        if (Stage >= StageCount)
        {
            SetState(GameState.Cleared);
            return;
        }
        EnterStage(Stage + 1);
    }

    public void PlayerDied()
    {
        if (State != GameState.Playing) return;
        Persistent.deathCount++;
        SetState(GameState.Dead);
        StartCoroutine(Rebirth());
    }

    void EnterStage(int stage)
    {
        Run.stage = stage;
        if (IsBossStage)
        {
            while (Persistent.bossEncounters.Count <= BossIndex) Persistent.bossEncounters.Add(0);
            Persistent.bossEncounters[BossIndex]++;
        }
        StageChanged?.Invoke(stage);
    }

    // 06:00에 도달하면 세계 초기화 없이 다음 날 18:00으로 이어진다
    void AdvanceTime(int hours)
    {
        Run.elapsedHours += hours;
        while (Run.elapsedHours >= NightLength)
        {
            Run.elapsedHours -= NightLength;
            Persistent.day++;
        }
    }

    // 결과 화면의 '처음부터 다시': 유지 상태까지 초기화하고 1일째부터
    public void RestartGame()
    {
        StopAllCoroutines();
        Persistent = new PersistentState();
        BeginRun();
    }

    IEnumerator Rebirth()
    {
        yield return new WaitForSeconds(rebirthDelay);
        Persistent.day++;
        BeginRun();
    }

    void SetState(GameState next)
    {
        State = next;
        StateChanged?.Invoke(next);
    }
}
