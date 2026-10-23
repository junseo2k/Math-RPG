using System;
using System.Collections.Generic;
using UnityEngine;

// 시험판 A 보스. 규칙: Docs/SliceA/CombatDecisions.md 6·7장
// 역행 대응: 코루틴·Time.time 없이 물리 프레임마다 진행하고, 공격 판정(탄·낙하 지점)도 State에 담는다.
// 화면 표시는 State에서 매 프레임 다시 그리므로 Restore 후 따로 맞출 필요가 없다.
[RequireComponent(typeof(Health), typeof(SpriteRenderer), typeof(BoxCollider2D))]
public class SliceBoss : MonoBehaviour, IRewindable
{
    public enum Phase { Idle, Telegraph, Active, Recover }
    public enum Pattern { Slash, Charge, Barrage, Fall }

    [Serializable]
    public class PatternSettings
    {
        public Pattern pattern;
        [Min(0f)] public float weight = 1f;
        public float telegraph;
        public float active;
        public float recover = 0.6f;
        public int damage;
    }

    public struct Shot
    {
        public Vector2 pos;
        public Vector2 vel;
        public float age;
        public bool alive;
    }

    // 역행 스냅샷 대상. HP는 Health 쪽에서 따로 복원한다
    public struct State
    {
        public Vector2 position;
        public int facing;
        public Phase phase;
        public float phaseTime;
        public float idleLength;
        public int decisionId;
        public int current;   // patterns 인덱스, -1 = 없음
        public int previous;
        // 결정 순간 플레이어 쪽 단위 벡터. 내려베기·돌진 방향
        public Vector2 aimDir;
        public Vector2[] fallPos;
        public int shotsFired;
        public Shot[] shots;
        public bool hitDone;
    }

    [SerializeField] int maxHp = 2000;
    [SerializeField] Vector2 idleRange = new(0.8f, 1.5f);
    [SerializeField] bool excludePrevious = true;
    [SerializeField] bool excludeCancelled = true;
    [SerializeField] List<PatternSettings> patterns = new()
    {
        new() { pattern = Pattern.Slash, telegraph = 0.6f, active = 0.2f, damage = 15 },
        new() { pattern = Pattern.Charge, telegraph = 0.8f, active = 1f, damage = 20 },
        new() { pattern = Pattern.Barrage, telegraph = 0.6f, active = 1.8f, damage = 12 },
        new() { pattern = Pattern.Fall, telegraph = 1.2f, active = 1.5f, damage = 25 },
    };

    [Header("내려베기")]
    [SerializeField] Vector2 slashSize = new(3f, 2f);

    [Header("돌진")]
    [SerializeField] float chargeSpeed = 18f;
    // 방 중심(0,0) 기준 이동 가능 범위
    [SerializeField] float moveLimitX = 8.5f;
    [SerializeField] float moveLimitY = 5f;

    [Header("탄막")]
    [SerializeField] int shotCount = 3;
    [SerializeField] float shotInterval = 0.6f;
    [SerializeField] float shotSpeed = 8f;
    [SerializeField] float shotRadius = 0.3f;
    [SerializeField] float shotLifetime = 3f;

    [Header("낙하 광역")]
    [SerializeField] int fallCount = 3;
    [SerializeField] float fallWidth = 2f;
    [SerializeField] float fallImpact = 0.2f;
    [SerializeField] float fallLimitX = 9f;
    [SerializeField] float fallLimitY = 5.5f;
    [SerializeField] float fallStartHeight = 8f;

    // 표시 전용. 판정은 위 수치로만 정해지고, 표시는 같은 계산 결과를 그린다
    [Header("표시")]
    [SerializeField] Color telegraphOuterColor = new(1f, 0.25f, 0.2f, 0.15f);
    [SerializeField] Color telegraphFillColor = new(1f, 0.25f, 0.2f, 0.35f);
    [SerializeField] Color activeColor = new(1f, 0.15f, 0.1f, 0.65f);
    [SerializeField] Color fallingColor = new(1f, 0.55f, 0.2f, 0.4f);
    [SerializeField] Color windupBodyColor = new(1f, 0.6f, 0.4f);
    [SerializeField] Color activeBodyColor = new(1f, 0.3f, 0.3f);
    [SerializeField] Color aimColor = new(1f, 0.85f, 0.4f, 0.35f);
    [SerializeField] float fireFlashTime = 0.1f;

    [Header("디버그")]
    [SerializeField] bool showDebug = true;
    // 실제 판정 검사 위치를 잠깐 표시(표시 범위와 비교용)
    [SerializeField] bool showHitQueries;

