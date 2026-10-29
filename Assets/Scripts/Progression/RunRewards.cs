using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// 보상·경제 규칙과 수치: 특성 추첨, 유물, G, 상점, 영구 성장. 규칙: Docs/SliceA/CombatDecisions.md 9~12장
// 상태 자체는 RunState(판)·PersistentState(유지)에 있고, 여기는 규칙과 인스펙터 수치만 가진다
public class RunRewards : MonoBehaviour
{
    [Header("보상")]
    [SerializeField] int normalGold = 50;
    [SerializeField] int bossGold = 100;
    [SerializeField] int bossShards = 10;
    [SerializeField] int offerCount = 3;

    [Header("상점 (시험판 A 전용 가격)")]
    [SerializeField] int potionPrice = 30;
    [SerializeField] int commonRelicPrice = 80;
    [SerializeField] int rareRelicPrice = 100;
    [SerializeField] int shopRelicCount = 2;

    [Header("정의")]
    [SerializeField] List<TraitDef> traits = RewardCatalog.Traits();
    [SerializeField] List<RelicDef> relics = RewardCatalog.Relics();
    [SerializeField] List<GrowthDef> growth = RewardCatalog.Growth();

    [Header("디버그")]
    [SerializeField] bool showDebug = true;

    // 특성·유물·성장이 바뀜 → 효과 재계산
    public event Action Changed;

    public int NormalGold => normalGold;
    public int BossGold => bossGold;
    public int BossShards => bossShards;
    public int PotionPrice => potionPrice;
    public IReadOnlyList<GrowthDef> GrowthItems => growth;

    GameFlow flowCache;
    // RequireComponent로 GameFlow보다 먼저 붙을 수 있어 늦게 찾는다
    GameFlow flow => flowCache ? flowCache : flowCache = GetComponent<GameFlow>();
    RunState Run => flow.Run;
    PersistentState Persistent => flow.Persistent;

    public void NotifyChanged() => Changed?.Invoke();

    static readonly TraitId[] SolarTraits = { TraitId.SolarWind, TraitId.WideExpansion, TraitId.LingeringSun, TraitId.WanderingSun, TraitId.DelayedRelease };

    // 씬에 저장된 목록에는 나중에 추가된 정의가 없을 수 있어 카탈로그에서 보충
    void Awake()
    {
        foreach (var t in RewardCatalog.Traits())
            if (Trait(t.id) == null) traits.Add(t);
        foreach (var r in RewardCatalog.Relics())
            if (Relic(r.id) == null) relics.Add(r);
        ApplySolarSettings();
    }

    // 태양핵 특성·유물의 카드 수치와 설명을 SolarCoreSettings 기준으로 채움 (수치 정의는 설정 에셋 한 곳)
    void ApplySolarSettings()
    {
        var s = SolarCoreSettings.Instance;
        SetTrait(TraitId.SolarWind, s.solarWind.extraMoveSpeedBonus, "태양핵 이동 속도 보너스 +{0:0.#%}p");
        SetTrait(TraitId.WideExpansion, s.wideExpansion.radiusBonus, "태양핵 효과 반경 +{0:0.#%}");
        SetTrait(TraitId.LingeringSun, s.lingeringSun.extraLifetime, "태양핵 유지시간 +{0:0.#}초");
        var w = s.wanderingSun;
        SetTrait(TraitId.WanderingSun, 1f,
            $"태양핵이 플레이어를 따라 이동\n반경 {w.radius:0.#}m, 이동 +{w.moveSpeedBonus:0.#%} 공격 +{w.attackBonus:0.#%} 방어 +{w.damageReduction:0.#%}");
        var d = s.delayedRelease;
        SetTrait(TraitId.DelayedRelease, 1f, $"태양핵 소멸 시 범위 내 적\n{d.duration:0.#}초간 이동 속도 -{d.slowRatio:0.#%}");

        var sun = s.brokenSundial;
        if (Relic(RelicId.BrokenSundial) is { } sundial)
            sundial.description = $"태양핵 소멸 위치에 {sun.duration:0.#}초간 잔광 영역\n(공격·방어 보너스 {sun.efficiency:0.#%})";
        var c = s.solarConstellation;
        if (Relic(RelicId.SolarConstellation) is { } constellation)
            constellation.description = $"태양핵 범위에 진입하면 일출 게이지 {c.chargeAmount:0.#%} 충전\n(태양핵당 {c.maxPerCore}회)";
    }

    void SetTrait(TraitId id, float value, string format)
    {
        if (Trait(id) is not { } def) return;
        def.levels = new[] { value };
        def.format = format;
    }

    // ---- 특성 ----
    public TraitDef Trait(TraitId id) => traits.Find(t => t.id == id);
    public int Level(TraitId id) => Run.traits.TryGetValue(id, out int lv) ? lv : 0;
    static int MaxLevel(TraitDef def) => def.maxLevel > 0 ? def.maxLevel : 3;

    public float TraitValue(TraitId id)
    {
        int lv = Level(id);
        var def = Trait(id);
        return lv > 0 && def != null ? def.levels[Mathf.Min(lv, def.levels.Length) - 1] : 0f;
    }

