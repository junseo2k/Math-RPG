using UnityEngine;

// 1장 보스1: 시간 축적. 규칙: Docs/Bosses/01_Accumulation.md
[RequireComponent(typeof(Health), typeof(SpriteRenderer), typeof(BoxCollider2D))]
public class AccumulationBoss : MonoBehaviour, IRewindable
{
    enum Phase { Accumulating, Windup, Charging, Warning, Exhausted }

    struct Snapshot
    {
        public Vector3 position;
        public Phase phase;
        public int stacks;
        public float stackLeft, chargeLeft, phaseLeft, traveled;
        public Vector2 chargeDir;
        public bool hit;
    }

    // 전투 수치는 CombatBalance (Tools/전투 밸런스 열기)
    static CombatBalance.AccumulationBossStats B => CombatBalance.Instance.accumulationBoss;

    // 방 중심(0,0) 기준 이동 가능 범위
    [SerializeField] float moveLimitX = 8.5f;
    [SerializeField] float moveLimitY = 5f;

    [Header("축적 게이지 (머리 위)")]
    [SerializeField] float pipSize = 0.3f;
    [SerializeField] float pipGap = 0.1f;
    [SerializeField] Color pipFull = new(0.75f, 0.85f, 1f);
    [SerializeField] Color pipEmpty = new(0.2f, 0.22f, 0.3f, 0.8f);

    public int Stacks { get; private set; }

    SpriteRenderer[] pips;

    static Sprite circleSprite;

    Health health;
    SpriteAnimator anim;
    SpriteRenderer body;
    BoxCollider2D box;
    Color baseColor;
    GameFlow flow;
    PlayerController player;
    Phase phase;
    float stackLeft;
    float chargeLeft;
    float phaseLeft;
    float traveled;
    Vector2 chargeDir;
    bool hit;

    void Awake()
    {
        health = GetComponent<Health>();
        health.Init(B.maxHp, 0f);
        health.Died += OnDied;
        body = GetComponent<SpriteRenderer>();
        box = GetComponent<BoxCollider2D>();
        baseColor = body.color;
        anim = GetComponent<SpriteAnimator>();
        BeginCycle();
        Rewindables.All.Add(this);
    }

    void Start()
    {
        flow = FindAnyObjectByType<GameFlow>();
        player = FindAnyObjectByType<PlayerController>();
    }

    public object Capture() => new Snapshot
    {
        position = transform.position,
        phase = phase,
        stacks = Stacks,
        stackLeft = stackLeft,
        chargeLeft = chargeLeft,
        phaseLeft = phaseLeft,
        traveled = traveled,
        chargeDir = chargeDir,
        hit = hit,
    };

    public void Restore(object state)
    {
        var s = (Snapshot)state;
        transform.position = s.position;
        phase = s.phase;
        Stacks = s.stacks;
        stackLeft = s.stackLeft;
        chargeLeft = s.chargeLeft;
        phaseLeft = s.phaseLeft;
        traveled = s.traveled;
        chargeDir = s.chargeDir;
        hit = s.hit;
        if (anim) anim.Release();
        UpdateView();
    }

    void BeginCycle()
    {
        phase = Phase.Accumulating;
        Stacks = 0;
        stackLeft = B.stackInterval;
        chargeLeft = B.chargeInterval;
    }

    void Update()
    {
        if (health.IsDead || !flow || flow.State != GameState.Playing || TimeControl.IsStopped) return;
        float dt = Time.deltaTime;
        if (phase is Phase.Accumulating or Phase.Windup or Phase.Charging)
        {
            stackLeft -= dt;
            while (stackLeft <= 0f && Stacks < B.maxStacks)
            {
                Stacks++;
                SoundPlayer.Play("sfx_acc_stack");
                stackLeft += Mathf.Max(B.stackInterval, 0.0001f);
            }
        }

        phaseLeft -= dt;
        switch (phase)
        {
            case Phase.Accumulating:
                chargeLeft -= dt;
                if (Stacks >= B.maxStacks)
                {
                    phase = Phase.Warning;
                    phaseLeft = B.releaseWarning;
                    SoundPlayer.Play("sfx_acc_warning");
                }
                else if (chargeLeft <= 0f && player)
                {
                    phase = Phase.Windup;
                    phaseLeft = B.chargeWindup;
                    SoundPlayer.Play("sfx_acc_windup");
                }
                break;
            case Phase.Windup:
                if (phaseLeft > 0f) break;
                chargeDir = player ? (Vector2)(player.transform.position - transform.position) : Vector2.right;
                chargeDir = chargeDir.sqrMagnitude > 0.0001f ? chargeDir.normalized : Vector2.right;
                traveled = 0f;
                hit = false;
                phase = Phase.Charging;
                SoundPlayer.Play("sfx_acc_charge");
                break;
            case Phase.Charging:
                TickCharge(dt);
                break;
            case Phase.Warning:
                if (phaseLeft > 0f) break;
                Shockwave();
                SoundPlayer.Play("sfx_acc_release");
                SoundPlayer.Play("sfx_acc_exhaust");
                Stacks = 0;
                phase = Phase.Exhausted;
                phaseLeft = B.exhaustTime;
                if (anim) anim.PlayOnce("release");
                break;
            case Phase.Exhausted:
                if (phaseLeft <= 0f) BeginCycle();
                break;
        }
        UpdateView();
    }