    static readonly Color ShotColor = new(1f, 0.85f, 0.4f);
    static readonly Color RockColor = new(0.6f, 0.5f, 0.45f);
    static readonly Color QueryColor = new(0.2f, 1f, 1f, 0.5f);

    public Phase CurrentPhase => s.phase;
    public int DecisionId => s.decisionId;
    public Pattern? CurrentPattern => s.current >= 0 ? patterns[s.current].pattern : null;
    // 다음 결정에서 뺄 패턴(역행으로 취소된 공격). 스냅샷 밖 정보
    public int CancelledPattern => cancelled;
    public string CancelledPatternName => cancelled >= 0 ? patterns[cancelled].pattern.ToString() : "-";
    // 특성 "반복되는 운명": 이 결정 번호의 공격은 실행 범위를 EarlyWarning초 일찍 표시
    public int RepeatDecisionId { get; set; } = -1;
    public float EarlyWarning { get; set; }

    State s;
    int cancelled = -1;
    Health health;
    SpriteRenderer body;
    SpriteAnimator anim;
    Color baseColor;
    GameFlow flow;
    PlayerController player;
    Transform fx;
    SpriteRenderer[] zones;
    SpriteRenderer[] fills;
    SpriteRenderer[] rocks;
    SpriteRenderer[] shotViews;
    SpriteRenderer aim;

    float HalfHeight => transform.localScale.y * 0.5f;
    float AimAngle => Mathf.Atan2(s.aimDir.y, s.aimDir.x) * Mathf.Rad2Deg;
    Vector2 FallSize => Vector2.one * fallWidth;
    PatternSettings P => patterns[s.current];

    void Awake()
    {
        health = GetComponent<Health>();
        health.Init(maxHp, 0f);
        health.Died += OnDied;
        body = GetComponent<SpriteRenderer>();
        baseColor = body.color;
        anim = GetComponent<SpriteAnimator>();

        s.position = transform.position;
        s.facing = -1;
        s.aimDir = Vector2.left;
        s.current = s.previous = -1;
        s.fallPos = new Vector2[fallCount];
        s.shots = new Shot[shotCount];
        EnterIdle();
        Rewindables.All.Add(this);
    }

    void Start()
    {
        flow = FindAnyObjectByType<GameFlow>();
        player = FindAnyObjectByType<PlayerController>();

        fx = new GameObject("SliceBossFx").transform;
        fx.SetParent(transform.parent);
        zones = MakeViews("Zone", Mathf.Max(fallCount, 1), telegraphOuterColor, 1);
        fills = MakeViews("Fill", Mathf.Max(fallCount, 1), telegraphFillColor, 2);
        rocks = MakeViews("Rock", fallCount, RockColor, 3);
        shotViews = MakeViews("Shot", shotCount, ShotColor, 3);
        aim = MakeViews("Aim", 1, aimColor, 2)[0];
        UpdateViews();
    }

    void OnDestroy()
    {
        Rewindables.All.Remove(this);
        if (fx) Destroy(fx.gameObject);
    }

    object IRewindable.Capture() => Capture();
    void IRewindable.Restore(object state) => Restore((State)state);

    public State Capture()
    {
        var c = s;
        c.fallPos = (Vector2[])s.fallPos.Clone();
        c.shots = (Shot[])s.shots.Clone();
        return c;
    }

    // 복원 지점 뒤에 결정된 공격은 취소된 것: 다음 결정에서 제외 (CombatDecisions 7장)
    // 결정 프레임과 같은 지점이면 결정 번호가 같으므로 "결정 이후"로 남는다 (6장)
    public void Restore(State snap)
    {
        if (snap.decisionId < s.decisionId && s.current >= 0) cancelled = s.current;
        s = snap;
        s.fallPos = (Vector2[])snap.fallPos.Clone();
        s.shots = (Shot[])snap.shots.Clone();
        transform.position = s.position;
        UpdateViews();
    }

    void FixedUpdate()
    {
        if (health.IsDead || !flow || flow.State != GameState.Playing) return;
        if (!player) player = FindAnyObjectByType<PlayerController>();
        float dt = Time.fixedDeltaTime;

        TickShots(dt);
        s.phaseTime += dt;
        switch (s.phase)
        {
            case Phase.Idle:
                FacePlayer();
                if (s.phaseTime >= s.idleLength) Decide();
                break;
            case Phase.Telegraph:
                if (s.phaseTime >= P.telegraph) Enter(Phase.Active);
                break;
            case Phase.Active:
                TickActive();
                if (s.phaseTime >= P.active) Enter(Phase.Recover);
                break;
            case Phase.Recover:
                FacePlayer();
                if (s.phaseTime >= P.recover) EnterIdle();
                break;
        }
        transform.position = s.position;
    }

