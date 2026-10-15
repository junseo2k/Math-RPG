using System;
using System.Collections;
using UnityEngine;

// 근접 순찰형 일반 적. 규칙: Docs/NormalRoom.md 2장
[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(Health))]
public class PatrolEnemy : MonoBehaviour, IFreezable
{
    [SerializeField] int maxHp = 3;
    [SerializeField] float patrolSpeed = 2f;
    [SerializeField] float chaseSpeed = 3.5f;
    [SerializeField] float detectRange = 6f;
    [SerializeField] float detectHeight = 1.5f;
    [SerializeField] float attackRange = 1.2f;
    [SerializeField] float windup = 0.5f;
    [SerializeField] int slashDamage = 1;
    [SerializeField] float recover = 1f;
    [SerializeField] float hitStun = 0.2f;
    [SerializeField] float gravityScale = 3f;
    [SerializeField] float fallLimitY = -10f;

    // 처치 또는 방 밖 낙하 시 1회
    public event Action Removed;

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
    int dir = 1;
    bool attacking;
    bool removed;
    float stunUntil;
    float frozenUntil;
    bool wasFrozen;

    bool Frozen => Time.time < frozenUntil;

    public void Freeze(float seconds) => frozenUntil = Mathf.Max(frozenUntil, Time.time + seconds);

    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        body.gravityScale = gravityScale;
        body.freezeRotation = true;
        box = GetComponent<BoxCollider2D>();
        view = GetComponent<SpriteRenderer>();
        baseColor = view ? view.color : Color.white;
        currentColor = baseColor;
        health = GetComponent<Health>();
        anim = GetComponent<SpriteAnimator>();
        health.Init(maxHp, 0f);
        health.Damaged += OnDamaged;
        health.Died += Remove;
    }

    void Update()
    {
        if (transform.position.y < fallLimitY)
        {
            Remove();
            return;
        }
        if (Frozen)
        {
            body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
            if (view) view.color = FrozenColor;
            wasFrozen = true;
            return;
        }
        if (wasFrozen)
        {
            wasFrozen = false;
            if (view) view.color = currentColor;
        }
        if (attacking || Time.time < stunUntil)
        {
            body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
            return;
        }
        if (!player) player = FindAnyObjectByType<PlayerController>();

        float speed = patrolSpeed;
        bool chasing = false;
        if (player && !player.Health.IsDead)
        {
            Vector2 delta = player.transform.position - transform.position;
            if (Mathf.Abs(delta.x) <= detectRange && Mathf.Abs(delta.y) <= detectHeight)
            {
                chasing = true;
                speed = chaseSpeed;
                if (delta.x != 0f) dir = delta.x > 0f ? 1 : -1;
                float gap = Mathf.Abs(delta.x) - box.bounds.extents.x - player.GetComponent<Collider2D>().bounds.extents.x;
                if (gap <= attackRange)
                {
                    StartCoroutine(Attack());
                    return;
                }
            }
        }

        if (Blocked())
        {
            if (chasing) speed = 0f;
            else dir = -dir;
        }
        body.linearVelocity = new Vector2(dir * speed, body.linearVelocity.y);
    }

    void LateUpdate()
    {
        if (!anim) return;
        anim.Face(dir);
        if (!attacking) anim.SetLoop(Mathf.Abs(body.linearVelocity.x) > 0.1f ? "walk" : "idle");
    }

    // 앞이 벽이거나 발판 끝이면 true
    bool Blocked()
    {
        Vector2 pos = transform.position;
        Vector2 ext = box.bounds.extents;
        bool wall = HitsGround(new Vector2(pos.x + dir * (ext.x + 0.02f), pos.y), Vector2.right * dir, 0.15f);
        bool grounded = Mathf.Abs(body.linearVelocity.y) < 0.01f;
        bool ledge = grounded && !HitsGround(new Vector2(pos.x + dir * (ext.x + 0.1f), pos.y - ext.y + 0.05f), Vector2.down, 0.3f);
        return wall || ledge;
    }

    // 지형(리지드바디 없는 고체 콜라이더)만 감지
    public static bool HitsGround(Vector2 origin, Vector2 direction, float distance)
    {
        foreach (var hit in Physics2D.RaycastAll(origin, direction, distance))
            if (!hit.collider.isTrigger && !hit.rigidbody) return true;
        return false;
    }

    IEnumerator Attack()
    {
        attacking = true;
        SetColor(WindupColor);
        if (anim) anim.SetLoop("windup");
        SoundPlayer.Play("sfx_enemy_windup");
        yield return WaitUnfrozen(windup);

        Vector2 center = (Vector2)transform.position + new Vector2(dir * (box.bounds.extents.x + attackRange * 0.5f), 0f);
        Vector2 size = new(attackRange, box.bounds.size.y);
        foreach (var hit in Physics2D.OverlapBoxAll(center, size, 0f))
            if (hit.TryGetComponent(out PlayerController target))
                target.Health.TakeDamage(slashDamage);
        ShowSlash(center, size);
        if (anim)
        {
            anim.PlayOnce("slash");
            anim.SetLoop("idle");
        }
        SoundPlayer.Play("sfx_enemy_slash");

        SetColor(baseColor);
        yield return WaitUnfrozen(recover);
        attacking = false;
    }

    // 정지된 시간은 세지 않는다(정지장이 예비 동작·후딜을 늦춤)
    IEnumerator WaitUnfrozen(float seconds)
    {
        float t = 0f;
        while (t < seconds)
        {
            if (!Frozen) t += Time.deltaTime;
            yield return null;
        }
    }

    void SetColor(Color c)
    {
        currentColor = c;
        if (view && !Frozen) view.color = c;
    }

    static void ShowSlash(Vector2 center, Vector2 size)
    {
        var slash = new GameObject("Slash").AddComponent<SpriteRenderer>();
        slash.sprite = RoomManager.Square;
        slash.color = new Color(1f, 1f, 1f, 0.5f);
        slash.transform.position = center;
        slash.transform.localScale = size;
        Destroy(slash.gameObject, 0.1f);
    }

    // 예비 동작·공격 중에는 경직으로 취소되지 않음
    void OnDamaged(int _)
    {
        SoundPlayer.Play("sfx_enemy_hurt");
        if (anim && !attacking) anim.PlayOnce("hurt");
        if (!attacking) stunUntil = Time.time + hitStun;
        if (view && !attacking && !Frozen) StartCoroutine(Flash());
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
        Destroy(gameObject);
    }
}
