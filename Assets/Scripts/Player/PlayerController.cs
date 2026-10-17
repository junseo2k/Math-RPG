using System;
using UnityEngine;
using UnityEngine.InputSystem;

// 이동·점프·대시 회피·피격 반응. 규칙: Docs/CombatDecisions.md 1장
[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(Health))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] int maxHp = 100;
    [SerializeField] float hitInvincibleTime = 0.8f;
    [SerializeField] float moveSpeed = 6f;
    [SerializeField] float jumpHeight = 3f;
    [SerializeField] float gravityScale = 3f;

    [Header("회피")]
    [SerializeField] float dashDistance = 3f;
    [SerializeField] float dashTime = 0.25f;
    [SerializeField] float dashInvincibleTime = 0.2f;
    [SerializeField] float dashCooldown = 0.8f;

    [Header("피격")]
    [SerializeField] float hitStun = 0.3f;
    [SerializeField] float knockbackDistance = 1f;

    [Header("디버그")]
    [SerializeField] bool showDebug = true;

    public int Facing { get; private set; } = 1;
    public Health Health { get; private set; }
    public bool CanAct => flow && flow.State == GameState.Playing && !Health.IsDead;
    public bool IsStunned => Time.time < stunUntil;
    public bool IsDashing => dashing;
    public bool CanAttack => CanAct && !IsStunned;
    public float MoveSpeedMultiplier { get; set; } = 1f;
    public float DashCooldownMultiplier { get; set; } = 1f;
    // 0 = 방금 사용, 1 = 사용 가능
    public float DashReadyRatio => dashing ? 0f : Mathf.Clamp01(1f - (dashReadyAt - Time.time) / (dashCooldown * DashCooldownMultiplier));

    public event Action Dashed;

    Rigidbody2D body;
    SpriteAnimator anim;
    GameFlow flow;
    ContactFilter2D groundFilter;
    Vector3 spawnPoint;
    bool dashing;
    int dashDir;
    float dashStart;
    float dashReadyAt;
    float stunUntil;
    float knockbackVelocity;
    bool wasGrounded;

    float JumpSpeed => Mathf.Sqrt(2f * Mathf.Abs(Physics2D.gravity.y) * gravityScale * jumpHeight);

    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        body.gravityScale = gravityScale;
        body.freezeRotation = true;
        Health = GetComponent<Health>();
        Health.Init(maxHp, hitInvincibleTime);
        Health.Died += OnDied;
        Health.Damaged += _ =>
        {
            SoundPlayer.Play("sfx_player_hurt");
            if (anim && !Health.IsDead) anim.PlayOnce("hurt");
        };
        Health.Hit += OnHit;
        anim = GetComponent<SpriteAnimator>();
        groundFilter.SetNormalAngle(45f, 135f);
        spawnPoint = transform.position;
    }

    void Start()
    {
        flow = FindAnyObjectByType<GameFlow>();
        if (flow) flow.RunReset += Respawn;
    }

    void OnDestroy()
    {
        if (flow) flow.RunReset -= Respawn;
    }

    // 애니메이션 상태: 대시 > 공중 > 달리기 > 대기
    void LateUpdate()
    {
        if (!anim || Health.IsDead) return;
        anim.Face(Facing);
        bool grounded = body.IsTouching(groundFilter);
        anim.SetLoop(dashing ? "dash" : !grounded ? "jump" : Mathf.Abs(body.linearVelocity.x) > 0.1f ? "run" : "idle");
    }

    void Update()
    {
        if (dashing)
        {
            float t = Time.time - dashStart;
            if (Health.Dodging && t >= dashInvincibleTime) SetDodgeInvincible(false);
            if (t < dashTime) return;
            EndDash();
        }

        if (IsStunned)
        {
            body.linearVelocity = new Vector2(knockbackVelocity, body.linearVelocity.y);
            return;
        }

        var kb = Keyboard.current;
        if (kb == null || !CanAct)
        {
            body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
            return;
        }

        float x = (kb.rightArrowKey.isPressed ? 1f : 0f) - (kb.leftArrowKey.isPressed ? 1f : 0f);
        if (x != 0f) Facing = (int)x;
        body.linearVelocity = new Vector2(x * moveSpeed * MoveSpeedMultiplier, body.linearVelocity.y);

        bool grounded = body.IsTouching(groundFilter);
        if (grounded && !wasGrounded) SoundPlayer.Play("sfx_land");
        wasGrounded = grounded;
        if (grounded && kb.spaceKey.wasPressedThisFrame)
        {
            body.linearVelocity = new Vector2(body.linearVelocity.x, JumpSpeed);
            SoundPlayer.Play("sfx_jump");
        }
        // 지상·공중 모두 가능, 제한은 쿨타임만
        if (kb.shiftKey.wasPressedThisFrame && Time.time >= dashReadyAt)
            StartDash(x != 0f ? (int)x : Facing);
    }

    void StartDash(int dir)
    {
        SoundPlayer.Play("sfx_dash");
        dashing = true;
        dashDir = dir;
        Facing = dir;
        dashStart = Time.time;
        dashReadyAt = dashStart + dashTime + dashCooldown * DashCooldownMultiplier;
        SetDodgeInvincible(true);
        body.gravityScale = 0f;
        body.linearVelocity = new Vector2(dashDir * dashDistance / dashTime, 0f);
        Dashed?.Invoke();
    }

    void EndDash()
    {
        dashing = false;
        SetDodgeInvincible(false);
        body.gravityScale = gravityScale;
        body.linearVelocity = Vector2.zero;
    }

    void SetDodgeInvincible(bool on)
    {
        Health.Invincible = on;
        Health.Dodging = on;
    }

    // 경직 동안 공격자 반대 방향으로 밀려남
    void OnHit(Vector2 from)
    {
        if (dashing) EndDash();
        int away = transform.position.x >= from.x ? 1 : -1;
        stunUntil = Time.time + hitStun;
        knockbackVelocity = hitStun > 0f ? away * knockbackDistance / hitStun : 0f;
    }

    void OnDied()
    {
        if (dashing) EndDash();
        stunUntil = 0f;
        SoundPlayer.Play("sfx_player_death");
        if (anim) anim.PlayHold("death");
        if (flow) flow.PlayerDied();
    }

    void Respawn()
    {
        if (dashing) EndDash();
        transform.position = spawnPoint;
        body.linearVelocity = Vector2.zero;
        dashReadyAt = 0f;
        stunUntil = 0f;
        Health.ResetHealth();
        if (anim) anim.Release();
    }

    void OnGUI()
    {
        if (!showDebug) return;
        string state = Health.IsDead ? "사망" : dashing ? (Health.Dodging ? "회피(무적)" : "회피") : IsStunned ? "경직" : "일반";
        GUI.Label(new Rect(10, Screen.height - 80, 400, 20),
            $"HP {Health.Hp}/{Health.MaxHp}  상태 {state}  회피 {(DashReadyRatio >= 1f ? "준비" : $"{Mathf.Max(dashReadyAt - Time.time, 0f):0.0}초")}");
    }
}