    void LateUpdate() => UpdateViews();

    void Enter(Phase phase)
    {
        s.phase = phase;
        s.phaseTime = 0f;
    }

    void EnterIdle()
    {
        Enter(Phase.Idle);
        s.idleLength = UnityEngine.Random.Range(idleRange.x, idleRange.y);
    }

    // 예고 시작 순간 = 결정 시점 (CombatDecisions 6장)
    void Decide()
    {
        var candidates = new List<int>();
        for (int i = 0; i < patterns.Count; i++)
        {
            if (patterns[i].weight <= 0f) continue;
            if (excludePrevious && i == s.current) continue;
            if (excludeCancelled && i == cancelled) continue;
            candidates.Add(i);
        }
        if (candidates.Count == 0)
            for (int i = 0; i < patterns.Count; i++) candidates.Add(i);

        float total = 0f;
        foreach (int i in candidates) total += patterns[i].weight;
        float roll = UnityEngine.Random.value * total;
        int pick = candidates[^1];
        foreach (int i in candidates)
        {
            roll -= patterns[i].weight;
            if (roll < 0f) { pick = i; break; }
        }

        cancelled = -1;
        s.previous = s.current;
        s.current = pick;
        s.decisionId++;
        s.hitDone = false;
        s.shotsFired = 0;
        FacePlayer();
        Vector2 to = player ? (Vector2)player.transform.position - s.position : Vector2.right * s.facing;
        s.aimDir = to.sqrMagnitude > 0.0001f ? to.normalized : Vector2.right * s.facing;

        // 낙하 지점은 결정 시 확정: 1곳은 플레이어 위치, 나머지는 무작위
        Vector2 pp = player ? (Vector2)player.transform.position : Vector2.zero;
        for (int i = 0; i < s.fallPos.Length; i++)
        {
            Vector2 p = i == 0 ? pp : new Vector2(UnityEngine.Random.Range(-fallLimitX, fallLimitX), UnityEngine.Random.Range(-fallLimitY, fallLimitY));
            s.fallPos[i] = new Vector2(Mathf.Clamp(p.x, -fallLimitX, fallLimitX), Mathf.Clamp(p.y, -fallLimitY, fallLimitY));
        }

        Enter(Phase.Telegraph);
    }

    void TickActive()
    {
        switch (P.pattern)
        {
            case Pattern.Slash:
                if (!s.hitDone) s.hitDone = HitPlayer(SlashCenter, slashSize, AimAngle);
                break;

            case Pattern.Charge:
                s.position = ClampToRoom(s.position + s.aimDir * chargeSpeed * Time.fixedDeltaTime);
                if (!s.hitDone) s.hitDone = HitPlayer(s.position, transform.localScale);
                break;

            case Pattern.Barrage:
                if (s.shotsFired < s.shots.Length && s.phaseTime >= s.shotsFired * shotInterval) Fire();
                break;

            case Pattern.Fall:
                if (s.hitDone || s.phaseTime < P.active - fallImpact) break;
                foreach (Vector2 p in s.fallPos)
                    if (HitPlayer(p, FallSize))
                    {
                        s.hitDone = true;
                        break;
                    }
                break;
        }
    }

    // 발사 순간 플레이어 조준
    void Fire()
    {
        Vector2 target = player ? (Vector2)player.transform.position : s.position + Vector2.right * s.facing;
        Vector2 dir = (target - s.position).normalized;
        if (dir == Vector2.zero) dir = Vector2.right * s.facing;
        s.shots[s.shotsFired] = new Shot { pos = s.position, vel = dir * shotSpeed, alive = true };
        s.shotsFired++;
        SoundPlayer.Play("sfx_rev_wave");
    }

    // 탄은 단계와 무관하게 수명까지 날아간다(회복 단계에도 판정 유지)
    void TickShots(float dt)
    {
        for (int i = 0; i < s.shots.Length; i++)
        {
            var shot = s.shots[i];
            if (!shot.alive) continue;
            shot.pos += shot.vel * dt;
            shot.age += dt;
            if (shot.age >= shotLifetime || Mathf.Abs(shot.pos.x) > moveLimitX + 3f || Mathf.Abs(shot.pos.y) > moveLimitY + 3f) shot.alive = false;
            else if (HitPlayerCircle(shot.pos, shotRadius)) shot.alive = false;
            s.shots[i] = shot;
        }
    }