    void TickCharge(float dt)
    {
        if (dt <= 0f) return;
        var pos = transform.position;
        float distance = Mathf.Min(B.chargeSpeed * dt, Mathf.Max(B.chargeDistance - traveled, 0f));
        Vector2 next = (Vector2)pos + chargeDir * distance;
        next = new Vector2(Mathf.Clamp(next.x, -moveLimitX, moveLimitX), Mathf.Clamp(next.y, -moveLimitY, moveLimitY));
        float step = Vector2.Distance(next, pos);
        traveled += step;
        transform.position = new Vector3(next.x, next.y, pos.z);
        if (!hit && player && box.bounds.Intersects(player.GetComponent<Collider2D>().bounds))
        {
            player.Health.TakeHit(B.chargeDamage, transform.position);
            hit = true;
        }
        if (step >= 0.0001f && traveled < B.chargeDistance) return;
        phase = Phase.Accumulating;
        chargeLeft = B.chargeInterval;
    }

    void UpdateView()
    {
        body.color = phase switch
        {
            Phase.Windup => new Color(1f, 0.6f, 0.1f),
            Phase.Warning => Mathf.FloorToInt(phaseLeft / 0.1f) % 2 == 0 ? Color.white : baseColor,
            Phase.Exhausted => baseColor * 0.5f,
            _ => baseColor,
        };
        if (anim)
        {
            anim.SetLoop(phase switch
            {
                Phase.Windup => "windup",
                Phase.Charging => "charge",
                Phase.Warning => "warning",
                Phase.Exhausted => "exhausted",
                _ => "idle",
            });
            if (phase == Phase.Charging) anim.Face(chargeDir.x < 0f ? -1 : 1);
        }
    }

    void Shockwave()
    {
        foreach (var hit in Physics2D.OverlapCircleAll(transform.position, B.shockwaveRadius))
            if (hit.TryGetComponent(out PlayerController target))
                target.Health.TakeHit(B.shockwaveDamage, transform.position);

        var view = new GameObject("Shockwave").AddComponent<SpriteRenderer>();
        view.sprite = AssetLibrary.SpriteUnitsOr("fx_shockwave_ring", 2f, CircleSprite());
        view.color = new Color(1f, 1f, 1f, 0.35f);
        view.sortingOrder = -1;
        view.transform.position = transform.position;
        view.transform.localScale = Vector3.one * B.shockwaveRadius;
        Destroy(view.gameObject, 0.2f);
    }

    // 지름 2유닛 원. scale = 반지름
    public static Sprite CircleSprite()
    {
        if (circleSprite) return circleSprite;
        const int size = 64;
        var tex = new Texture2D(size, size);
        float r = size / 2f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                bool inside = new Vector2(x + 0.5f - r, y + 0.5f - r).sqrMagnitude <= r * r;
                tex.SetPixel(x, y, inside ? Color.white : Color.clear);
            }
        tex.Apply();
        circleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size / 2f);
        return circleSprite;
    }

    void OnDied()
    {
        if (flow) flow.ClearStage();
        Destroy(gameObject);
    }

    void OnDestroy()
    {
        Rewindables.All.Remove(this);
        ClearPips();
    }

    void ClearPips()
    {
        if (pips == null) return;
        foreach (var p in pips)
            if (p) Destroy(p.gameObject);
    }

    // 칸 수 = 최대 스택. 보스 크기에 늘어나지 않게 보스가 아닌 방 아래에 두고 위치만 따라간다
    void LateUpdate()
    {
        int n = Mathf.Max(B.maxStacks, 1);
        if (pips == null || pips.Length != n)
        {
            ClearPips();
            pips = new SpriteRenderer[n];
            for (int i = 0; i < n; i++)
            {
                var sr = new GameObject($"StackPip{i}").AddComponent<SpriteRenderer>();
                sr.transform.SetParent(transform.parent);
                sr.sprite = AssetLibrary.SpriteUnitsOr("ui_acc_pip", 1f, RoomManager.Square);
                sr.sortingOrder = 5;
                pips[i] = sr;
            }
        }

        float width = n * pipSize + (n - 1) * pipGap;
        Vector3 top = transform.position + Vector3.up * (transform.localScale.y * 0.5f + 0.5f);
        for (int i = 0; i < n; i++)
        {
            pips[i].transform.position = top + Vector3.right * (-width / 2f + pipSize / 2f + i * (pipSize + pipGap));
            pips[i].transform.localScale = Vector3.one * pipSize;
            pips[i].color = i < Stacks ? pipFull : pipEmpty;
        }
    }
}
