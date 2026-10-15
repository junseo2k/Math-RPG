using UnityEngine;

// 1장 보스2: 시간 유예. 규칙: Docs/Bosses/02_Deferral.md
// 정지장 대상 아님(IFreezable 미구현)
[RequireComponent(typeof(Health), typeof(SpriteRenderer), typeof(BoxCollider2D))]
public class DeferralBoss : MonoBehaviour
{
    [SerializeField] int maxHp = 35;
    [SerializeField] float moveSpeed = 1.5f;
    [SerializeField] float moveLimitX = 8.5f;

    [Header("투사체")]
    [SerializeField] float fireInterval = 2f;
    [SerializeField] float projectileSpeed = 8f;
    [SerializeField] int directDamage = 1;
    [SerializeField] float projectileSize = 0.4f;
    [SerializeField] float maxDistance = 40f;

    [Header("궤적 예약 · 피해 유예")]
    [SerializeField] TrailSettings trail = new() { delay = 2f, warning = 0.3f, width = 0.6f, active = 0.3f, damage = 1 };
    [SerializeField] int reducePerHit = 1;

    static readonly Color ProjectileColor = new(1f, 0.75f, 0.3f);

    Health health;
    SpriteAnimator anim;
    GameFlow flow;
    PlayerController player;
    DeferredDamage deferred;
    int dir = 1;
    float nextFire;

    void Awake()
    {
        health = GetComponent<Health>();
        health.Init(maxHp, 0f);
        health.Damaged += OnHit;
        health.Died += OnDied;
        anim = GetComponent<SpriteAnimator>();
    }

    void Start()
    {
        flow = FindAnyObjectByType<GameFlow>();
        player = FindAnyObjectByType<PlayerController>();
        if (player) deferred = player.GetComponent<DeferredDamage>();
        nextFire = Time.time + fireInterval;
    }

    void Update()
    {
        var pos = transform.position;
        float x = pos.x + dir * moveSpeed * Time.deltaTime;
        if (Mathf.Abs(x) >= moveLimitX)
        {
            x = Mathf.Clamp(x, -moveLimitX, moveLimitX);
            dir = -dir;
        }
        transform.position = new Vector3(x, pos.y, pos.z);
        if (anim && player) anim.Face(player.transform.position.x >= x ? 1 : -1);

        if (Time.time >= nextFire && player && !player.Health.IsDead)
        {
            nextFire = Time.time + fireInterval;
            Fire();
        }
    }

    void Fire()
    {
        SoundPlayer.Play("sfx_def_fire");
        if (anim) anim.PlayOnce("cast");
        Vector2 from = transform.position;
        Vector2 aim = (Vector2)player.transform.position - from;
        Vector2 direction = aim.sqrMagnitude > 0.001f ? aim.normalized : Vector2.right;

        var go = new GameObject("DeferredProjectile");
        go.transform.SetParent(transform.parent);
        go.transform.position = from;
        go.transform.localScale = Vector3.one * projectileSize;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = AssetLibrary.SpriteUnitsOr("fx_def_projectile", 1f, RoomManager.Square);
        sr.color = ProjectileColor;
        sr.sortingOrder = 2;
        go.AddComponent<DeferredProjectile>().Init(direction, projectileSpeed, directDamage, maxDistance, trail);
    }

    void OnHit(int _)
    {
        if (deferred) deferred.Reduce(reducePerHit);
    }

    void OnDied()
    {
        if (deferred) deferred.Clear();
        if (flow) flow.ClearStage();
        Destroy(gameObject);
    }
}
