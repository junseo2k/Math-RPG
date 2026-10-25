using System;
using System.Collections;
using UnityEngine;

// 근접 순찰형 일반 적. 규칙: Docs/SliceA/NormalRoom.md 2장
// 역행 대응: 공격은 코루틴 대신 단계·경과 시간으로 진행, 처치 시 파괴 대신 비활성화(역행으로 되살아남)
[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(Health))]
public class PatrolEnemy : MonoBehaviour, IFreezable, IRewindable
{
    // 수치는 CombatBalance (Tools/전투 밸런스 열기)
    static CombatBalance.SentinelStats B => CombatBalance.Instance.sentinel;

    // 처치 시 1회
    public event Action Removed;

    enum AttackPhase { None, Windup, Recover }

    struct Snapshot
    {
        public bool active;
        public bool removed;
        public Vector2 position;
        public Vector2 velocity;
        public Vector2 dir;
        public AttackPhase attack;
        public float attackTime;
        public float stunLeft;
        public float frozenLeft;
        public float slowLeft;
        public float slowRatio;
        public Color color;
    }

    static readonly Color WindupColor = new(1f, 0.6f, 0.1f);
    static readonly Color FrozenColor = new(0.4f, 0.7f, 1f);

    Rigidbody2D body;
    BoxCollider2D box;
    SpriteRenderer view;
    Color baseColor;
    Color currentColor;
    Health health;
    SpriteAnimator anim;
    PlayerController player;
    // 이동·공격 방향 단위 벡터. 순찰은 좌우 왕복으로 시작
    Vector2 dir = Vector2.right;
    AttackPhase attack;
    float attackTime;
    bool removed;
    float stunUntil;
    float frozenUntil;
    bool wasFrozen;
    float slowUntil;
    float slowRatio;

    bool Attacking => attack != AttackPhase.None;
    bool Frozen => Time.time < frozenUntil;
    float SpeedScale => Time.time < slowUntil ? 1f - slowRatio : 1f;

    public void Freeze(float seconds) => frozenUntil = Mathf.Max(frozenUntil, Time.time + seconds);

