using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GameState { AbilitySelect, Playing, Dead, Cleared, RewardSelect, Shop, Growth }

// 한 판 흐름: 스테이지 → 클리어 보상 선택 → 다음 스테이지 / 보스 → 보상 → 결과 → 영구 성장 → 다음 순환
// 사망 → 영구 성장 → 다음 순환 18:00. 규칙: Docs/SliceA/VerticalSliceA.md 4·10장
[RequireComponent(typeof(StartAbilityPool), typeof(RunRewards))]
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
    [SerializeField] int roomsPerBoss = 2;
    [SerializeField] int bossCount = 1;
    [SerializeField] int startHour = 18;
    [SerializeField] int nightEndHour = 6;
    [SerializeField] int hoursPerBoss = 1;
    [SerializeField] float rebirthDelay = 2f;

    public GameState State { get; private set; }
    public RunState Run { get; private set; } = new();
    public PersistentState Persistent { get; private set; } = new();
    public RunRewards Rewards { get; private set; }

    public int Stage => Run.stage;
    public StartAbilityDef SelectedAbility => Run.selectedAbility;
    public IReadOnlyList<StartAbilityDef> Choices => Run.choices;
    public IReadOnlyList<RewardOffer> Offers => Run.offers;
    public int Hour => (startHour + Run.elapsedHours) % 24;
    public string TimeText => $"{Hour:00}:00";
    public int StageCount => (roomsPerBoss + 1) * bossCount;
    public bool IsBossStage => IsBossStageAt(Stage);
    public bool IsBossStageAt(int stage) => stage > 0 && stage % (roomsPerBoss + 1) == 0;
    public int BossIndex => Stage / (roomsPerBoss + 1) - 1;

    int NightLength => (nightEndHour - startHour + 24) % 24;
    int deathFrame = -1;

    public event Action<GameState> StateChanged;
    public event Action<int> StageChanged;
    public event Action<StartAbilityDef> AbilitySelected;
    public event Action RunReset;

    void Awake()
    {
        if (!abilityPool) abilityPool = GetComponent<StartAbilityPool>();
        if (!abilityPool) abilityPool = gameObject.AddComponent<StartAbilityPool>();
        Rewards = GetComponent<RunRewards>();
        if (!Rewards) Rewards = gameObject.AddComponent<RunRewards>();
        Persistent = SaveData.Load();
    }

    void Start() => BeginRun();

    // 시험판 A: 권능은 역행 고정이라 시작 능력 선택 없이 바로 1스테이지
    // 새 RunState = 특성·유물·G 초기화. 기억 조각·영구 성장은 Persistent에 유지
    void BeginRun()
    {
        Run = new RunState();
        RunReset?.Invoke();
        Rewards.NotifyChanged();
        EnterStage(1);
        SetState(GameState.Playing);
    }

    public void SelectAbility(int index)
    {
        if (State != GameState.AbilitySelect || index < 0 || index >= Run.choices.Count) return;
        Run.selectedAbility = Run.choices[index];
        AbilitySelected?.Invoke(Run.selectedAbility);
        EnterStage(1);
        SetState(GameState.Playing);
    }

    // 일반 방: 출구 진입 시, 보스 방: 보스 처치 시 호출
    public void ClearStage()
    {
        if (State != GameState.Playing) return;
        bool boss = IsBossStage;
        int gold = boss ? Rewards.BossGold : Rewards.NormalGold;
        Run.gold += gold;
        Run.rewardMessage = $"+{gold}G";
        Run.pendingSelections = 1;

        if (boss)
        {
            AdvanceTime(hoursPerBoss);
            Rewards.AddShards(Rewards.BossShards);
            Run.rewardMessage += $"  기억 조각 +{Rewards.BossShards}";
            // 보스 처치 시 유물 1개. 풀 소진이면 특성 선택 1회 추가
            var relic = Rewards.DrawRelic();
            if (relic != null)
            {
                Rewards.AddRelic(relic.id);
                Run.rewardMessage += $"  유물 [{relic.displayName}] 획득";
            }
            else Run.pendingSelections++;
        }
        OfferTraits(boss);
    }

    // 보스 보상 첫 선택은 역행 전용, 그 외는 통합 풀
    void OfferTraits(bool rewindOnly)
    {
        Run.offers = Rewards.DrawTraits(rewindOnly);
        Run.offerVersion++;
        if (Run.offers.Count == 0)
        {
            FinishRewards();
            return;
        }
        SetState(GameState.RewardSelect);
    }

    public void ChooseReward(int index)
    {
        if (State != GameState.RewardSelect || index < 0 || index >= Run.offers.Count) return;
        Rewards.ChooseTrait(Run.offers[index].trait);
        Run.pendingSelections--;
        if (Run.pendingSelections > 0) OfferTraits(false);
        else FinishRewards();
    }

    void FinishRewards()
    {
        if (Stage >= StageCount)
        {
            SetState(GameState.Cleared);
            return;
        }
        EnterStage(Stage + 1);
        SetState(GameState.Playing);
    }

    // 숨겨진 시간의 통로: 시간이 정지된 공간 (스테이지 진행 정지)
    public void EnterShop()
    {
        if (State != GameState.Playing) return;
        Rewards.StockShop();
        SetState(GameState.Shop);
    }

    public void ExitShop()
    {
        if (State == GameState.Shop) SetState(GameState.Playing);
    }

    public void PlayerDied()
    {
        if (State != GameState.Playing) return;
        deathFrame = Time.frameCount;
        Persistent.deathCount++;
        SetState(GameState.Dead);
        StartCoroutine(OpenGrowthAfterDeath());
    }

    // 사망과 같은 프레임의 역행 입력은 역행 우선 (CombatDecisions 8장)
    public bool DiedThisFrame => State == GameState.Dead && deathFrame == Time.frameCount;

    public void CancelDeath()
    {
        if (!DiedThisFrame) return;
        StopAllCoroutines();
        Persistent.deathCount--;
        SetState(GameState.Playing);
    }

    // 결과 화면(클리어) → 영구 성장
    public void OpenGrowth()
    {
        if (State == GameState.Cleared) SetState(GameState.Growth);
    }

    // 영구 성장 화면 → 다음 순환 18:00
    public void StartNextCycle()
    {
        if (State != GameState.Growth) return;
        Persistent.day++;
        BeginRun();
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

    // 유지 상태까지 초기화하고 1일째부터 (결과 화면에서는 더 이상 호출하지 않음)
    public void RestartGame()
    {
        StopAllCoroutines();
        Persistent = new PersistentState();
        BeginRun();
    }

    IEnumerator OpenGrowthAfterDeath()
    {
        yield return new WaitForSeconds(rebirthDelay);
        SetState(GameState.Growth);
    }

    void SetState(GameState next)
    {
        State = next;
        StateChanged?.Invoke(next);
    }
}
