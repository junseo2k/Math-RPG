using System;
using UnityEngine;
using UnityEngine.InputSystem;

// 탑다운 8방향 이동(그림 4방향)·대시 회피·피격 반응. 규칙: Docs/SliceA/CombatDecisions.md 1장
[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(Health))]
public class PlayerController : MonoBehaviour, IRewindable
{
    public struct Snapshot
    {
        public Vector2 position;
        public Vector2 velocity;
        public Vector2 aim;
        public bool dashing;
        public Vector2 dashDir;
        public float dashElapsed;
        public float dashReadyLeft;
        public float stunLeft;
        public Vector2 knockbackVelocity;
    }

    // 수치는 CombatBalance (Tools/전투 밸런스 열기)
    static CombatBalance.PlayerStats B => CombatBalance.Instance.player;

    [Header("디버그")]
    [SerializeField] bool showDebug = true;

    // 마지막 이동(WASD) 방향 단위 벡터. 공격·회피·그림 방향
    public Vector2 Aim { get; private set; } = Vector2.right;
    // 좌우 반전용 (-1/1)
    public int Facing => Aim.x < 0f ? -1 : 1;
    // 특성·유물·성장 보너스를 더하기 전 최대 HP
    public int BaseMaxHp => B.maxHp;
    public Health Health { get; private set; }
    public bool CanAct => flow && flow.State == GameState.Playing && !Health.IsDead && !TimeControl.IsStopped;
    public bool IsStunned => Time.time < stunUntil;
    public bool IsDashing => dashing;
    public bool CanAttack => CanAct && !IsStunned;
    public float MoveSpeedMultiplier { get; set; } = 1f;
    // 범위 버프(태양핵)의 이동 속도 증가%. MoveSpeedMultiplier와 따로 둬서 서로 덮어쓰지 않게 한다
    public float ZoneMoveBonus { get; set; }
    public float DashCooldownMultiplier { get; set; } = 1f;
    // 0 = 방금 사용, 1 = 사용 가능
    public float DashReadyRatio => dashing ? 0f : Mathf.Clamp01(1f - (dashReadyAt - Time.time) / (B.dashCooldown * DashCooldownMultiplier));

    public event Action Dashed;

