using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 1장 보스3(최종): 시간 역행. 규칙: Docs/Bosses/03_Reversal.md
// 1단계: 고리 파동 + 역재생 / 2단계(체력 절반 이하): 위치 되감기 추가. 정지장 대상 아님
[RequireComponent(typeof(Health), typeof(SpriteRenderer), typeof(BoxCollider2D))]
public class ReversalBoss : MonoBehaviour
{
    [SerializeField] int maxHp = 40;
    [SerializeField, Range(0f, 1f)] float phase2Ratio = 0.5f;
    [SerializeField] float phaseTransition = 1f;
    [SerializeField] float walkSpeed = 1f;
    [SerializeField] float keepDistance = 2f;
    // 방 중심(0,0) 기준 이동 가능 범위
    [SerializeField] float moveLimitX = 8.5f;
    [SerializeField] float moveLimitY = 5f;

    [Header("고리 파동")]
    [SerializeField] float waveInterval = 3.5f;
    [SerializeField] float waveWindup = 0.6f;
    [SerializeField] float waveSpeed = 7f;
    [SerializeField] float waveThickness = 1f;
    [SerializeField] float waveMaxRadius = 12f;
    [SerializeField] int waveDamage = 1;

    [Header("역재생")]
    [SerializeField] float wallPause = 0.5f;
    [SerializeField] float returnSpeed = 7f;

    [Header("위치 되감기")]
    [SerializeField] float rewindInterval = 8f;
    [SerializeField] float rewindWarning = 1f;
    [SerializeField] float rewindSeconds = 3f;

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
    readonly List<(float time, Vector2 pos)> history = new();

    void Awake()
    {
        health = GetComponent<Health>();
        health.Init(maxHp, 0f);
        health.Died += OnDied;
        body = GetComponent<SpriteRenderer>();
        baseColor = body.color;
        anim = GetComponent<SpriteAnimator>();
    }

    void Start()
    {
        flow = FindAnyObjectByType<GameFlow>();
        player = FindAnyObjectByType<PlayerController>();
        if (player) playerBody = player.GetComponent<Rigidbody2D>();
        StartCoroutine(WaveLoop());
    }

    void Update()
    {
        RecordPlayer();
        if (!phase2 && !health.IsDead && health.Hp <= maxHp * phase2Ratio) StartCoroutine(EnterPhase2());
        if (windingUp || transitioning || !player) return;

        var pos = transform.position;
        Vector2 delta = player.transform.position - pos;
        bool walking = delta.magnitude > keepDistance;
        if (anim)
        {
            anim.Face(delta.x >= 0f ? 1 : -1);
            anim.SetLoop(walking ? "walk" : "idle");
        }
        if (!walking) return;
        Vector2 next = (Vector2)pos + delta.normalized * walkSpeed * Time.deltaTime;
        transform.position = new Vector3(Mathf.Clamp(next.x, -moveLimitX, moveLimitX), Mathf.Clamp(next.y, -moveLimitY, moveLimitY), pos.z);
    }

    IEnumerator WaveLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(waveInterval);
            while (transitioning) yield return null;
            if (!player) continue;

            windingUp = true;
            body.color = WindupColor;
            SoundPlayer.Play("sfx_rev_windup");
            if (anim) anim.PlayOnce("slam");
            yield return new WaitForSeconds(waveWindup);
            body.color = baseColor;
            windingUp = false;
            SpawnWave();
            SoundPlayer.Play("sfx_rev_wave");
        }
    }

    void SpawnWave()
    {
        var go = new GameObject("RingWave");
        go.transform.SetParent(transform.parent);
        go.AddComponent<GroundWave>().Init(transform.localScale.x * 0.5f, waveMaxRadius, waveThickness,
            waveSpeed, returnSpeed, wallPause, waveDamage, transform, WaveColor);
    }

    IEnumerator EnterPhase2()
    {
        phase2 = true;
        transitioning = true;
        SoundPlayer.Play("sfx_rev_phase2");
        if (anim) anim.PlayOnce("phase2");
        yield return GroundWave.Spiral(transform.position, phaseTransition, transform.parent);
        transitioning = false;
        StartCoroutine(RewindLoop());
    }

    IEnumerator RewindLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(rewindInterval - rewindWarning);
            if (!player || player.Health.IsDead) continue;

            // 예고 시작 시점 기준 N초 전 위치
            Vector2 target = PositionAgo(rewindSeconds);
            var ghost = new GameObject("RewindGhost").AddComponent<SpriteRenderer>();
            ghost.transform.SetParent(transform.parent);
            ghost.transform.position = target;
            ghost.transform.localScale = player.transform.localScale;
            // 잔상 = 플레이어 모습 그대로(임시 도형이든 그림이든)
            ghost.sprite = player.GetComponent<SpriteRenderer>().sprite;
            ghost.color = GhostColor;
            ghost.sortingOrder = 2;
            SoundPlayer.Play("sfx_rev_rewind_warn");

            yield return new WaitForSeconds(rewindWarning);
            if (ghost) Destroy(ghost.gameObject);
            if (!player || player.Health.IsDead) continue;
            player.transform.position = target;
            SoundPlayer.Play("sfx_rev_rewind");
            playerBody.linearVelocity = Vector2.zero;
        }
    }

    void RecordPlayer()
    {
        if (!player) return;
        history.Add((Time.time, player.transform.position));
        float oldest = Time.time - rewindSeconds - 1f;
        while (history.Count > 1 && history[0].time < oldest) history.RemoveAt(0);
    }

    // 기록이 N초보다 짧으면 가장 오래된 위치
    Vector2 PositionAgo(float seconds)
    {
        float t = Time.time - seconds;
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
        StopAllCoroutines();
        if (flow) flow.ClearStage();
        Destroy(gameObject);
    }

    void OnGUI()
    {
        if (!phase2) return;
        var cam = Camera.main;
        if (!cam) return;
        Vector3 s = cam.WorldToScreenPoint(transform.position + Vector3.up * (transform.localScale.y * 0.5f + 0.6f));
        GUI.Label(new Rect(s.x - 70f, Screen.height - s.y - 10f, 140f, 20f), "[2단계]");
    }
}