    // 회피 중 겹쳐도 "판정 끝"으로 본다(회피 성공 이벤트가 프레임마다 반복되지 않게)
    bool HitPlayer(Vector2 center, Vector2 size, float angle = 0f)
    {
        if (showHitQueries) ShowQuery(center, size);
        foreach (var hit in Physics2D.OverlapBoxAll(center, size, angle))
            if (hit.TryGetComponent(out PlayerController p))
            {
                p.Health.TakeHit(P.damage, s.position);
                return true;
            }
        return false;
    }

    bool HitPlayerCircle(Vector2 center, float radius)
    {
        if (showHitQueries) ShowQuery(center, Vector2.one * radius * 2f);
        foreach (var hit in Physics2D.OverlapCircleAll(center, radius))
            if (hit.TryGetComponent(out PlayerController p))
            {
                p.Health.TakeHit(DamageOf(Pattern.Barrage), center);
                return true;
            }
        return false;
    }

    int DamageOf(Pattern pattern)
    {
        foreach (var p in patterns)
            if (p.pattern == pattern) return p.damage;
        return 0;
    }

    void FacePlayer()
    {
        if (player) s.facing = player.transform.position.x >= s.position.x ? 1 : -1;
    }

    Vector2 SlashCenter => s.position + s.aimDir * (transform.localScale.x * 0.5f + slashSize.x * 0.5f);
    Vector2 ClampToRoom(Vector2 p) => new(Mathf.Clamp(p.x, -moveLimitX, moveLimitX), Mathf.Clamp(p.y, -moveLimitY, moveLimitY));

    // 돌진 방향으로 방 경계에 닿는 지점
    Vector2 ChargeEnd()
    {
        float t = float.PositiveInfinity;
        if (s.aimDir.x != 0f) t = Mathf.Min(t, ((s.aimDir.x > 0f ? moveLimitX : -moveLimitX) - s.position.x) / s.aimDir.x);
        if (s.aimDir.y != 0f) t = Mathf.Min(t, ((s.aimDir.y > 0f ? moveLimitY : -moveLimitY) - s.position.y) / s.aimDir.y);
        return ClampToRoom(s.position + s.aimDir * Mathf.Max(t, 0f));
    }

    void OnDied()
    {
        for (int i = 0; i < s.shots.Length; i++) s.shots[i].alive = false;
        if (flow) flow.ClearStage();
        Destroy(gameObject);
    }

    SpriteRenderer[] MakeViews(string name, int count, Color color, int order)
    {
        var views = new SpriteRenderer[count];
        for (int i = 0; i < count; i++)
        {
            var sr = new GameObject($"{name}{i}").AddComponent<SpriteRenderer>();
            sr.transform.SetParent(fx);
            // 이펙트 그림이 생기면 자동 교체, 없으면 1유닛 사각형
            sr.sprite = AssetLibrary.SpriteUnitsOr($"fx_boss_{name.ToLowerInvariant()}", 1f, RoomManager.Square);
            sr.color = color;
            sr.sortingOrder = order;
            sr.enabled = false;
            views[i] = sr;
        }
        return views;
    }

    static void Show(SpriteRenderer sr, Vector2 center, Vector2 size, Color color, float angle = 0f)
    {
        sr.enabled = true;
        sr.color = color;
        sr.transform.position = center;
        sr.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        sr.transform.localScale = size;
    }

    // 판정 전: 옅은 바깥 + 판정 순간(hitAt)에 꽉 차는 채움. 판정 중: 진한 실행 색
    // "반복되는 운명": 다시 겪는 공격은 EarlyWarning초 일찍 실행 색으로 표시
    void ShowWarning(int i, Vector2 center, Vector2 size, float since, float hitAt, Color? fill = null, float angle = 0f)
    {
        if (since >= hitAt)
        {
            Show(zones[i], center, size, activeColor, angle);
            return;
        }
        bool early = s.decisionId == RepeatDecisionId && since >= hitAt - EarlyWarning;
        Show(zones[i], center, size, telegraphOuterColor, angle);
        float k = hitAt > 0f ? Mathf.Clamp01(since / hitAt) : 1f;
        Show(fills[i], center, size * k, early ? activeColor : fill ?? telegraphFillColor, angle);
    }