    // 중복 없이, 최대 레벨 제외, 후보별 동일 확률 (CombatDecisions 9장)
    public List<RewardOffer> DrawTraits(bool rewindOnly)
    {
        ApplySolarSettings(); // 플레이 중 설정 변경도 카드 문구에 반영
        var pool = new List<TraitDef>();
        foreach (var t in traits)
            // 현재 1장 보스는 반복되는 운명의 조기 예고 표시를 지원하지 않는다.
            if (t.id != TraitId.RepeatingFate && Level(t.id) < MaxLevel(t) && (!rewindOnly || t.rewindOnly)) pool.Add(t);
        Shuffle(pool);

        var offers = new List<RewardOffer>();
        for (int i = 0; i < pool.Count && i < offerCount; i++)
        {
            var t = pool[i];
            int lv = Level(t.id);
            offers.Add(new RewardOffer
            {
                trait = t.id,
                rewindOnly = t.rewindOnly,
                title = t.displayName,
                description = string.Format(t.format, t.levels[lv]),
                badge = (t.rare ? "희귀 · " : "") + (MaxLevel(t) == 1 ? "신규" : lv == 0 ? "신규 Lv.1" : $"Lv.{lv} → Lv.{lv + 1}"),
            });
        }
        return offers;
    }

    public void ChooseTrait(TraitId id)
    {
        Run.traits[id] = Mathf.Min(Level(id) + 1, Trait(id) is { } def ? MaxLevel(def) : 3);
        Changed?.Invoke();
    }

    // ---- 유물 ----
    public RelicDef Relic(RelicId id) => relics.Find(r => r.id == id);
    public bool HasRelic(RelicId id) => Run.relics.Contains(id);
    public int RelicPrice(RelicDef def) => def.rare ? rareRelicPrice : commonRelicPrice;

    // 보유 유물·제외 목록을 뺀 무작위. 없으면 null(풀 소진)
    public RelicDef DrawRelic(ICollection<RelicId> exclude = null)
    {
        var pool = new List<RelicDef>();
        foreach (var r in relics)
            if (!r.excludeFromPool && !HasRelic(r.id) && (exclude == null || !exclude.Contains(r.id))) pool.Add(r);
        return pool.Count == 0 ? null : pool[UnityEngine.Random.Range(0, pool.Count)];
    }

    public void AddRelic(RelicId id)
    {
        if (HasRelic(id)) return;
        Run.relics.Add(id);
        Changed?.Invoke();
    }

    // ---- 상점 ----
    public void StockShop()
    {
        ApplySolarSettings();
        Run.shopStock.Clear();
        for (int i = 0; i < shopRelicCount; i++)
        {
            var r = DrawRelic(Run.shopStock);
            if (r == null) break;
            Run.shopStock.Add(r.id);
        }
    }

    public bool TryBuyPotion(PotionUser potion)
    {
        if (!potion || !potion.CanAdd || Run.gold < potionPrice) return false;
        Run.gold -= potionPrice;
        potion.Add();
        return true;
    }

    public bool TryBuyRelic(RelicId id)
    {
        var def = Relic(id);
        if (def == null || HasRelic(id) || !Run.shopStock.Contains(id) || Run.gold < RelicPrice(def)) return false;
        Run.gold -= RelicPrice(def);
        Run.shopStock.Remove(id);
        AddRelic(id);
        return true;
    }

    // ---- 영구 성장 (1단계만) ----
    public int GrowthLevel(GrowthStat stat) => Persistent.growthLevels[(int)stat];
    public GrowthDef Growth(GrowthStat stat) => growth.Find(g => g.stat == stat);
    public float GrowthValue(GrowthStat stat) => Growth(stat) is { } def ? def.perLevel * GrowthLevel(stat) : 0f;

    // 최대 레벨이면 -1
    public int GrowthCost(GrowthStat stat)
    {
        var def = Growth(stat);
        int lv = GrowthLevel(stat);
        return def == null || lv >= def.costs.Length ? -1 : def.costs[lv];
    }

    public bool TryBuyGrowth(GrowthStat stat)
    {
        int cost = GrowthCost(stat);
        if (cost < 0 || Persistent.memoryShards < cost) return false;
        Persistent.memoryShards -= cost;
        Persistent.invested += cost;
        Persistent.growthLevels[(int)stat]++;
        SaveData.Save(Persistent);
        Changed?.Invoke();
        return true;
    }

    public void AddShards(int amount)
    {
        Persistent.memoryShards += amount;
        SaveData.Save(Persistent);
    }

    static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    // 디버그: F11 = G +100, 기억 조각 +10 / F12 = 태양핵 특성 5종·유물 2종, Shift+F12 = 유물 2종만
    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null || !flow) return;
        if (kb.f11Key.wasPressedThisFrame)
        {
            Run.gold += 100;
            AddShards(10);
        }
        if (kb.f12Key.wasPressedThisFrame)
        {
            // Shift+F12 = 유물만 (유랑 없이 성좌 시험용)
            if (!kb.shiftKey.isPressed)
                foreach (var id in SolarTraits) ChooseTrait(id);
            AddRelic(RelicId.BrokenSundial);
            AddRelic(RelicId.SolarConstellation);
        }
    }

    void OnGUI()
    {
        if (!showDebug || !flow) return;
        var lines = new List<string> { $"[보상 디버그 F11]  G {Run.gold}  기억 조각 {Persistent.memoryShards}" };
        foreach (var (id, lv) in Run.traits) lines.Add($"특성 {Trait(id)?.displayName} Lv.{lv}");
        foreach (var id in Run.relics) lines.Add($"유물 {Relic(id)?.displayName}");
        for (int i = 0; i < lines.Count; i++)
            GUI.Label(new Rect(Screen.width - 330, Screen.height - 30 - (lines.Count - i) * 20, 320, 20), lines[i]);
    }
}
