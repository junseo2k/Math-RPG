using System.Collections.Generic;
using TMPro;
using UnityEngine;

// 유예된 피해. 규칙: Docs/Bosses/02_Deferral.md 2-2
// 머리 위 모래시계: 위 모래 = 가장 먼저 들어올 유예 피해의 남은 시간, 숫자 = 보류된 피해 합 (2-3)
[RequireComponent(typeof(Health))]
public class DeferredDamage : MonoBehaviour, IRewindable
{
    [SerializeField] float deferTime = 3f;

    [Header("모래시계 표시")]
    [SerializeField] float viewHeight = 1.1f;
    // HUD의 유예 피해 파란색과 맞춤
    [SerializeField] Color sandColor = new(0.35f, 0.6f, 1f);

    // 모래시계 칸 안쪽 크기(유닛). 임시 모래시계 그림(12×16px) 기준
    const float SandWidth = 0.2f;
    const float SandHeight = 0.2f;

    static Sprite hourglassSprite;

    Transform view;
    SpriteRenderer topSand;
    SpriteRenderer bottomSand;
    TextMeshPro label;

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

    void Awake()
    {
        health = GetComponent<Health>();
        Rewindables.All.Add(this);
    }

    void Start()
    {
        flow = FindAnyObjectByType<GameFlow>();
        if (flow) flow.RunReset += Clear;
    }

    void OnDestroy()
    {
        Rewindables.All.Remove(this);
        if (flow) flow.RunReset -= Clear;
        if (view) Destroy(view.gameObject);
    }

    public object Capture()
    {
        var s = new (int amount, float left)[pending.Count];
        for (int i = 0; i < s.Length; i++) s[i] = (pending[i].amount, Mathf.Max(pending[i].applyAt - Time.time, 0f));
        return s;
    }

    public void Restore(object state)
    {
        pending.Clear();
        foreach (var (amount, left) in ((int, float)[])state) pending.Add(new Pending { amount = amount, applyAt = Time.time + left });
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

    void LateUpdate()
    {
        bool show = pending.Count > 0 && !health.IsDead;
        if (!show)
        {
            if (view) view.gameObject.SetActive(false);
            return;
        }
        if (!view) BuildView();
        view.gameObject.SetActive(true);
        view.position = transform.position + Vector3.up * viewHeight;

        float left = deferTime > 0f ? Mathf.Clamp01((pending[0].applyAt - Time.time) / deferTime) : 0f;
        // 위 칸은 목(가운데)에서 위로, 아래 칸은 바닥에서 위로 쌓인다
        SetSand(topSand, 0f, left);
        SetSand(bottomSand, -SandHeight - 0.02f, 1f - left);
        label.text = Total.ToString();
    }

    static void SetSand(SpriteRenderer sand, float bottom, float ratio)
    {
        float h = SandHeight * ratio;
        sand.enabled = h > 0.001f;
        sand.transform.localPosition = new Vector3(0f, bottom + h / 2f, 0f);
        sand.transform.localScale = new Vector3(SandWidth, h, 1f);
    }

    void BuildView()
    {
        view = new GameObject("DeferredHourglass").transform;
        var frame = view.gameObject.AddComponent<SpriteRenderer>();
        frame.sprite = AssetLibrary.SpriteOr("ui_hourglass", HourglassSprite());
        frame.sortingOrder = 7;
        topSand = Sand("TopSand");
        bottomSand = Sand("BottomSand");

        label = new GameObject("Amount").AddComponent<TextMeshPro>();
        label.transform.SetParent(view, false);
        if (GameUI.Font) label.font = GameUI.Font;
        label.fontSize = 3.5f;
        label.color = sandColor;
        label.alignment = TextAlignmentOptions.Left;
        label.rectTransform.pivot = new Vector2(0f, 0.5f);
        label.rectTransform.sizeDelta = new Vector2(2f, 0.6f);
        label.transform.localPosition = new Vector3(0.28f, 0f, 0f);
        label.sortingOrder = 7;
    }

    SpriteRenderer Sand(string name)
    {
        var sr = new GameObject(name).AddComponent<SpriteRenderer>();
        sr.transform.SetParent(view, false);
        sr.sprite = RoomManager.Square;
        sr.color = sandColor;
        sr.sortingOrder = 6;
        return sr;
    }

    // 임시 모래시계 테두리(12×16px, PPU 32): 위아래 뚜껑 + 가운데로 좁아지는 옆선
    static Sprite HourglassSprite()
    {
        if (hourglassSprite) return hourglassSprite;
        const int w = 12, h = 16;
        var tex = new Texture2D(w, h) { filterMode = FilterMode.Point };
        float mid = (h - 1) / 2f;
        for (int y = 0; y < h; y++)
        {
            float half = Mathf.Lerp(1f, w / 2f - 1f, Mathf.Abs(y - mid) / mid);
            for (int x = 0; x < w; x++)
            {
                float dx = Mathf.Abs(x + 0.5f - w / 2f);
                bool cap = y == 0 || y == h - 1;
                bool side = Mathf.Abs(dx - half) < 0.75f;
                tex.SetPixel(x, y, cap || side ? Color.white : Color.clear);
            }
        }
        tex.Apply();
        hourglassSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 32f);
        return hourglassSprite;
    }
}
