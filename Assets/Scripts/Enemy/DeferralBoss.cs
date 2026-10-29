using UnityEngine;

// 1장 보스2: 시간 유예. 규칙: Docs/Bosses/02_Deferral.md
// 정지장 대상 아님(IFreezable 미구현)
[RequireComponent(typeof(Health), typeof(SpriteRenderer), typeof(BoxCollider2D))]
public class DeferralBoss : MonoBehaviour, IRewindable
{
    struct Snapshot
    {
        public Vector3 position;
        public Vector2 dir;
        public float fireLeft;
    }

    // 전투 수치는 CombatBalance (Tools/전투 밸런스 열기)
    static CombatBalance.DeferralBossStats B => CombatBalance.Instance.deferralBoss;

    // 방 중심(0,0) 기준 이동 가능 범위
    [SerializeField] float moveLimitX = 8.5f;
    [SerializeField] float moveLimitY = 5f;

    static readonly Color ProjectileColor = new(1f, 0.75f, 0.3f);

    Health health;
    SpriteAnimator anim;
    GameFlow flow;
    PlayerController player;
    DeferredDamage deferred;
    // 방 안을 비스듬히 떠돌다 경계에서 튕긴다
    Vector2 dir;
    float nextFire;

    void Awake()
    {
        health = GetComponent<Health>();
        health.Init(B.maxHp, 0f);
        health.Damaged += OnHit;
        health.Died += OnDied;
        anim = GetComponent<SpriteAnimator>();
        Rewindables.All.Add(this);
    }

    void Start()
    {
        flow = FindAnyObjectByType<GameFlow>();
        player = FindAnyObjectByType<PlayerController>();
        if (player) deferred = player.GetComponent<DeferredDamage>();
        nextFire = Time.time + B.fireInterval;
        // 축에 딱 붙지 않는 대각선 방향(30~60도)
        float angle = Random.Range(30f, 60f) * Mathf.Deg2Rad;
        dir = new Vector2(Mathf.Cos(angle) * (Random.value < 0.5f ? -1f : 1f), Mathf.Sin(angle) * (Random.value < 0.5f ? -1f : 1f));
    }

    void Update()
    {
        if (health.IsDead || !flow || flow.State != GameState.Playing || TimeControl.IsStopped) return;
        var pos = transform.position;
        Vector2 next = (Vector2)pos + dir * B.moveSpeed * Time.deltaTime;
        if (Mathf.Abs(next.x) >= moveLimitX) dir.x = -Mathf.Sign(next.x) * Mathf.Abs(dir.x);
        if (Mathf.Abs(next.y) >= moveLimitY) dir.y = -Mathf.Sign(next.y) * Mathf.Abs(dir.y);
        next = new Vector2(Mathf.Clamp(next.x, -moveLimitX, moveLimitX), Mathf.Clamp(next.y, -moveLimitY, moveLimitY));
        transform.position = new Vector3(next.x, next.y, pos.z);
        if (anim && player) anim.Face(player.transform.position.x >= next.x ? 1 : -1);

        if (Time.time >= nextFire && player && !player.Health.IsDead)
        {
            nextFire = Time.time + B.fireInterval;
            Fire();
        }
    }

    void OnDestroy() => Rewindables.All.Remove(this);

    public object Capture() => new Snapshot
    {
        position = transform.position,
        dir = dir,
        fireLeft = Mathf.Max(nextFire - Time.time, 0f),
    };

    public void Restore(object state)
    {
        var s = (Snapshot)state;
        transform.position = s.position;
        dir = s.dir;
        nextFire = Time.time + s.fireLeft;
        if (anim) anim.Release();
    }

    void Fire()
    {
        SoundPlayer.Play("sfx_def_fire");
        if (anim) anim.PlayOnce("cast");
        Vector2 from = transform.position;
        Vector2 aim = (Vector2)player.transform.position - from;
        Vector2 direction = aim.sqrMagnitude > 0.001f ? aim.normalized : Vector2.right;

        var go = new GameObject("DeferredProjectile");
        go.AddComponent<RewindSpawned>();
        go.transform.SetParent(transform.parent);
        go.transform.position = from;
        go.transform.localScale = Vector3.one * B.projectileSize;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = AssetLibrary.SpriteUnitsOr("fx_def_projectile", 1f, RoomManager.Square);
        sr.color = ProjectileColor;
        sr.sortingOrder = 2;
        go.AddComponent<DeferredProjectile>().Init(direction, B.projectileSpeed, B.directDamage, B.maxDistance, B.trail);
    }

    void OnHit(int _)
    {
        if (deferred) deferred.Reduce(B.reducePerHit);
    }

    void OnDied()
    {
        if (deferred) deferred.Clear();
        if (flow) flow.ClearStage();
        Destroy(gameObject);
    }
}
