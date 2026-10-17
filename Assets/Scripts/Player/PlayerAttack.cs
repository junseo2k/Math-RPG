using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerController))]
public class PlayerAttack : MonoBehaviour, IRewindable
{
    struct Snapshot
    {
        public float readyLeft;
        public int bonus;
        public float bonusLeft;
        public (float amount, float left)[] oneShot;
    }

    [SerializeField] int damage = 10;
    [SerializeField] float interval = 0.4f;
    [SerializeField] Vector2 range = new(1.5f, 1f);
    [SerializeField, Range(0f, 1f)] float critChance = 0.05f;
    [SerializeField] float critMultiplier = 1.5f;

    PlayerController controller;
    float readyAt;
    int bonus;
    float bonusUntil;
    // 다음 공격 1회 피해 증가%(만료 시각). 휘두르면 소모
    readonly List<(float amount, float until)> oneShot = new();

    public bool HasBonus => bonus > 0 && Time.time < bonusUntil;
    // 피해 증가% 합(0.08 = +8%). 특성·유물이 더한다
    public float DamageBonus { get; set; }
    public bool LastHitCrit { get; private set; }

    // 적중한 휘두름 1회마다: 판정 중심, 크기, 피해 (역행의 취소된 공격 기록용)
    public static event Action<Vector2, Vector2, int> HitLanded;

    // 고정 추가 피해. 다음 적중 1회에 적용 (빗나가면 유지)
    public void GrantNextBonus(int amount, float duration)
    {
        bonus = amount;
        bonusUntil = Time.time + duration;
    }

    public void ClearBonus() => bonus = 0;

    public void AddOneShotBonus(float amount, float duration) => oneShot.Add((amount, Time.time + duration));

    SpriteAnimator anim;

    void Awake()
    {
        controller = GetComponent<PlayerController>();
        anim = GetComponent<SpriteAnimator>();
        Rewindables.All.Add(this);
    }

    void OnDestroy() => Rewindables.All.Remove(this);

    public object Capture()
    {
        var o = new (float, float)[oneShot.Count];
        for (int i = 0; i < o.Length; i++) o[i] = (oneShot[i].amount, Mathf.Max(oneShot[i].until - Time.time, 0f));
        return new Snapshot
        {
            readyLeft = Mathf.Max(readyAt - Time.time, 0f),
            bonus = bonus,
            bonusLeft = Mathf.Max(bonusUntil - Time.time, 0f),
            oneShot = o,
        };
    }

    public void Restore(object state)
    {
        var s = (Snapshot)state;
        readyAt = Time.time + s.readyLeft;
        bonus = s.bonus;
        bonusUntil = Time.time + s.bonusLeft;
        oneShot.Clear();
        foreach (var (amount, left) in s.oneShot) oneShot.Add((amount, Time.time + left));
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null || !controller.CanAttack || !kb.aKey.wasPressedThisFrame || Time.time < readyAt) return;
        readyAt = Time.time + interval;
        SoundPlayer.Play("sfx_sword_swing", 3);
        if (anim) anim.PlayOnce("attack");

        float swingBonus = 0f;
        foreach (var (amount, until) in oneShot)
            if (Time.time < until) swingBonus += amount;
        oneShot.Clear();

        // 공격력 × (1 + 피해 증가% 합) × 치명타 + 고정 추가 피해 (CombatDecisions 2장)
        LastHitCrit = UnityEngine.Random.value < critChance;
        float scaled = damage * (1f + DamageBonus + swingBonus) * (LastHitCrit ? critMultiplier : 1f);
        int total = Health.Round(scaled + (HasBonus ? bonus : 0));

        Vector2 center = (Vector2)transform.position + new Vector2(controller.Facing * (transform.localScale.x + range.x) * 0.5f, 0f);
        bool landed = false;
        foreach (var hit in Physics2D.OverlapBoxAll(center, range, 0f))
            if (hit.gameObject != gameObject && hit.TryGetComponent(out Health target) && !target.IsDead)
            {
                target.TakeHit(total, transform.position);
                SoundPlayer.Play("sfx_sword_hit", 3);
                landed = true;
            }
        if (!landed) return;
        bonus = 0;
        HitLanded?.Invoke(center, range, total);
    }
}
