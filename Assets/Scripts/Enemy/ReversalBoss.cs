using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 1장 보스3(최종): 시간 역행. 규칙: Docs/Bosses/03_Reversal.md
// 1단계: 고리 파동 + 역재생 / 2단계(체력 절반 이하): 위치 되감기 추가. 정지장 대상 아님
[RequireComponent(typeof(Health), typeof(SpriteRenderer), typeof(BoxCollider2D))]
public class ReversalBoss : MonoBehaviour
{
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
    readonly List<(float time, Vector2 pos)> history = new();

    void Awake()
    {
        health = GetComponent<Health>();
        health.Init(B.maxHp, 0f);
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
        // 예비 동작 색이 아니면 기본 색(2단계 색 포함). 피격 번쩍임은 HitFlash가 그 뒤에 덮어쓴다
        if (!windingUp) body.color = baseColor;
        if (!phase2 && !health.IsDead && health.Hp <= B.maxHp * B.phase2Ratio) StartCoroutine(EnterPhase2());
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

    IEnumerator WaveLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(B.waveInterval);
            while (transitioning) yield return null;
            if (!player) continue;

            windingUp = true;
            body.color = WindupColor;
            SoundPlayer.Play("sfx_rev_windup");
            if (anim) anim.PlayOnce("slam");
            yield return new WaitForSeconds(B.waveWindup);
            body.color = baseColor;
            windingUp = false;
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

    IEnumerator EnterPhase2()
    {
        phase2 = true;
        transitioning = true;
        SoundPlayer.Play("sfx_rev_phase2");
        if (anim) anim.PlayOnce("phase2");
        yield return GroundWave.Spiral(transform.position, B.phaseTransition, transform.parent);
        baseColor = Color.Lerp(baseColor, phase2Color, phase2Blend);
        transitioning = false;
        StartCoroutine(RewindLoop());
    }

    IEnumerator RewindLoop()
    {
        while (true)
        {
            // 간격은 매번 무작위(타이밍 외워서 피하지 못하게)
            float interval = Random.Range(B.rewindIntervalRange.x, B.rewindIntervalRange.y);
            yield return new WaitForSeconds(Mathf.Max(interval - B.rewindWarning, 0f));
            if (!player || player.Health.IsDead) continue;

            // 예고 시작 시점 기준 N초 전 위치
            Vector2 target = PositionAgo(B.rewindSeconds);
            var ghost = new GameObject("RewindGhost").AddComponent<SpriteRenderer>();
            ghost.transform.SetParent(transform.parent);
            ghost.transform.position = target;
            ghost.transform.localScale = player.transform.localScale;
            // 잔상 = 플레이어 모습 그대로(임시 도형이든 그림이든)
            ghost.sprite = player.GetComponent<SpriteRenderer>().sprite;
            ghost.color = GhostColor;
            ghost.sortingOrder = 2;
            SoundPlayer.Play("sfx_rev_rewind_warn");

            yield return new WaitForSeconds(B.rewindWarning);
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
        float oldest = Time.time - B.rewindSeconds - 1f;
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
}