    public void Slow(float ratio, float seconds)
    {
        slowRatio = Mathf.Clamp01(ratio);
        slowUntil = Mathf.Max(slowUntil, Time.time + seconds);
    }

    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.freezeRotation = true;
        box = GetComponent<BoxCollider2D>();
        view = GetComponent<SpriteRenderer>();
        baseColor = view ? view.color : Color.white;
        currentColor = baseColor;
        health = GetComponent<Health>();
        anim = GetComponent<SpriteAnimator>();
        health.Init(B.maxHp, 0f);
        health.Damaged += OnDamaged;
        health.Died += Remove;
        Rewindables.All.Add(this);
    }

    void OnDestroy() => Rewindables.All.Remove(this);

    public object Capture() => new Snapshot
    {
        active = gameObject.activeSelf,
        removed = removed,
        position = transform.position,
        velocity = body.linearVelocity,
        dir = dir,
        attack = attack,
        attackTime = attackTime,
        stunLeft = Mathf.Max(stunUntil - Time.time, 0f),
        frozenLeft = Mathf.Max(frozenUntil - Time.time, 0f),
        slowLeft = Mathf.Max(slowUntil - Time.time, 0f),
        slowRatio = slowRatio,
        color = currentColor,
    };

    public void Restore(object state)
    {
        var s = (Snapshot)state;
        gameObject.SetActive(s.active);
        removed = s.removed;
        transform.position = s.position;
        body.position = s.position;
        body.linearVelocity = s.velocity;
        dir = s.dir;
        attack = s.attack;
        attackTime = s.attackTime;
        stunUntil = Time.time + s.stunLeft;
        frozenUntil = Time.time + s.frozenLeft;
        slowUntil = Time.time + s.slowLeft;
        slowRatio = s.slowRatio;
        wasFrozen = false;
        SetColor(s.color);
        if (view && !Frozen) view.color = currentColor;
    }

    void Update()
    {
        if (Frozen)
        {
            body.linearVelocity = Vector2.zero;
            if (view) view.color = FrozenColor;
            wasFrozen = true;
            return;
        }
        if (wasFrozen)
        {
            wasFrozen = false;
            if (view) view.color = currentColor;
        }
        if (Attacking)
        {
            body.linearVelocity = Vector2.zero;
            TickAttack();
            return;
        }
        if (Time.time < stunUntil)
        {
            body.linearVelocity = Vector2.zero;
            return;
        }
        if (!player) player = FindAnyObjectByType<PlayerController>();

        float speed = B.patrolSpeed;
        bool chasing = false;
        if (player && !player.Health.IsDead)
        {
            Vector2 delta = player.transform.position - transform.position;
            if (delta.magnitude <= B.detectRange)
            {
                chasing = true;
                speed = B.chaseSpeed;
                if (delta != Vector2.zero) dir = delta.normalized;
                float gap = delta.magnitude - box.bounds.extents.x - player.GetComponent<Collider2D>().bounds.extents.x;
                if (gap <= B.attackRange)
                {
                    StartAttack();
                    return;
                }
            }
        }

        if (Blocked())
        {
            if (chasing) speed = 0f;
            else dir = -dir;
        }
        body.linearVelocity = dir * speed * SpeedScale;
    }

    void LateUpdate()
    {
        if (!anim) return;
        anim.Face(dir.x < 0f ? -1 : 1);
        if (!Attacking) anim.SetLoop(body.linearVelocity.sqrMagnitude > 0.01f ? "walk" : "idle");
    }

    // 진행 방향 앞이 벽이면 true
    bool Blocked()
    {
        Vector2 pos = transform.position;
        float ext = box.bounds.extents.x;
        return HitsGround(pos + dir * (ext + 0.02f), dir, 0.15f);
    }

    // 지형(리지드바디 없는 고체 콜라이더)만 감지
    public static bool HitsGround(Vector2 origin, Vector2 direction, float distance)
    {
        foreach (var hit in Physics2D.RaycastAll(origin, direction, distance))
            if (!hit.collider.isTrigger && !hit.rigidbody) return true;
        return false;
    }

    void StartAttack()
    {
        attack = AttackPhase.Windup;
        attackTime = 0f;
        SetColor(WindupColor);
        if (anim) anim.SetLoop("windup");
        SoundPlayer.Play("sfx_enemy_windup");
    }

    // 정지된 시간은 세지 않는다(Update에서 정지 중이면 여기까지 오지 않음)
    void TickAttack()
    {
        attackTime += Time.deltaTime;
        if (attack == AttackPhase.Windup && attackTime >= B.windup)
        {
            Slash();
            attack = AttackPhase.Recover;
            attackTime = 0f;
        }
        else if (attack == AttackPhase.Recover && attackTime >= B.recover)
        {
            attack = AttackPhase.None;
        }
    }

    void Slash()
    {
        Vector2 center = (Vector2)transform.position + dir * (box.bounds.extents.x + B.attackRange * 0.5f);
        Vector2 size = new(B.attackRange, box.bounds.size.y);
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        foreach (var hit in Physics2D.OverlapBoxAll(center, size, angle))
            if (hit.TryGetComponent(out PlayerController target))
                target.Health.TakeHit(B.slashDamage, transform.position);
        ShowSlash(center, size, angle);
        if (anim)
        {
            anim.PlayOnce("slash");
            anim.SetLoop("idle");
        }
        SoundPlayer.Play("sfx_enemy_slash");
        SetColor(baseColor);
    }

    void SetColor(Color c)
    {
        currentColor = c;
        if (view && !Frozen) view.color = c;
    }

    static void ShowSlash(Vector2 center, Vector2 size, float angle)
    {
        var slash = new GameObject("Slash").AddComponent<SpriteRenderer>();
        slash.sprite = RoomManager.Square;
        slash.color = new Color(1f, 1f, 1f, 0.5f);
        slash.transform.position = center;
        slash.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        slash.transform.localScale = size;
        Destroy(slash.gameObject, 0.1f);
    }

    // 예비 동작·공격 중에는 경직으로 취소되지 않음
    void OnDamaged(int _)
    {
        SoundPlayer.Play("sfx_enemy_hurt");
        if (anim && !Attacking) anim.PlayOnce("hurt");
        if (!Attacking) stunUntil = Time.time + B.hitStun;
        if (view && !Attacking && !Frozen && gameObject.activeInHierarchy) StartCoroutine(Flash());
    }

    IEnumerator Flash()
    {
        view.color = Color.white;
        yield return new WaitForSeconds(0.08f);
        if (!Frozen) view.color = currentColor;
    }

    void Remove()
    {
        if (removed) return;
        removed = true;
        Removed?.Invoke();
        gameObject.SetActive(false);
    }
}
