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

    // 수치는 CombatBalance (Tools/전투 밸런스 열기)
    static CombatBalance.SwordStats B => CombatBalance.Instance.sword;

    public enum RangeDisplay { Off, OnSwing, Always }

    // 표시 전용: 판정과 같은 HitBox()를 그린다
    [Header("검 범위 표시")]
    [SerializeField] RangeDisplay rangeDisplay = RangeDisplay.OnSwing;
    [SerializeField] Color swingColor = new(1f, 1f, 1f, 0.35f);
    [SerializeField] float swingShowTime = 0.1f;
    [SerializeField] Color guideColor = new(1f, 1f, 1f, 0.12f);

    SpriteRenderer rangeView;
    float swingShownUntil;
    Vector2 swingCenter;
    float swingAngle;

    PlayerController controller;
    GameFlow flow;
    float readyAt;
    int bonus;
    float bonusUntil;
    // 다음 공격 1회 피해 증가%(만료 시각). 휘두르면 소모
    readonly List<(float amount, float until)> oneShot = new();

    public bool HasBonus => bonus > 0 && Time.time < bonusUntil;
    // 피해 증가% 합(0.08 = +8%). 특성·유물이 더한다
    public float DamageBonus { get; set; }
    // 범위 버프(태양핵)의 피해 증가%. Recalc가 DamageBonus를 덮어써도 유지된다
    public float ZoneDamageBonus { get; set; }
    public bool LastHitCrit { get; private set; }

    // 적중한 휘두름 1회마다: 판정 중심, 크기, 피해 (역행의 취소된 공격 기록용)
    public static event Action<Vector2, Vector2, float, int> HitLanded;

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

    void OnDestroy()
    {
        Rewindables.All.Remove(this);
        if (flow) flow.RunReset -= ResetAttack;
        if (rangeView) Destroy(rangeView.gameObject);
    }

    void Start()
    {
        flow = FindAnyObjectByType<GameFlow>();
        if (flow) flow.RunReset += ResetAttack;
    }

    void ResetAttack()
    {
        readyAt = 0f;
        bonus = 0;
        bonusUntil = 0f;
        oneShot.Clear();
        swingShownUntil = 0f;
        LastHitCrit = false;
    }

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
        // 취소된 휘두름 표시는 남기지 않음
        swingShownUntil = 0f;
    }

    // 실제 판정 상자. 공격과 표시가 같은 계산을 쓴다
    public Vector2 HitBoxCenter => (Vector2)transform.position + controller.Aim * (transform.localScale.x + B.range.x) * 0.5f;
    public Vector2 HitBoxSize => B.range;
    // 조준 방향으로 돌린 상자 각도(도)
    public float HitBoxAngle => Mathf.Atan2(controller.Aim.y, controller.Aim.x) * Mathf.Rad2Deg;

    void LateUpdate()
    {
        bool swing = rangeDisplay != RangeDisplay.Off && Time.time < swingShownUntil;
        bool guide = rangeDisplay == RangeDisplay.Always && controller.CanAct;
        if (!swing && !guide)
        {
            if (rangeView) rangeView.enabled = false;
            return;
        }
        if (!rangeView)
        {
            rangeView = new GameObject("SwordRange").AddComponent<SpriteRenderer>();
            rangeView.sprite = AssetLibrary.SpriteUnitsOr("fx_sword_range", 1f, RoomManager.Square);
            rangeView.sortingOrder = 2;
        }
        rangeView.enabled = true;
        rangeView.color = swing ? swingColor : guideColor;
        // 휘두름 표시는 판정한 순간의 위치에 고정
        rangeView.transform.position = swing ? swingCenter : HitBoxCenter;
        rangeView.transform.rotation = Quaternion.Euler(0f, 0f, swing ? swingAngle : HitBoxAngle);
        rangeView.transform.localScale = HitBoxSize;
    }

    void Update()
    {
        var mouse = Mouse.current;
        if (mouse == null || !controller.CanAttack || !mouse.leftButton.wasPressedThisFrame || Time.time < readyAt) return;
        readyAt = Time.time + B.interval;
        SoundPlayer.Play("sfx_sword_swing", 3);
        if (anim) anim.PlayOnce("attack");

        float swingBonus = 0f;
        foreach (var (amount, until) in oneShot)
            if (Time.time < until) swingBonus += amount;
        oneShot.Clear();

        // 공격력 × (1 + 피해 증가% 합) × 치명타 + 고정 추가 피해 (CombatDecisions 2장)
        LastHitCrit = UnityEngine.Random.value < B.critChance;
        float scaled = B.damage * (1f + DamageBonus + ZoneDamageBonus + swingBonus) * (LastHitCrit ? B.critMultiplier : 1f);
        int total = Health.Round(scaled + (HasBonus ? bonus : 0));

        Vector2 center = HitBoxCenter;
        swingCenter = center;
        swingAngle = HitBoxAngle;
        swingShownUntil = Time.time + swingShowTime;
        bool landed = false;
        foreach (var hit in Physics2D.OverlapBoxAll(center, B.range, swingAngle))
            if (hit.gameObject != gameObject && hit.TryGetComponent(out Health target) && !target.IsDead)
            {
                target.TakeHit(total, transform.position);
                SoundPlayer.Play("sfx_sword_hit", 3);
                landed = true;
            }
        if (!landed) return;
        bonus = 0;
        HitLanded?.Invoke(center, B.range, swingAngle, total);
    }
}
