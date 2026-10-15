using System.Collections.Generic;
using UnityEngine;

// 유예된 피해. 규칙: Docs/Bosses/02_Deferral.md 2-2
[RequireComponent(typeof(Health))]
public class DeferredDamage : MonoBehaviour
{
    [SerializeField] float deferTime = 3f;

    struct Pending
    {
        public int amount;
        public float applyAt;
    }

    readonly List<Pending> pending = new();
    Health health;
    GameFlow flow;

    public int Total
    {
        get
        {
            int total = 0;
            foreach (var p in pending) total += p.amount;
            return total;
        }
    }

    void Awake() => health = GetComponent<Health>();

    void Start()
    {
        flow = FindAnyObjectByType<GameFlow>();
        if (flow) flow.RunReset += Clear;
    }

    void OnDestroy()
    {
        if (flow) flow.RunReset -= Clear;
    }

    public void Add(int amount)
    {
        pending.Add(new Pending { amount = amount, applyAt = Time.time + deferTime });
        SoundPlayer.Play("sfx_def_deferred");
    }

    // 먼저 들어온 것부터 줄인다
    public void Reduce(int amount)
    {
        while (amount > 0 && pending.Count > 0)
        {
            var p = pending[0];
            int cut = Mathf.Min(amount, p.amount);
            p.amount -= cut;
            amount -= cut;
            if (p.amount == 0) pending.RemoveAt(0);
            else pending[0] = p;
        }
    }

    public void Clear() => pending.Clear();

    void Update()
    {
        for (int i = pending.Count - 1; i >= 0; i--)
        {
            if (Time.time < pending[i].applyAt) continue;
            int amount = pending[i].amount;
            pending.RemoveAt(i);
            health.ApplyDirect(amount);
            SoundPlayer.Play("sfx_def_apply");
        }
    }

    void OnGUI()
    {
        if (pending.Count == 0) return;
        var cam = Camera.main;
        if (!cam) return;
        Vector3 s = cam.WorldToScreenPoint(transform.position + Vector3.up * 1.2f);
        float remain = Mathf.Max(pending[0].applyAt - Time.time, 0f);
        GUI.Label(new Rect(s.x - 60f, Screen.height - s.y - 10f, 120f, 20f), $"유예 {Total} ({remain:0.0}초)");
    }
}
