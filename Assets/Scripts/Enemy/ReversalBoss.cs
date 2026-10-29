using System.Collections.Generic;
using UnityEngine;

// 1장 보스3(최종): 시간 역행. 규칙: Docs/Bosses/03_Reversal.md
// 1단계: 고리 파동 + 역재생 / 2단계(체력 절반 이하): 위치 되감기 추가. 정지장 대상 아님
[RequireComponent(typeof(Health), typeof(SpriteRenderer), typeof(BoxCollider2D))]
public class ReversalBoss : MonoBehaviour, IRewindable
{
    struct Snapshot
    {
        public Vector3 position;
        public bool phase2, windingUp, transitioning, rewinding;
        public float waveLeft, transitionLeft, rewindLeft;
        public Vector2 rewindTarget;
        public (float age, Vector2 pos)[] history;
    }

    // 전투 수치는 CombatBalance (Tools/전투 밸런스 열기)
    static CombatBalance.ReversalBossStats B => CombatBalance.Instance.reversalBoss;

    // 방 중심(0,0) 기준 이동 가능 범위
    [SerializeField] float moveLimitX = 8.5f;
    [SerializeField] float moveLimitY = 5f;
    // 2단계부터 몸 색을 이쪽으로 섞어 단계를 구분한다(빨강은 위험 표시 전용이라 쓰지 않음)
    [SerializeField] Color phase2Color = new(0.55f, 1f, 1f);
    [SerializeField, Range(0f, 1f)] float phase2Blend = 0.5f;

    static readonly Color WindupColor = new(0.6f, 1f, 1f);
    static readonly Color WaveColor = new(0.3f, 0.9f, 0.9f);
    static readonly Color GhostColor = new(1f, 0.8f, 0.3f, 0.35f);

    Health health;
    SpriteAnimator anim;
    SpriteRenderer body;
    Color baseColor;
    GameFlow flow;
    PlayerController player;
    Rigidbody2D playerBody;
    bool phase2;
    bool windingUp;
    bool transitioning;
    bool rewinding;
    float waveLeft;
    float transitionLeft;
    float rewindLeft;
    Vector2 rewindTarget;
    SpriteRenderer rewindGhost;
    SpriteRenderer transitionSpiral;
    readonly List<(float time, Vector2 pos)> history = new();

    void Awake()
    {
        health = GetComponent<Health>();
        health.Init(B.maxHp, 0f);
        health.Died += OnDied;
        body = GetComponent<SpriteRenderer>();
        baseColor = body.color;
        anim = GetComponent<SpriteAnimator>();
        waveLeft = B.waveInterval;
        Rewindables.All.Add(this);
    }

    void Start()
    {
        flow = FindAnyObjectByType<GameFlow>();
        player = FindAnyObjectByType<PlayerController>();
        if (player) playerBody = player.GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        if (health.IsDead || !flow || flow.State != GameState.Playing || TimeControl.IsStopped) return;
        RecordPlayer();
        float dt = Time.deltaTime;
        if (!phase2 && health.Hp <= B.maxHp * B.phase2Ratio)
        {
            phase2 = true;
            transitioning = true;
            transitionLeft = B.phaseTransition;
            SoundPlayer.Play("sfx_rev_phase2");
            if (anim) anim.PlayOnce("phase2");
        }
        if (transitioning)
        {
            transitionLeft -= dt;
            if (transitionLeft <= 0f)
            {
                transitioning = false;
                ScheduleRewind();
            }
        }
        TickWave(dt);
        if (phase2 && !transitioning) TickRewind(dt);
        UpdateView();
        if (windingUp || transitioning || !player) return;

        var pos = transform.position;
        Vector2 delta = player.transform.position - pos;
        bool walking = delta.magnitude > B.keepDistance;
        if (anim)
        {
            anim.Face(delta.x >= 0f ? 1 : -1);
            anim.SetLoop(walking ? "walk" : "idle");
        }
        if (!walking) return;
        Vector2 next = (Vector2)pos + delta.normalized * B.walkSpeed * Time.deltaTime;
        transform.position = new Vector3(Mathf.Clamp(next.x, -moveLimitX, moveLimitX), Mathf.Clamp(next.y, -moveLimitY, moveLimitY), pos.z);
    }

    void TickWave(float dt)
    {
        waveLeft -= dt;
        if (waveLeft > 0f || !player) return;
        if (!windingUp)
        {
            if (transitioning) return;
            windingUp = true;
            waveLeft = B.waveWindup;
            SoundPlayer.Play("sfx_rev_windup");
            if (anim) anim.PlayOnce("slam");
        }
        else
        {
            windingUp = false;
            waveLeft = B.waveInterval;
            SpawnWave();
            SoundPlayer.Play("sfx_rev_wave");
        }
    }

