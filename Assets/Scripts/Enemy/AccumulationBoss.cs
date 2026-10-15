using System.Collections;
using UnityEngine;

// 1장 보스1: 시간 축적. 규칙: Docs/Bosses/01_Accumulation.md
[RequireComponent(typeof(Health), typeof(SpriteRenderer), typeof(BoxCollider2D))]
public class AccumulationBoss : MonoBehaviour
{
    [SerializeField] int maxHp = 30;
    [SerializeField] float moveLimitX = 8.5f;

    [Header("축적·방출")]
    [SerializeField] float stackInterval = 1.5f;
    [SerializeField] int maxStacks = 5;
    [SerializeField] float releaseWarning = 1f;
    [SerializeField] float shockwaveRadius = 3.5f;
    [SerializeField] int shockwaveDamage = 2;
    [SerializeField] float exhaustTime = 2f;

    [Header("돌진")]
    [SerializeField] float chargeInterval = 3f;
    [SerializeField] float chargeWindup = 0.4f;
    [SerializeField] float chargeSpeed = 12f;
    [SerializeField] float chargeDistance = 6f;
    [SerializeField] int chargeDamage = 1;

    public int Stacks { get; private set; }

    static Sprite circleSprite;

    Health health;
    SpriteRenderer body;
    BoxCollider2D box;
    Color baseColor;
    GameFlow flow;
    PlayerController player;

    void Awake()
    {
        health = GetComponent<Health>();
        health.Init(maxHp, 0f);
        health.Died += OnDied;
        body = GetComponent<SpriteRenderer>();
        box = GetComponent<BoxCollider2D>();
        baseColor = body.color;
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
            float nextStack = Time.time + stackInterval;
            float nextCharge = Time.time + chargeInterval;

            while (Stacks < maxStacks)
            {
                if (Time.time >= nextCharge)
                {
                    yield return Charge();
                    nextCharge = Time.time + chargeInterval;
                }
                // 돌진 중에도 축적은 계속된다
                while (Time.time >= nextStack && Stacks < maxStacks)
                {
                    Stacks++;
                    nextStack += stackInterval;
                }
                yield return null;
            }

            yield return Blink(releaseWarning);
            Shockwave();

            Stacks = 0;
            body.color = baseColor * 0.5f;
            yield return new WaitForSeconds(exhaustTime);
            body.color = baseColor;
        }
    }

    IEnumerator Charge()
    {
        if (!player) yield break;

        body.color = new Color(1f, 0.6f, 0.1f);
        yield return new WaitForSeconds(chargeWindup);

        float dir = Mathf.Sign(player.transform.position.x - transform.position.x);
        float traveled = 0f;
        bool hit = false;
        while (traveled < chargeDistance)
        {
            var pos = transform.position;
            float nextX = Mathf.Clamp(pos.x + dir * chargeSpeed * Time.deltaTime, -moveLimitX, moveLimitX);
            if (Mathf.Approximately(nextX, pos.x)) break;
            traveled += Mathf.Abs(nextX - pos.x);
            transform.position = new Vector3(nextX, pos.y, pos.z);

            if (!hit && box.bounds.Intersects(player.GetComponent<Collider2D>().bounds))
            {
                player.Health.TakeDamage(chargeDamage);
                hit = true;
            }
            yield return null;
        }
        body.color = baseColor;
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
        foreach (var hit in Physics2D.OverlapCircleAll(transform.position, shockwaveRadius))
            if (hit.TryGetComponent(out PlayerController target))
                target.Health.TakeDamage(shockwaveDamage);

        var view = new GameObject("Shockwave").AddComponent<SpriteRenderer>();
        view.sprite = CircleSprite();
        view.color = new Color(1f, 1f, 1f, 0.35f);
        view.sortingOrder = -1;
        view.transform.position = transform.position;
        view.transform.localScale = Vector3.one * shockwaveRadius;
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

    void OnGUI()
    {
        var cam = Camera.main;
        if (!cam) return;
        Vector3 s = cam.WorldToScreenPoint(transform.position + Vector3.up * (transform.localScale.y * 0.5f + 0.8f));
        string gauge = new string('#', Stacks) + new string('-', maxStacks - Stacks);
        GUI.Label(new Rect(s.x - 60f, Screen.height - s.y - 20f, 120f, 40f), $"축적 [{gauge}]");
    }
}
