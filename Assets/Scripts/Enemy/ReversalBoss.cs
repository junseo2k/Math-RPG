using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 1장 보스3(최종): 시간 역행. 규칙: Docs/Bosses/03_Reversal.md
// 1단계: 지면 파동 + 역재생 / 2단계(체력 절반 이하): 위치 되감기 추가. 정지장 대상 아님
[RequireComponent(typeof(Health), typeof(SpriteRenderer), typeof(BoxCollider2D))]
public class ReversalBoss : MonoBehaviour
{
    [SerializeField] int maxHp = 40;
    [SerializeField, Range(0f, 1f)] float phase2Ratio = 0.5f;
    [SerializeField] float phaseTransition = 1f;
    [SerializeField] float walkSpeed = 1f;
    [SerializeField] float keepDistance = 2f;
    [SerializeField] float moveLimitX = 8.5f;

    [Header("지면 파동")]
    [SerializeField] float waveInterval = 3.5f;
    [SerializeField] float waveWindup = 0.6f;
    [SerializeField] float waveSpeed = 7f;
    [SerializeField] float waveHeight = 1f;
    [SerializeField] float waveWidth = 0.6f;
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
        float dx = player.transform.position.x - pos.x;
        bool walking = Mathf.Abs(dx) > keepDistance;
        if (anim)
        {
            anim.Face(dx >= 0f ? 1 : -1);
            anim.SetLoop(walking ? "walk" : "idle");
        }
        if (!walking) return;
        float x = Mathf.Clamp(pos.x + Mathf.Sign(dx) * walkSpeed * Time.deltaTime, -moveLimitX, moveLimitX);
        transform.position = new Vector3(x, pos.y, pos.z);
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
            SpawnWave(player.transform.position.x >= transform.position.x ? 1 : -1);
            SoundPlayer.Play("sfx_rev_wave");
        }
    }

    void SpawnWave(int dir)
    {
        float floorTop = transform.position.y - transform.localScale.y * 0.5f;
        var go = new GameObject("GroundWave");
        go.transform.SetParent(transform.parent);
        go.transform.position = new Vector3(
            transform.position.x + dir * (transform.localScale.x * 0.5f + waveWidth * 0.5f),
            floorTop + waveHeight * 0.5f, 0f);
        go.transform.localScale = new Vector3(waveWidth, waveHeight, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = AssetLibrary.SpriteUnitsOr("fx_rev_wave", 1f, RoomManager.Square);
        sr.color = WaveColor;
        sr.sortingOrder = 2;
        go.AddComponent<GroundWave>().Init(dir, waveSpeed, returnSpeed, wallPause, waveDamage, transform);
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