    void SpawnWave()
    {
        var go = new GameObject("RingWave");
        go.AddComponent<RewindSpawned>();
        go.transform.SetParent(transform.parent);
        go.AddComponent<GroundWave>().Init(transform.localScale.x * 0.5f, B.waveMaxRadius, B.waveThickness,
            B.waveSpeed, B.returnSpeed, B.wallPause, B.waveDamage, transform, WaveColor);
    }

    void ScheduleRewind()
    {
        rewindLeft = Mathf.Max(Random.Range(B.rewindIntervalRange.x, B.rewindIntervalRange.y) - B.rewindWarning, 0f);
    }

    void TickRewind(float dt)
    {
        rewindLeft -= dt;
        if (rewindLeft > 0f) return;
        if (!player || player.Health.IsDead) return;
        if (!rewinding)
        {
            // 예고 시작 시점 기준 N초 전 위치
            rewindTarget = PositionAgo(B.rewindSeconds);
            rewinding = true;
            rewindLeft = B.rewindWarning;
            SoundPlayer.Play("sfx_rev_rewind_warn");
        }
        else
        {
            rewinding = false;
            player.transform.position = rewindTarget;
            playerBody.position = rewindTarget;
            SoundPlayer.Play("sfx_rev_rewind");
            playerBody.linearVelocity = Vector2.zero;
            ScheduleRewind();
        }
    }

    void UpdateView()
    {
        Color idle = phase2 && !transitioning ? Color.Lerp(baseColor, phase2Color, phase2Blend) : baseColor;
        body.color = windingUp ? WindupColor : idle;
        if (rewinding && player)
        {
            if (!rewindGhost)
            {
                rewindGhost = new GameObject("RewindGhost").AddComponent<SpriteRenderer>();
                rewindGhost.transform.SetParent(transform.parent);
                rewindGhost.color = GhostColor;
                rewindGhost.sortingOrder = 2;
                rewindGhost.sprite = player.GetComponent<SpriteRenderer>().sprite;
            }
            rewindGhost.transform.position = rewindTarget;
            rewindGhost.transform.localScale = player.transform.localScale;
        }
        if (rewindGhost) rewindGhost.enabled = rewinding;
        if (transitioning)
        {
            if (!transitionSpiral) transitionSpiral = GroundWave.CreateSpiral(transform.position, transform);
            transitionSpiral.transform.rotation = Quaternion.Euler(0f, 0f, (B.phaseTransition - transitionLeft) * 360f);
        }
        if (transitionSpiral) transitionSpiral.enabled = transitioning;
    }

    public object Capture()
    {
        var recorded = new (float, Vector2)[history.Count];
        for (int i = 0; i < recorded.Length; i++) recorded[i] = (Time.time - history[i].time, history[i].pos);
        return new Snapshot
        {
            position = transform.position,
            phase2 = phase2,
            windingUp = windingUp,
            transitioning = transitioning,
            rewinding = rewinding,
            waveLeft = waveLeft,
            transitionLeft = transitionLeft,
            rewindLeft = rewindLeft,
            rewindTarget = rewindTarget,
            history = recorded,
        };
    }

    public void Restore(object state)
    {
        var s = (Snapshot)state;
        transform.position = s.position;
        phase2 = s.phase2;
        windingUp = s.windingUp;
        transitioning = s.transitioning;
        rewinding = s.rewinding;
        waveLeft = s.waveLeft;
        transitionLeft = s.transitionLeft;
        rewindLeft = s.rewindLeft;
        rewindTarget = s.rewindTarget;
        history.Clear();
        foreach (var (age, pos) in s.history) history.Add((Time.time - age, pos));
        if (rewindGhost)
        {
            Destroy(rewindGhost.gameObject);
            rewindGhost = null;
        }
        if (anim)
        {
            anim.Release();
            anim.SetLoop(transitioning ? "phase2" : windingUp ? "slam" : "idle");
        }
        UpdateView();
    }

    void RecordPlayer()
    {
        if (!player) return;
        history.Add((Time.time, player.transform.position));
        float oldest = Time.time - B.rewindSeconds - 1f;
        while (history.Count > 1 && history[0].time < oldest) history.RemoveAt(0);
    }

    // 기록이 N초보다 짧으면 가장 오래된 위치
    Vector2 PositionAgo(float seconds)
    {
        float t = Time.time - seconds;
        if (history.Count == 0) return player ? (Vector2)player.transform.position : Vector2.zero;
        Vector2 result = history[0].pos;
        foreach (var (time, pos) in history)
        {
            if (time > t) break;
            result = pos;
        }
        return result;
    }

    void OnDied()
    {
        if (flow) flow.ClearStage();
        Destroy(gameObject);
    }

    void OnDestroy()
    {
        Rewindables.All.Remove(this);
        if (rewindGhost) Destroy(rewindGhost.gameObject);
    }
}