    // 탄막: 남은 탄이 있는 동안 발사 방향(= 현재 플레이어 쪽) 조준선. 발사 직후 잠깐 실행 색
    void ShowAim()
    {
        if (s.shotsFired >= s.shots.Length || !player) return;
        Vector2 to = (Vector2)player.transform.position - s.position;
        if (to.sqrMagnitude < 0.0001f) return;
        bool flash = s.phase == Phase.Active && s.shotsFired > 0 &&
                     s.phaseTime - (s.shotsFired - 1) * shotInterval < fireFlashTime;
        float length = to.magnitude;
        aim.enabled = true;
        aim.color = flash ? activeColor : aimColor;
        aim.transform.position = s.position + to * 0.5f;
        aim.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(to.y, to.x) * Mathf.Rad2Deg);
        aim.transform.localScale = new Vector2(length, shotRadius * 2f);
    }

    void ShowQuery(Vector2 center, Vector2 size)
    {
        var sr = new GameObject("HitQuery").AddComponent<SpriteRenderer>();
        sr.sprite = RoomManager.Square;
        sr.color = QueryColor;
        sr.sortingOrder = 5;
        sr.transform.position = center;
        sr.transform.localScale = size;
        Destroy(sr.gameObject, 0.06f);
    }

    // 표시는 전부 State에서 계산 → 역행 Restore 직후·정지 연출 중에도 실제 상태와 일치
    void UpdateViews()
    {
        if (!fx) return;
        foreach (var z in zones) z.enabled = false;
        foreach (var f in fills) f.enabled = false;
        foreach (var r in rocks) r.enabled = false;
        aim.enabled = false;
        body.color = baseColor;
        if (anim) anim.Face(s.facing);

        bool telegraph = s.phase == Phase.Telegraph;
        if (s.current >= 0 && (telegraph || s.phase == Phase.Active))
        {
            body.color = telegraph ? windupBodyColor : activeBodyColor;
            // 결정 이후 경과 시간
            float since = telegraph ? s.phaseTime : P.telegraph + s.phaseTime;
            switch (P.pattern)
            {
                case Pattern.Slash:
                    // 실행 단계 전체가 판정 구간
                    ShowWarning(0, SlashCenter, slashSize, since, P.telegraph, angle: AimAngle);
                    break;
                case Pattern.Charge:
                    // 예고: 돌진 경로. 실행: 보스 몸이 판정(몸 색 = activeBodyColor)
                    if (!telegraph) break;
                    Vector2 end = ChargeEnd();
                    ShowWarning(0, (s.position + end) * 0.5f,
                        new Vector2(Vector2.Distance(s.position, end) + transform.localScale.x, transform.localScale.y), since, P.telegraph, angle: AimAngle);
                    break;
                case Pattern.Barrage:
                    ShowAim();
                    break;
                case Pattern.Fall:
                    // 판정은 착지 구간(실행 마지막 fallImpact초)뿐
                    float impactAt = P.telegraph + P.active - fallImpact;
                    for (int i = 0; i < s.fallPos.Length && i < zones.Length; i++)
                    {
                        ShowWarning(i, s.fallPos[i], FallSize, since, impactAt, telegraph ? telegraphFillColor : fallingColor);
                        if (telegraph) continue;
                        // 탑다운: 바위가 화면 위쪽에서 착지 지점으로 내려옴
                        float k = Mathf.Clamp01(s.phaseTime / Mathf.Max(P.active - fallImpact, 0.01f));
                        Show(rocks[i], s.fallPos[i] + Vector2.up * Mathf.Lerp(fallStartHeight, 0f, k), new Vector2(fallWidth * 0.8f, 1f), RockColor);
                    }
                    break;
            }
        }

        for (int i = 0; i < shotViews.Length; i++)
        {
            bool alive = i < s.shots.Length && s.shots[i].alive;
            shotViews[i].enabled = alive;
            if (alive)
            {
                shotViews[i].transform.position = s.shots[i].pos;
                shotViews[i].transform.localScale = Vector3.one * shotRadius * 2f;
            }
        }
    }

    void OnGUI()
    {
        if (!showDebug || health.IsDead) return;
        var cam = Camera.main;
        if (!cam) return;
        Vector3 p = cam.WorldToScreenPoint(transform.position + Vector3.up * (HalfHeight + 0.8f));
        string pattern = CurrentPattern?.ToString() ?? "-";
        GUI.Label(new Rect(p.x - 150f, Screen.height - p.y - 10f, 300f, 20f),
            $"{s.phase} {pattern} {s.phaseTime:0.00}s  결정#{s.decisionId}  제외:{CancelledPatternName}");
    }
}
