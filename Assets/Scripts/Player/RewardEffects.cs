using System.Collections.Generic;
using UnityEngine;

// 특성·유물·영구 성장 효과를 플레이어에 적용. 규칙: Docs/SliceA/CombatDecisions.md 10·12장
[RequireComponent(typeof(PlayerController), typeof(PlayerAttack))]
public class RewardEffects : MonoBehaviour
{
    [SerializeField] float afterimageShow = 0.12f;

    static readonly Color AfterimageColor = new(0.5f, 0.9f, 1f, 0.5f);

    PlayerController controller;
    PlayerAttack attack;
    Health health;
    SunCoreCaster sunCore;
    GameFlow flow;
    RunRewards rewards;
    RewindSystem rewind;
    bool refundPending;
    // 과거의 잔상: 재개 후 실행할 시각, 판정, 피해
    readonly List<(float at, Vector2 center, Vector2 size, int damage)> afterimages = new();

    void Awake()
    {
        controller = GetComponent<PlayerController>();
        attack = GetComponent<PlayerAttack>();
        health = GetComponent<Health>();
        // SunCoreCaster 부트스트랩보다 먼저 실행될 수 있어 여기서도 보장
        sunCore = GetComponent<SunCoreCaster>();
        if (!sunCore) sunCore = gameObject.AddComponent<SunCoreCaster>();
    }

    void Start()
    {
        flow = FindAnyObjectByType<GameFlow>();
        rewind = FindAnyObjectByType<RewindSystem>();
        rewards = flow ? flow.Rewards : null;
        health.DodgeSucceeded += OnDodge;
        PlayerAttack.HitLanded += OnHitLanded;
        if (flow)
        {
            flow.RunReset += OnRunReset;
            flow.StageChanged += OnStageChanged;
        }
        if (rewards) rewards.Changed += Recalc;
        if (rewind) rewind.Rewound += OnRewound;
        Recalc();
    }

    void OnDestroy()
    {
        PlayerAttack.HitLanded -= OnHitLanded;
        if (flow)
        {
            flow.RunReset -= OnRunReset;
            flow.StageChanged -= OnStageChanged;
        }
        if (rewards) rewards.Changed -= Recalc;
        if (rewind) rewind.Rewound -= OnRewound;
    }

    float T(TraitId id) => rewards ? rewards.TraitValue(id) : 0f;
    bool Has(RelicId id) => rewards && rewards.HasRelic(id);
    bool HasTrait(TraitId id) => rewards && rewards.Level(id) > 0;
    RelicDef R(RelicId id) => rewards.Relic(id);
    float G(GrowthStat stat) => rewards ? rewards.GrowthValue(stat) : 0f;

    // 상시 효과 재계산
    void Recalc()
    {
        attack.DamageBonus = T(TraitId.SharpSlash) + (Has(RelicId.BrokenNeedle) ? R(RelicId.BrokenNeedle).value : 0f) + G(GrowthStat.Attack);
        int bonusHp = Mathf.RoundToInt(T(TraitId.ToughBody) + (Has(RelicId.IronSpring) ? R(RelicId.IronSpring).value : 0f) + G(GrowthStat.MaxHp));
        health.SetMaxHp(controller.BaseMaxHp + bonusHp);
        if (rewind) rewind.CooldownMultiplier = 1f - T(TraitId.TimeSaving);

        // 태양핵 수치는 SolarCoreSettings가 가지므로 보유 여부만 전달
        sunCore.SolarWind = HasTrait(TraitId.SolarWind);
        sunCore.WideExpansion = HasTrait(TraitId.WideExpansion);
        sunCore.LingeringSun = HasTrait(TraitId.LingeringSun);
        sunCore.Wandering = HasTrait(TraitId.WanderingSun);
        sunCore.DelayedRelease = HasTrait(TraitId.DelayedRelease);
        sunCore.Sundial = Has(RelicId.BrokenSundial);
        sunCore.Constellation = Has(RelicId.SolarConstellation);
    }

    // 보호막은 스테이지 종료 시 소멸 (CombatDecisions 2장)
    void OnStageChanged(int _)
    {
        health.ClearShield();
        afterimages.Clear();
        refundPending = false;
    }

    void OnRunReset()
    {
        OnStageChanged(0);
        Recalc();
    }

    void OnRewound(RewindSystem.RewindInfo info)
    {
        if (info.taken >= 1 && T(TraitId.LostMoment) > 0f)
            health.AddShield(Health.Round(info.taken * T(TraitId.LostMoment)));

        // 다음 적중까지 유지
        if (info.dealt > 0 && T(TraitId.ReclaimedStrike) > 0f)
            attack.GrantNextBonus(Health.Round(info.dealt * T(TraitId.ReclaimedStrike)), float.MaxValue);

        if (info.bossContinuing && T(TraitId.RepeatingFate) > 0f)
        {
            info.boss.RepeatDecisionId = info.boss.DecisionId;
            info.boss.EarlyWarning = T(TraitId.RepeatingFate);
        }

        if (T(TraitId.PastAfterimage) > 0f)
            foreach (var (offset, center, size, damage) in info.hits)
                afterimages.Add((Time.time + offset, center, size, Health.Round(damage * T(TraitId.PastAfterimage))));

        refundPending = T(TraitId.TimeRefund) > 0f;

        if (Has(RelicId.HistoryShard))
            attack.AddOneShotBonus(R(RelicId.HistoryShard).value, R(RelicId.HistoryShard).duration);
    }

    void OnDodge()
    {
        if (Has(RelicId.CrackedPocketWatch))
            attack.AddOneShotBonus(R(RelicId.CrackedPocketWatch).value, R(RelicId.CrackedPocketWatch).duration);
        if (Has(RelicId.MoonShard))
            health.AddReduction(R(RelicId.MoonShard).value, R(RelicId.MoonShard).duration);
    }

    void OnHitLanded(Vector2 center, Vector2 size, int damage)
    {
        if (!refundPending || !rewind) return;
        refundPending = false;
        rewind.ReduceCooldown(T(TraitId.TimeRefund));
    }

    void Update()
    {
        for (int i = afterimages.Count - 1; i >= 0; i--)
        {
            var (at, center, size, damage) = afterimages[i];
            if (Time.time < at) continue;
            afterimages.RemoveAt(i);
            foreach (var hit in Physics2D.OverlapBoxAll(center, size, 0f))
                if (hit.gameObject != gameObject && hit.TryGetComponent(out Health target) && !target.IsDead)
                    target.TakeHit(damage, center);
            ShowAfterimage(center, size);
        }
    }

    void ShowAfterimage(Vector2 center, Vector2 size)
    {
        var sr = new GameObject("Afterimage").AddComponent<SpriteRenderer>();
        sr.sprite = RoomManager.Square;
        sr.color = AfterimageColor;
        sr.sortingOrder = 3;
        sr.transform.position = center;
        sr.transform.localScale = size;
        Destroy(sr.gameObject, afterimageShow);
    }
}
