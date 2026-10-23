using System;
using System.Collections.Generic;
using UnityEngine;

// 시험판 A 특성·유물 정의. 규칙: Docs/SliceA/CombatDecisions.md 10장 (수치는 모두 임시)
// .asset을 만들지 않고 기본값을 코드에 두며, RunRewards 인스펙터에서 조절한다
public enum TraitId
{
    LostMoment, ReclaimedStrike, RepeatingFate, PastAfterimage, SharpSlash, ToughBody, TimeSaving, TimeRefund,
    SolarWind, WideExpansion, LingeringSun, WanderingSun, DelayedRelease,
}
public enum RelicId { HistoryShard, CrackedPocketWatch, IronSpring, BrokenNeedle, MoonShard, BrokenSundial, SolarConstellation }
public enum GrowthStat { MaxHp, Attack, PotionHeal }

[Serializable]
public class TraitDef
{
    public TraitId id;
    public string displayName;
    public bool rewindOnly;
    // 표시만. 추첨 확률은 동일 (SolarCoreSpec M6)
    public bool rare;
    // 0 이하는 3으로 취급 (씬에 저장된 기존 정의 호환)
    public int maxLevel = 3;
    // {0} = 해당 레벨 수치
    public string format;
    public float[] levels = new float[3];
}

[Serializable]
public class RelicDef
{
    public RelicId id;
    public string displayName;
    public bool rare;
    public float value;
    public float duration;
    public string description;
    // 보스 보상·상점에 나오지 않음 (디버그로만 획득)
    public bool excludeFromPool;
}

[Serializable]
public class GrowthDef
{
    public GrowthStat stat;
    public string displayName;
    public float perLevel;
    public int[] costs = { 5, 5, 10 };
    public string format;
}

public class RewardOffer
{
    public TraitId trait;
    public bool rewindOnly;
    public string title;
    public string description;
    public string badge;
}

public static class RewardCatalog
{
    public static List<TraitDef> Traits() => new()
    {
        new() { id = TraitId.LostMoment, displayName = "잃어버린 찰나", rewindOnly = true, levels = new[] { 0.5f, 0.75f, 1f },
                format = "피해를 받은 뒤 역행하면\n취소된 피해의 {0:0%}만큼 보호막" },
        new() { id = TraitId.ReclaimedStrike, displayName = "되찾은 일격", rewindOnly = true, levels = new[] { 0.2f, 0.35f, 0.5f },
                format = "역행으로 취소된 준 피해의 {0:0%}를\n다음 적중에 추가 피해로 더함" },
        new() { id = TraitId.RepeatingFate, displayName = "반복되는 운명", rewindOnly = true, levels = new[] { 0.2f, 0.35f, 0.5f },
                format = "다시 겪는 보스 공격의\n위험 범위를 {0:0.00}초 일찍 표시" },
        new() { id = TraitId.PastAfterimage, displayName = "과거의 잔상", rewindOnly = true, levels = new[] { 0.3f, 0.5f, 0.7f },
                format = "역행으로 취소된 내 공격을\n잔상이 {0:0%} 피해로 재현" },
        new() { id = TraitId.SharpSlash, displayName = "날카로운 검격", levels = new[] { 0.08f, 0.16f, 0.25f },
                format = "공격 피해 +{0:0%}" },
        new() { id = TraitId.ToughBody, displayName = "강인한 육체", levels = new[] { 10f, 20f, 35f },
                format = "최대 HP +{0}" },
        new() { id = TraitId.TimeSaving, displayName = "시간 절약", levels = new[] { 0.05f, 0.1f, 0.15f },
                format = "권능 쿨타임 -{0:0%}" },
        new() { id = TraitId.TimeRefund, displayName = "시간 환급", levels = new[] { 1f, 1.5f, 2f },
                format = "권능 사용 후 첫 적중 시\n쿨타임 {0}초 감소" },
        // 태양핵 전용 (SolarCoreSpec 3장). 단일 레벨. 수치와 설명은 RunRewards.ApplySolarSettings가 SolarCoreSettings에서 채운다
        new() { id = TraitId.SolarWind, displayName = "태양풍", maxLevel = 1, levels = new[] { 0f } },
        new() { id = TraitId.WideExpansion, displayName = "광역 확장", maxLevel = 1, levels = new[] { 0f } },
        new() { id = TraitId.LingeringSun, displayName = "잔존하는 태양", maxLevel = 1, levels = new[] { 0f } },
        new() { id = TraitId.WanderingSun, displayName = "유랑하는 태양", rare = true, maxLevel = 1, levels = new[] { 0f } },
        new() { id = TraitId.DelayedRelease, displayName = "지연 방출", rare = true, maxLevel = 1, levels = new[] { 0f } },
    };

    public static List<RelicDef> Relics() => new()
    {
        new() { id = RelicId.HistoryShard, displayName = "역사의 파편", rare = true, value = 0.4f, duration = 3f,
                description = "역행 후 3초 내 첫 공격 피해 +40%" },
        new() { id = RelicId.CrackedPocketWatch, displayName = "균열의 회중시계", value = 0.35f, duration = 3f,
                description = "회피 성공 후 3초 내 다음 공격 피해 +35%" },
        new() { id = RelicId.IronSpring, displayName = "철의 태엽", value = 20f,
                description = "최대 HP +20" },
        new() { id = RelicId.BrokenNeedle, displayName = "부서진 검침", value = 0.15f,
                description = "기본 공격 피해 +15%" },
        new() { id = RelicId.MoonShard, displayName = "달빛의 파편", value = 0.2f, duration = 3f,
                description = "회피 성공 시 3초간 받는 피해 -20%" },
        // 태양핵 전용 (SolarCoreSpec 4장). 수치는 SolarCoreSettings, 설명은 RunRewards.ApplySolarSettings가 채운다
        new() { id = RelicId.BrokenSundial, displayName = "부서진 해시계", rare = true },
        // 일출 게이지가 없어 풀 제외 (SolarCoreSpec M2)
        new() { id = RelicId.SolarConstellation, displayName = "태양의 성좌", rare = true, excludeFromPool = true },
    };

    public static List<GrowthDef> Growth() => new()
    {
        new() { stat = GrowthStat.MaxHp, displayName = "최대 HP", perLevel = 10f, format = "+{0}" },
        new() { stat = GrowthStat.Attack, displayName = "공격력", perLevel = 0.05f, format = "+{0:0%}" },
        new() { stat = GrowthStat.PotionHeal, displayName = "포션 회복 효율", perLevel = 0.1f, format = "+{0:0%}" },
    };
}
