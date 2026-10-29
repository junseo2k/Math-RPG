using System;
using UnityEngine;

[Serializable]
public struct TrailSettings
{
    public float delay;
    public float warning;
    public float width;
    public float active;
    public int damage;
}

// 투사체가 남긴 점선 궤적: 대기 → 예고 깜빡임 → 발동(맞으면 유예 피해). 규칙: Docs/Bosses/02_Deferral.md
[RequireComponent(typeof(RewindSpawned))]
public class TrailStrike : MonoBehaviour, IRewindable
{
    struct Snapshot
    {
        public float elapsed;
        public bool hitDone, warned;
    }

    static readonly Color DotColor = new(1f, 0.75f, 0.3f, 0.6f);
    static readonly Color StrikeColor = new(1f, 0.8f, 0.4f, 0.9f);

    TrailSettings settings;
    float length;
    float angle;
    float startTime;
    bool hitDone;
    bool warned;
    SpriteRenderer[] dots;
    SpriteRenderer bar;
    RewindSpawned lifetime;

    void Awake()
    {
        lifetime = GetComponent<RewindSpawned>();
        Rewindables.All.Add(this);
    }

    void OnDestroy() => Rewindables.All.Remove(this);

    public object Capture() => new Snapshot
    {
        elapsed = Time.time - startTime,
        hitDone = hitDone,
        warned = warned,
    };

    public void Restore(object state)
    {
        var s = (Snapshot)state;
        startTime = Time.time - s.elapsed;
        hitDone = s.hitDone;
        warned = s.warned;
        RefreshView(s.elapsed);
    }

    void RefreshView(float elapsed)
    {
        bool active = elapsed >= settings.delay && elapsed < settings.delay + settings.active;
        bar.enabled = active;
        bool warning = elapsed >= settings.delay - settings.warning && elapsed < settings.delay;
        foreach (var d in dots)
        {
            d.enabled = !active;
            d.color = warning && Mathf.FloorToInt(elapsed / 0.075f) % 2 == 0 ? Color.white : DotColor;
        }
    }

    public void Init(Vector2 from, Vector2 to, TrailSettings trail)
    {
        settings = trail;
        startTime = Time.time;
        Vector2 delta = to - from;
        length = delta.magnitude;
        angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
        transform.position = (from + to) * 0.5f;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        int count = Mathf.Max(1, Mathf.FloorToInt(length / 0.5f));
        dots = new SpriteRenderer[count];
        for (int i = 0; i < count; i++)
        {
            dots[i] = Part("Dot", "fx_def_trail_dot", new Vector2(-length / 2f + (i + 0.5f) * length / count, 0f), new Vector2(0.15f, 0.15f), DotColor);
        }
        bar = Part("Strike", "fx_def_trail_strike", Vector2.zero, new Vector2(length, settings.width), StrikeColor);
        bar.enabled = false;
    }

    SpriteRenderer Part(string name, string art, Vector2 localPos, Vector2 size, Color color)
    {
        var sr = new GameObject(name).AddComponent<SpriteRenderer>();
        sr.transform.SetParent(transform, false);
        sr.transform.localPosition = localPos;
        sr.transform.localScale = size;
        sr.sprite = AssetLibrary.SpriteUnitsOr(art, 1f, RoomManager.Square);
        sr.color = color;
        return sr;
    }

    void Update()
    {
        if (!lifetime.CanTick) return;
        float t = Time.time - startTime;
        if (t < settings.delay - settings.warning) return;

        if (t < settings.delay)
        {
            if (!warned)
            {
                warned = true;
                SoundPlayer.Play("sfx_def_trail_warn");
            }
            bool on = Mathf.FloorToInt(t / 0.075f) % 2 == 0;
            foreach (var d in dots) d.color = on ? Color.white : DotColor;
            return;
        }

        if (t < settings.delay + settings.active)
        {
            if (!bar.enabled)
            {
                bar.enabled = true;
                SoundPlayer.Play("sfx_def_trail_strike");
                foreach (var d in dots) d.enabled = false;
            }
            if (!hitDone) CheckHit();
            return;
        }

        lifetime.Despawn();
    }

    // 대시 무적 중이면 맞지 않음. 궤적 하나당 한 번만 판정
    void CheckHit()
    {
        foreach (var hit in Physics2D.OverlapBoxAll(transform.position, new Vector2(length, settings.width), angle))
        {
            if (!hit.TryGetComponent(out PlayerController player)) continue;
            if (player.Health.Invincible || player.Health.IsDead) continue;
            if (player.TryGetComponent(out DeferredDamage deferred)) deferred.Add(settings.damage);
            hitDone = true;
            return;
        }
    }
}
