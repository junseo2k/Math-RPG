using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// 보상·경제 규칙과 수치: 특성 추첨, 유물, G, 상점, 영구 성장. 규칙: Docs/CombatDecisions.md 9~12장
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

    // ---- 특성 ----
    public TraitDef Trait(TraitId id) => traits.Find(t => t.id == id);
    public int Level(TraitId id) => Run.traits.TryGetValue(id, out int lv) ? lv : 0;

    public float TraitValue(TraitId id)
    {
        int lv = Level(id);
        var def = Trait(id);
        return lv > 0 && def != null ? def.levels[Mathf.Min(lv, def.levels.Length) - 1] : 0f;
    }

    // 중복 없이, Lv.3 제외, 후보별 동일 확률 (CombatDecisions 9장)
    public List<RewardOffer> DrawTraits(bool rewindOnly)
    {
        var pool = new List<TraitDef>();
        foreach (var t in traits)
            if (Level(t.id) < 3 && (!rewindOnly || t.rewindOnly)) pool.Add(t);
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
                badge = lv == 0 ? "신규 Lv.1" : $"Lv.{lv} → Lv.{lv + 1}",
            });
        }
        return offers;
    }

    public void ChooseTrait(TraitId id)
    {
        Run.traits[id] = Mathf.Min(Level(id) + 1, 3);
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
            if (!HasRelic(r.id) && (exclude == null || !exclude.Contains(r.id))) pool.Add(r);
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

    // 디버그: F11 = G +100, 기억 조각 +10
    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null || !kb.f11Key.wasPressedThisFrame || !flow) return;
        Run.gold += 100;
        AddShards(10);
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
