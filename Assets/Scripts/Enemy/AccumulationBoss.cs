using System.Collections;
using UnityEngine;

// 1장 보스1: 시간 축적. 규칙: Docs/Bosses/01_Accumulation.md
[RequireComponent(typeof(Health), typeof(SpriteRenderer), typeof(BoxCollider2D))]
public class AccumulationBoss : MonoBehaviour
{
    // 전투 수치는 CombatBalance (Tools/전투 밸런스 열기)
    static CombatBalance.AccumulationBossStats B => CombatBalance.Instance.accumulationBoss;

    // 방 중심(0,0) 기준 이동 가능 범위
    [SerializeField] float moveLimitX = 8.5f;
    [SerializeField] float moveLimitY = 5f;

    [Header("축적 게이지 (머리 위)")]
    [SerializeField] float pipSize = 0.3f;
    [SerializeField] float pipGap = 0.1f;
    [SerializeField] Color pipFull = new(0.75f, 0.85f, 1f);
    [SerializeField] Color pipEmpty = new(0.2f, 0.22f, 0.3f, 0.8f);

    public int Stacks { get; private set; }

    SpriteRenderer[] pips;

    static Sprite circleSprite;

    Health health;
    SpriteAnimator anim;
    SpriteRenderer body;
    BoxCollider2D box;
    Color baseColor;
    GameFlow flow;
    PlayerController player;

    void Awake()
    {
        health = GetComponent<Health>();
        health.Init(B.maxHp, 0f);
        health.Died += OnDied;
        body = GetComponent<SpriteRenderer>();
        box = GetComponent<BoxCollider2D>();
        baseColor = body.color;
        anim = GetComponent<SpriteAnimator>();
    }

    void Start()
    {
        flow = FindAnyObjectByType<GameFlow>();
        player = FindAnyObjectByType<PlayerController>();
        StartCoroutine(Loop());
    }

    IEnumerator Loop()
    {
        while (true)
        {
            Stacks = 0;
            float nextStack = Time.time + B.stackInterval;
            float nextCharge = Time.time + B.chargeInterval;

            while (Stacks < B.maxStacks)
            {
                if (Time.time >= nextCharge)
                {
                    yield return Charge();
                    nextCharge = Time.time + B.chargeInterval;
                }
                // 돌진 중에도 축적은 계속된다
                while (Time.time >= nextStack && Stacks < B.maxStacks)
                {
                    Stacks++;
                    SoundPlayer.Play("sfx_acc_stack");
                    nextStack += B.stackInterval;
                }
                yield return null;
            }

            SoundPlayer.Play("sfx_acc_warning");
            if (anim) anim.SetLoop("warning");
            yield return Blink(B.releaseWarning);
            Shockwave();
            SoundPlayer.Play("sfx_acc_release");
            if (anim)
            {
                anim.PlayOnce("release");
                anim.SetLoop("exhausted");
            }

            Stacks = 0;
            body.color = baseColor * 0.5f;
            SoundPlayer.Play("sfx_acc_exhaust");
            yield return new WaitForSeconds(B.exhaustTime);
            body.color = baseColor;
            if (anim) anim.SetLoop("idle");
        }
    }

    IEnumerator Charge()
    {
        if (!player) yield break;

        body.color = new Color(1f, 0.6f, 0.1f);
        SoundPlayer.Play("sfx_acc_windup");
        if (anim) anim.SetLoop("windup");
        yield return new WaitForSeconds(B.chargeWindup);

        Vector2 dir = ((Vector2)(player.transform.position - transform.position)).normalized;
        SoundPlayer.Play("sfx_acc_charge");
        if (anim)
        {
            anim.Face(dir.x < 0f ? -1 : 1);
            anim.SetLoop("charge");
        }
        float traveled = 0f;
        bool hit = false;
        while (traveled < B.chargeDistance)
        {
            var pos = transform.position;
            Vector2 next = (Vector2)pos + dir * B.chargeSpeed * Time.deltaTime;
            next = new Vector2(Mathf.Clamp(next.x, -moveLimitX, moveLimitX), Mathf.Clamp(next.y, -moveLimitY, moveLimitY));
            float step = Vector2.Distance(next, pos);
            if (step < 0.0001f) break;
            traveled += step;
            transform.position = new Vector3(next.x, next.y, pos.z);

            if (!hit && box.bounds.Intersects(player.GetComponent<Collider2D>().bounds))
            {
                player.Health.TakeHit(B.chargeDamage, transform.position);
                hit = true;
            }
            yield return null;
        }
        body.color = baseColor;
        if (anim) anim.SetLoop("idle");
    }

    IEnumerator Blink(float duration)
    {
        float end = Time.time + duration;
        bool on = false;
        while (Time.time < end)
        {
            on = !on;
            body.color = on ? Color.white : baseColor;
            yield return new WaitForSeconds(0.1f);
        }
        body.color = baseColor;
    }

    void Shockwave()
    {
        foreach (var hit in Physics2D.OverlapCircleAll(transform.position, B.shockwaveRadius))
            if (hit.TryGetComponent(out PlayerController target))
                target.Health.TakeHit(B.shockwaveDamage, transform.position);

        var view = new GameObject("Shockwave").AddComponent<SpriteRenderer>();
        view.sprite = AssetLibrary.SpriteUnitsOr("fx_shockwave_ring", 2f, CircleSprite());
        view.color = new Color(1f, 1f, 1f, 0.35f);
        view.sortingOrder = -1;
        view.transform.position = transform.position;
        view.transform.localScale = Vector3.one * B.shockwaveRadius;
        Destroy(view.gameObject, 0.2f);
    }

    // 지름 2유닛 원. scale = 반지름
    public static Sprite CircleSprite()
    {
        if (circleSprite) return circleSprite;
        const int size = 64;
        var tex = new Texture2D(size, size);
        float r = size / 2f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                bool inside = new Vector2(x + 0.5f - r, y + 0.5f - r).sqrMagnitude <= r * r;
                tex.SetPixel(x, y, inside ? Color.white : Color.clear);
            }
        tex.Apply();
        circleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size / 2f);
        return circleSprite;
    }

    void OnDied()
    {
        StopAllCoroutines();
        if (flow) flow.ClearStage();
        Destroy(gameObject);
    }

    void OnDestroy()
    {
        if (pips == null) return;
        foreach (var p in pips)
            if (p) Destroy(p.gameObject);
    }

    // 칸 수 = 최대 스택. 보스 크기에 늘어나지 않게 보스가 아닌 방 아래에 두고 위치만 따라간다
    void LateUpdate()
    {
        int n = Mathf.Max(B.maxStacks, 1);
        if (pips == null || pips.Length != n)
        {
            OnDestroy();
            pips = new SpriteRenderer[n];
            for (int i = 0; i < n; i++)
            {
                var sr = new GameObject($"StackPip{i}").AddComponent<SpriteRenderer>();
                sr.transform.SetParent(transform.parent);
                sr.sprite = AssetLibrary.SpriteUnitsOr("ui_acc_pip", 1f, RoomManager.Square);
                sr.sortingOrder = 5;
                pips[i] = sr;
            }
        }

        float width = n * pipSize + (n - 1) * pipGap;
        Vector3 top = transform.position + Vector3.up * (transform.localScale.y * 0.5f + 0.5f);
        for (int i = 0; i < n; i++)
        {
            pips[i].transform.position = top + Vector3.right * (-width / 2f + pipSize / 2f + i * (pipSize + pipGap));
            pips[i].transform.localScale = Vector3.one * pipSize;
            pips[i].color = i < Stacks ? pipFull : pipEmpty;
        }
    }
}
