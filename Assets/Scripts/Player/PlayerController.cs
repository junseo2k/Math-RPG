using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(Health))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] int maxHp = 5;
    [SerializeField] float hitInvincibleTime = 0.5f;
    [SerializeField] float moveSpeed = 6f;
    [SerializeField] float jumpSpeed = 12f;
    [SerializeField] float gravityScale = 3f;
    [SerializeField] float dashSpeed = 20f;
    [SerializeField] float dashTime = 0.15f;
    [SerializeField] float dashCooldown = 0.6f;

    public int Facing { get; private set; } = 1;
    public Health Health { get; private set; }
    public bool CanAct => flow && flow.State == GameState.Playing && !Health.IsDead;
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
    float dashEnd;
    float dashReadyAt;
    bool wasGrounded;

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
            if (Time.time < dashEnd) return;
            EndDash();
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
            body.linearVelocity = new Vector2(body.linearVelocity.x, jumpSpeed);
            SoundPlayer.Play("sfx_jump");
        }
        if (grounded && kb.shiftKey.wasPressedThisFrame && Time.time >= dashReadyAt)
            StartDash();
    }

    void StartDash()
    {
        SoundPlayer.Play("sfx_dash");
        dashing = true;
        dashEnd = Time.time + dashTime;
        dashReadyAt = dashEnd + dashCooldown * DashCooldownMultiplier;
        Health.Invincible = true;
        body.gravityScale = 0f;
        body.linearVelocity = new Vector2(Facing * dashSpeed, 0f);
        Dashed?.Invoke();
    }

    void EndDash()
    {
        dashing = false;
        Health.Invincible = false;
        body.gravityScale = gravityScale;
        body.linearVelocity = Vector2.zero;
    }

    void OnDied()
    {
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
        Health.ResetHealth();
        if (anim) anim.Release();
    }
}