    Rigidbody2D body;
    SpriteAnimator anim;
    GameFlow flow;
    Vector3 spawnPoint;
    bool dashing;
    Vector2 dashDir;
    float dashStart;
    float dashReadyAt;
    float stunUntil;
    Vector2 knockbackVelocity;

    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.freezeRotation = true;
        // 물리 틱(50Hz)과 화면 주사율 차이로 카메라가 따라갈 때 덜컥거리지 않게
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        // 마찰 0: 벽에 비스듬히 붙어 이동할 때 마찰로 멈추는 문제 방지
        GetComponent<BoxCollider2D>().sharedMaterial = new PhysicsMaterial2D("PlayerNoFriction") { friction = 0f, bounciness = 0f };
        Health = GetComponent<Health>();
        Health.Init(B.maxHp, B.hitInvincibleTime);
        Health.Died += OnDied;
        Health.Damaged += _ =>
        {
            SoundPlayer.Play("sfx_player_hurt");
            if (anim && !Health.IsDead) anim.PlayOnce("hurt");
        };
        Health.Hit += OnHit;
        anim = GetComponent<SpriteAnimator>();
        spawnPoint = transform.position;
        Rewindables.All.Add(this);
    }

    public object Capture() => new Snapshot
    {
        position = transform.position,
        velocity = body.linearVelocity,
        aim = Aim,
        dashing = dashing,
        dashDir = dashDir,
        dashElapsed = dashing ? Time.time - dashStart : 0f,
        dashReadyLeft = Mathf.Max(dashReadyAt - Time.time, 0f),
        stunLeft = Mathf.Max(stunUntil - Time.time, 0f),
        knockbackVelocity = knockbackVelocity,
    };

    public void Restore(object state)
    {
        var s = (Snapshot)state;
        transform.position = s.position;
        body.position = s.position;
        body.linearVelocity = s.velocity;
        Aim = s.aim;
        dashing = s.dashing;
        dashDir = s.dashDir;
        dashStart = Time.time - s.dashElapsed;
        dashReadyAt = Time.time + s.dashReadyLeft;
        stunUntil = Time.time + s.stunLeft;
        knockbackVelocity = s.knockbackVelocity;
        // 같은 프레임 사망을 취소한 경우 사망 자세 해제
        if (anim) anim.Release();
    }

    void Start()
    {
        flow = FindAnyObjectByType<GameFlow>();
        if (flow) flow.RunReset += Respawn;
    }

    void OnDestroy()
    {
        Rewindables.All.Remove(this);
        if (flow) flow.RunReset -= Respawn;
    }

    // 그림은 4방향(2026-11-13). 대각선은 좌우 그림을 쓴다
    static string DirectionOf(Vector2 v)
    {
        if (Mathf.Abs(v.x) >= Mathf.Abs(v.y)) return v.x < 0f ? "w" : "e";
        return v.y < 0f ? "s" : "n";
    }

    // 애니메이션 상태: 대시 > 달리기 > 대기
    void LateUpdate()
    {
        if (!anim || Health.IsDead) return;
        anim.Face(Facing);
        anim.SetDirection(DirectionOf(Aim));
        anim.SetLoop(dashing ? "dash" : body.linearVelocity.sqrMagnitude > 0.01f ? "run" : "idle");
        PlayFootstep();
    }

    // 달리기 그림에서 발이 땅에 닿는 프레임마다 발소리(2026-11-13, Kenney footstep_concrete 3종 중 무작위)
    [SerializeField] int[] footstepFrames = { 0, 4 };
    int lastRunFrame = -1;

    void PlayFootstep()
    {
        int frame = anim.State == "run" ? anim.FrameIndex : -1;
        if (frame != lastRunFrame && Array.IndexOf(footstepFrames, frame) >= 0)
            SoundPlayer.Play("sfx_footstep_" + UnityEngine.Random.Range(0, 3), 1);
        lastRunFrame = frame;
    }

    void Update()
    {
        if (dashing)
        {
            float t = Time.time - dashStart;
            if (Health.Dodging && t >= B.dashInvincibleTime) SetDodgeInvincible(false);
            if (t < B.dashTime) return;
            EndDash();
        }

        if (IsStunned)
        {
            body.linearVelocity = knockbackVelocity;
            return;
        }

        var kb = Keyboard.current;
        if (kb == null || !CanAct)
        {
            body.linearVelocity = Vector2.zero;
            return;
        }

        var move = new Vector2(
            (kb.dKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed ? 1f : 0f),
            (kb.wKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed ? 1f : 0f)).normalized;
        // 이동 방향을 바라봄. 입력이 없으면 마지막 방향 유지
        if (move != Vector2.zero) Aim = move;
        body.linearVelocity = move * B.moveSpeed * (MoveSpeedMultiplier + ZoneMoveBonus);

        // 제한은 쿨타임만. 입력 없으면 조준 방향
        if (kb.spaceKey.wasPressedThisFrame && Time.time >= dashReadyAt)
            StartDash(move != Vector2.zero ? move : Aim);
    }

    void StartDash(Vector2 dir)
    {
        SoundPlayer.Play("sfx_dash");
        dashing = true;
        dashDir = dir;
        dashStart = Time.time;
        dashReadyAt = dashStart + B.dashTime + B.dashCooldown * DashCooldownMultiplier;
        SetDodgeInvincible(true);
        body.linearVelocity = dashDir * B.dashDistance / B.dashTime;
        Dashed?.Invoke();
    }

    void EndDash()
    {
        dashing = false;
        SetDodgeInvincible(false);
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
        Vector2 away = (Vector2)transform.position - from;
        away = away.sqrMagnitude > 0.0001f ? away.normalized : -Aim;
        stunUntil = Time.time + B.hitStun;
        knockbackVelocity = B.hitStun > 0f ? away * B.knockbackDistance / B.hitStun : Vector2.zero;
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
