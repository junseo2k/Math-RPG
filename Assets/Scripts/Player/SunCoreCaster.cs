using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// 태양신 고유 능력 태양핵: 소환 입력, 소멸 후 쿨타임, 최대 개수, 범위 버프 적용
// 수치는 모두 SolarCoreSettings에서 매 프레임 읽는다 (플레이 중 수정도 즉시 반영)
[RequireComponent(typeof(PlayerController), typeof(PlayerAttack), typeof(Health))]
public class SunCoreCaster : MonoBehaviour
{
    // 플레이어에 없으면 플레이 시작 시 자동 부착
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap() => GameScene.WhenLoaded(() =>
    {
        foreach (var p in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
            if (!p.GetComponent<SunCoreCaster>()) p.gameObject.AddComponent<SunCoreCaster>();
    });

    [SerializeField] bool showDebug = true;

    // 태양의 성좌 충전 요청(0.05 = 5%). 일출 게이지 구현 시 여기에 연결 (SolarCoreSpec M2)
    public static event Action<float> SunriseChargeRequested;

    PlayerController controller;
    PlayerAttack attack;
    Health health;
    GameFlow flow;
    readonly List<SunCore> cores = new();
    readonly List<SunCore> afterglows = new();
    float readyAt;
    float failShownUntil;
    float lastConstellationAt = float.NegativeInfinity;

    public bool InZone { get; private set; }

    // 특성·유물 보유 여부. RewardEffects.Recalc가 설정한다 (SolarCoreSpec 3·4장, 조합 규칙 M8)
    public bool SolarWind { get; set; }
    public bool WideExpansion { get; set; }
    public bool LingeringSun { get; set; }
    public bool Wandering { get; set; }
    public bool DelayedRelease { get; set; }
    public bool Sundial { get; set; }
    public bool Constellation { get; set; }

    static SolarCoreSettings S => SolarCoreSettings.Instance;

    float Radius => (Wandering ? S.wanderingSun.radius : S.basic.radius) * (1f + (WideExpansion ? S.wideExpansion.radiusBonus : 0f));
    float Lifetime => S.basic.lifetime + (LingeringSun ? S.lingeringSun.extraLifetime : 0f);
    float MoveBonus => (Wandering ? S.wanderingSun.moveSpeedBonus : S.basic.moveSpeedBonus) + (SolarWind ? S.solarWind.extraMoveSpeedBonus : 0f);
    float AttackBonus => Wandering ? S.wanderingSun.attackBonus : S.basic.attackBonus;
    float Reduction => Wandering ? S.wanderingSun.damageReduction : S.basic.damageReduction;
    // 유랑하는 태양을 얻으면 성좌 비활성 (유물은 보유 유지)
    bool ConstellationActive => Constellation && !Wandering;

    void Awake()
    {
        controller = GetComponent<PlayerController>();
        attack = GetComponent<PlayerAttack>();
        health = GetComponent<Health>();
    }

    void Start()
    {
        flow = FindAnyObjectByType<GameFlow>();
        if (!flow) return;
        flow.RunReset += OnRunReset;
        flow.StageChanged += OnStageChanged;
    }

    void OnDestroy()
    {
        if (!flow) return;
        flow.RunReset -= OnRunReset;
        flow.StageChanged -= OnStageChanged;
    }

    void Update()
    {
        // 소멸한 태양핵이 있으면 그때부터 쿨타임 (잔광은 쿨타임과 무관)
        if (cores.RemoveAll(c => !c) > 0) readyAt = Time.time + S.basic.cooldown;
        afterglows.RemoveAll(a => !a);

        var kb = Keyboard.current;
        if (kb != null && kb.fKey.wasPressedThisFrame && controller.CanAct && cores.Count < S.basic.maxCount && Time.time >= readyAt)
        {
            // 동반형은 플레이어 위치에서 따라다님. 실패 시 쿨타임 미소모
            Vector2? point = Wandering ? (Vector2)transform.position : FindSpawnPoint();
            if (point is Vector2 p)
            {
                var core = SunCore.Spawn(p, Radius, Lifetime, Wandering ? transform : null);
                core.Expired += OnCoreExpired;
                // 소환 순간 이미 안에 있으면 진입으로 치지 않음 (M11)
                core.PlayerInside = core.Contains(transform.position);
                cores.Add(core);
                SoundPlayer.Play("sfx_sun_core");
            }
            else failShownUntil = Time.time + 1f;
        }

        // 태양핵 범위 우선, 밖이면 잔광. 각각 여러 개가 겹쳐도 중첩하지 않음 (M4)
        bool inCore = false;
        foreach (var c in cores)
        {
            bool inside = c.Contains(transform.position);
            if (inside && !c.PlayerInside) TryConstellation(c);
            c.PlayerInside = inside;
            inCore |= inside;
        }
        bool inAfterglow = false;
        if (!inCore && Sundial)
            foreach (var a in afterglows)
                if (a.Contains(transform.position)) { inAfterglow = true; break; }

        float eff = S.brokenSundial.efficiency;
        if (inCore) SetZone(MoveBonus, AttackBonus, Reduction);
        else if (inAfterglow) SetZone(0f, AttackBonus * eff, Reduction * eff);
        else SetZone(0f, 0f, 0f);
        InZone = inCore;
    }

    // 범위 밖 → 안 진입. 태양핵당 최대 횟수, 전체 공통 최소 간격
    void TryConstellation(SunCore core)
    {
        var c = S.solarConstellation;
        if (!ConstellationActive || RewindSystem.IsFrozen) return;
        if (core.ConstellationCount >= c.maxPerCore || Time.time < lastConstellationAt + c.minInterval) return;
        core.ConstellationCount++;
        lastConstellationAt = Time.time;
        SunriseChargeRequested?.Invoke(c.chargeAmount);
    }

    // 조준 방향 앞 → 막히면 플레이어 위치 → 그것도 무효면 null (SolarCoreSpec 2장)
    // ponytail: 지형 = Health 없는 비트리거 콜라이더. M9(막힘·무효 정의) 확정 시 레이어 기준으로 교체
    Vector2? FindSpawnPoint()
    {
        Vector2 origin = transform.position;
        Vector2 ahead = origin + controller.Aim * S.basic.spawnDistance;
        bool blocked = InTerrain(ahead);
        foreach (var hit in Physics2D.LinecastAll(origin, ahead))
            if (IsTerrain(hit.collider)) { blocked = true; break; }
        if (!blocked) return ahead;
        return InTerrain(origin) ? null : origin;
    }

    static bool IsTerrain(Collider2D c) => !c.isTrigger && !c.GetComponentInParent<Health>();

    static bool InTerrain(Vector2 point)
    {
        foreach (var c in Physics2D.OverlapPointAll(point))
            if (IsTerrain(c)) return true;
        return false;
    }

    void SetZone(float move, float damage, float reduction)
    {
        controller.ZoneMoveBonus = move;
        attack.ZoneDamageBonus = damage;
        health.ZoneReduction = reduction;
    }

    void OnCoreExpired(SunCore core)
    {
        Vector2 at = core.transform.position;

        // 지연 방출: 소멸 시점 범위 안의 적 감속. 정지장과 같은 IFreezable 대상(일반 적)만
        if (DelayedRelease)
            foreach (var hit in Physics2D.OverlapCircleAll(at, core.Radius))
                if (hit.TryGetComponent(out IFreezable target))
                    target.Slow(S.delayedRelease.slowRatio, S.delayedRelease.duration);

        // 부서진 해시계: 소멸 위치에 고정된 잔광 (동반형이어도 따라오지 않음)
        if (Sundial)
            afterglows.Add(SunCore.Spawn(at, core.Radius, S.brokenSundial.duration, afterglow: true));
    }

    void ClearCores()
    {
        foreach (var c in cores)
            if (c) Destroy(c.gameObject);
        foreach (var a in afterglows)
            if (a) Destroy(a.gameObject);
        cores.Clear();
        afterglows.Clear();
        SetZone(0f, 0f, 0f);
        InZone = false;
    }

    // 방 이동 시 제거. 소멸이 아니므로 지연 방출·잔광 없음 (M10). 쿨타임은 시작
    void OnStageChanged(int _)
    {
        bool hadCore = cores.Count > 0;
        ClearCores();
        if (hadCore) readyAt = Time.time + S.basic.cooldown;
    }

    void OnRunReset()
    {
        ClearCores();
        readyAt = 0f;
        lastConstellationAt = float.NegativeInfinity;
    }

    void OnGUI()
    {
        if (!showDebug) return;
        string state = Time.time < failShownUntil ? "소환 불가" : cores.Count > 0 ? (InZone ? "범위 안" : "설치됨")
            : Time.time >= readyAt ? "[F] 준비" : $"{readyAt - Time.time:0.0}초";
        string mode = (Wandering ? "동반형" : "설치형") + (DelayedRelease ? "+지연" : "");
        string relics = (Sundial ? $"  잔광 {afterglows.Count}" : "")
            + (!Constellation ? "" : !ConstellationActive ? "  성좌 비활성(유랑)"
                : cores.Count > 0 ? $"  성좌 {cores[0].ConstellationCount}/{S.solarConstellation.maxPerCore}" : "  성좌 대기");
        GUI.Label(new Rect(10, Screen.height - 130, 760, 20),
            $"태양핵 {state}  [{mode} 반경 {Radius:0.0}m 유지 {Lifetime:0.#}초 이동 +{MoveBonus:0.#%} 공격 +{AttackBonus:0.#%} 방어 +{Reduction:0.#%}]{relics}");
    }
}
