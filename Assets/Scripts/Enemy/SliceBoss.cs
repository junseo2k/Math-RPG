using System;
using System.Collections.Generic;
using UnityEngine;

// 시험판 A 보스. 규칙: Docs/CombatDecisions.md 6·7장
// 역행 대응: 코루틴·Time.time 없이 물리 프레임마다 진행하고, 공격 판정(탄·낙하 지점)도 State에 담는다.
// 화면 표시는 State에서 매 프레임 다시 그리므로 Restore 후 따로 맞출 필요가 없다.
[RequireComponent(typeof(Health), typeof(SpriteRenderer), typeof(BoxCollider2D))]
public class SliceBoss : MonoBehaviour
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
        public int chargeDir;
        public float[] fallX;
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
    [SerializeField] float moveLimitX = 8.5f;

    [Header("탄막")]
    [SerializeField] int shotCount = 3;
    [SerializeField] float shotInterval = 0.6f;
    [SerializeField] float shotSpeed = 8f;
    [SerializeField] float shotRadius = 0.3f;
    [SerializeField] float shotLifetime = 3f;

    [Header("낙하 광역")]
    [SerializeField] int fallCount = 3;
    [SerializeField] float fallWidth = 2f;
    [SerializeField] float fallZoneHeight = 2f;
    [SerializeField] float fallImpact = 0.2f;
    [SerializeField] float fallLimitX = 9f;
    [SerializeField] float fallStartHeight = 8f;

    [Header("디버그")]
    [SerializeField] bool showDebug = true;

    static readonly Color TelegraphColor = new(1f, 0.3f, 0.2f, 0.25f);
    static readonly Color ActiveColor = new(1f, 0.2f, 0.1f, 0.6f);
    static readonly Color WindupColor = new(1f, 0.6f, 0.4f);
    static readonly Color ShotColor = new(1f, 0.85f, 0.4f);
    static readonly Color RockColor = new(0.6f, 0.5f, 0.45f);

    public Phase CurrentPhase => s.phase;
    public int DecisionId => s.decisionId;
    public Pattern? CurrentPattern => s.current >= 0 ? patterns[s.current].pattern : null;
    // 다음 결정에서 뺄 패턴(역행으로 취소된 공격). 스냅샷 밖 정보
    public int CancelledPattern => cancelled;

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
    SpriteRenderer[] rocks;
    SpriteRenderer[] shotViews;

    float HalfHeight => transform.localScale.y * 0.5f;
    float FloorY => s.position.y - HalfHeight;
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
        s.current = s.previous = -1;
        s.fallX = new float[fallCount];
        s.shots = new Shot[shotCount];
        EnterIdle();
    }

    void Start()
    {
        flow = FindAnyObjectByType<GameFlow>();
        player = FindAnyObjectByType<PlayerController>();

        fx = new GameObject("SliceBossFx").transform;
        fx.SetParent(transform.parent);
        zones = MakeViews("Zone", Mathf.Max(fallCount, 1), TelegraphColor, 1);
        rocks = MakeViews("Rock", fallCount, RockColor, 3);
        shotViews = MakeViews("Shot", shotCount, ShotColor, 3);
        UpdateViews();
    }

    void OnDestroy()
    {
        if (fx) Destroy(fx.gameObject);
    }

    public State Capture()
    {
        var c = s;
        c.fallX = (float[])s.fallX.Clone();
        c.shots = (Shot[])s.shots.Clone();
        return c;
    }

    // 복원 지점 뒤에 결정된 공격은 취소된 것: 다음 결정에서 제외 (CombatDecisions 7장)
    // 결정 프레임과 같은 지점이면 결정 번호가 같으므로 "결정 이후"로 남는다 (6장)
    public void Restore(State snap)
    {
        if (snap.decisionId < s.decisionId && s.current >= 0) cancelled = s.current;
        s = snap;
        s.fallX = (float[])snap.fallX.Clone();
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
        s.chargeDir = s.facing;

        // 낙하 지점은 결정 시 확정: 1곳은 플레이어 위치, 나머지는 무작위
        float px = player ? player.transform.position.x : 0f;
        for (int i = 0; i < s.fallX.Length; i++)
            s.fallX[i] = Mathf.Clamp(i == 0 ? px : UnityEngine.Random.Range(-fallLimitX, fallLimitX), -fallLimitX, fallLimitX);

        Enter(Phase.Telegraph);
    }

    void TickActive()
    {
        switch (P.pattern)
        {
            case Pattern.Slash:
                if (!s.hitDone) s.hitDone = HitPlayer(SlashCenter, slashSize);
                break;

            case Pattern.Charge:
                s.position.x = Mathf.Clamp(s.position.x + s.chargeDir * chargeSpeed * Time.fixedDeltaTime, -moveLimitX, moveLimitX);
                if (!s.hitDone) s.hitDone = HitPlayer(s.position, transform.localScale);
                break;

            case Pattern.Barrage:
                if (s.shotsFired < s.shots.Length && s.phaseTime >= s.shotsFired * shotInterval) Fire();
                break;

            case Pattern.Fall:
                if (s.hitDone || s.phaseTime < P.active - fallImpact) break;
                foreach (float x in s.fallX)
                    if (HitPlayer(FallCenter(x), new Vector2(fallWidth, fallZoneHeight)))
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
            if (shot.age >= shotLifetime || Mathf.Abs(shot.pos.x) > moveLimitX + 3f) shot.alive = false;
            else if (HitPlayerCircle(shot.pos, shotRadius)) shot.alive = false;
            s.shots[i] = shot;
        }
    }

    // 회피 중 겹쳐도 "판정 끝"으로 본다(회피 성공 이벤트가 프레임마다 반복되지 않게)
    bool HitPlayer(Vector2 center, Vector2 size)
    {
        foreach (var hit in Physics2D.OverlapBoxAll(center, size, 0f))
            if (hit.TryGetComponent(out PlayerController p))
            {
                p.Health.TakeHit(P.damage, s.position);
                return true;
            }
        return false;
    }

    bool HitPlayerCircle(Vector2 center, float radius)
    {
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

    Vector2 SlashCenter => new(s.position.x + s.facing * (transform.localScale.x * 0.5f + slashSize.x * 0.5f), FloorY + slashSize.y * 0.5f);
    Vector2 FallCenter(float x) => new(x, FloorY + fallZoneHeight * 0.5f);

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
            sr.sprite = RoomManager.Square;
            sr.color = color;
            sr.sortingOrder = order;
            sr.enabled = false;
            views[i] = sr;
        }
        return views;
    }

    static void Show(SpriteRenderer sr, Vector2 center, Vector2 size, Color color)
    {
        sr.enabled = true;
        sr.color = color;
        sr.transform.position = center;
        sr.transform.localScale = size;
    }

    void UpdateViews()
    {
        if (!fx) return;
        foreach (var z in zones) z.enabled = false;
        foreach (var r in rocks) r.enabled = false;
        body.color = baseColor;
        if (anim) anim.Face(s.facing);

        bool telegraph = s.phase == Phase.Telegraph;
        if (s.current >= 0 && (telegraph || s.phase == Phase.Active))
        {
            Color zone = telegraph ? TelegraphColor : ActiveColor;
            switch (P.pattern)
            {
                case Pattern.Slash:
                    Show(zones[0], SlashCenter, slashSize, zone);
                    break;
                case Pattern.Charge:
                    if (!telegraph) break;
                    float end = s.chargeDir * moveLimitX;
                    Show(zones[0], new Vector2((s.position.x + end) * 0.5f, s.position.y),
                        new Vector2(Mathf.Abs(end - s.position.x) + transform.localScale.x, transform.localScale.y), zone);
                    break;
                case Pattern.Barrage:
                    if (telegraph) body.color = WindupColor;
                    break;
                case Pattern.Fall:
                    for (int i = 0; i < s.fallX.Length && i < zones.Length; i++)
                    {
                        Show(zones[i], FallCenter(s.fallX[i]), new Vector2(fallWidth, fallZoneHeight), zone);
                        if (telegraph) continue;
                        float k = Mathf.Clamp01(s.phaseTime / Mathf.Max(P.active - fallImpact, 0.01f));
                        Show(rocks[i], new Vector2(s.fallX[i], Mathf.Lerp(FloorY + fallStartHeight, FloorY + 0.5f, k)), new Vector2(fallWidth * 0.8f, 1f), RockColor);
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
        string excluded = cancelled >= 0 ? patterns[cancelled].pattern.ToString() : "-";
        GUI.Label(new Rect(p.x - 150f, Screen.height - p.y - 10f, 300f, 20f),
            $"{s.phase} {pattern} {s.phaseTime:0.00}s  결정#{s.decisionId}  제외:{excluded}");
    }
}
