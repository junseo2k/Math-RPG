using System;
using System.Collections.Generic;

// 판 단위 임시 상태. 재탄생 시 새 인스턴스로 교체(= 초기화)
[Serializable]
public class RunState
{
    public int stage;
    public int elapsedHours;
    public List<StartAbilityDef> choices = new();
    public StartAbilityDef selectedAbility;

    public int gold;
    public Dictionary<TraitId, int> traits = new();
    public List<RelicId> relics = new();
    public List<RewardOffer> offers = new();
    public int offerVersion;
    public int pendingSelections;
    public string rewardMessage = "";
    public List<RelicId> shopStock = new();
}

// 재탄생해도 유지되는 상태. 기억 조각·영구 성장은 SaveData로 저장
[Serializable]
public class PersistentState
{
    public int day = 1;
    public int deathCount;
    public List<int> bossEncounters = new();

    public int memoryShards;
    public int invested;
    public List<int> growthLevels = new() { 0, 0, 0 };
}
