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
        public float pendingLeft;
        public Vector2 pendingDir;
        public int pendingDamage;
        public bool pendingCrit;
        public int pendingStep;
        public int comboStep;
        public float comboLeft;
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
    // 휘두른 뒤 판정 대기 중인 공격. 방향·피해는 클릭 순간 값 (hitAt < 0 = 없음)
    float hitAt = -1f;
    Vector2 pendingDir;
    int pendingDamage;
    bool pendingCrit;
    int pendingStep;
    // 3타 콤보: 마지막으로 휘두른 타(0~2, -1 = 없음)와 다음 타를 받는 마감 시각
    int comboStep = -1;
    float comboUntil;
    bool buffered;

    // 다음에 휘두를 타. 마감 안에 누르면 이어지고(3타 다음은 1타), 지나면 1타
    public static int NextStep(int lastStep, float now, float comboUntil, int count) =>
        lastStep >= 0 && now <= comboUntil ? (lastStep + 1) % count : 0;

    public int ComboStep => comboStep;

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
        hitAt = -1f;
        comboStep = -1;
        comboUntil = 0f;
        buffered = false;
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
            pendingLeft = hitAt < 0f ? -1f : Mathf.Max(hitAt - Time.time, 0f),
            pendingDir = pendingDir,
            pendingDamage = pendingDamage,
            pendingCrit = pendingCrit,
            pendingStep = pendingStep,
            comboStep = comboStep,
            comboLeft = comboUntil - Time.time,
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
        hitAt = s.pendingLeft < 0f ? -1f : Time.time + s.pendingLeft;
        pendingDir = s.pendingDir;
        pendingDamage = s.pendingDamage;
        pendingCrit = s.pendingCrit;
        pendingStep = s.pendingStep;
        // 콤보 단계도 역행 복원 대상(CombatDecisions A-2)
        comboStep = s.comboStep;
        comboUntil = Time.time + s.comboLeft;
        buffered = false;
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
        if (hitAt >= 0f && Time.time >= hitAt) Strike();

        var mouse = Mouse.current;
        bool click = mouse != null && mouse.leftButton.wasPressedThisFrame;
        if (!controller.CanAttack)
        {
            buffered = false;
            return;
        }
        // 다음 클릭 가능 시점 직전의 입력은 모아 두었다가 그 시점에 실행
        if (click && Time.time < readyAt && readyAt - Time.time <= B.inputBuffer) buffered = true;
        if (Time.time < readyAt || !(click || buffered)) return;
        buffered = false;
        Swing();
    }

    void Swing()
    {
        var combo = B.combo;
        int step = NextStep(comboStep, Time.time, comboUntil, combo.Length);
        var hit = combo[step];
        comboStep = step;
        readyAt = Time.time + hit.interval;
        comboUntil = readyAt + B.comboWindow;
        SoundPlayer.Play("sfx_sword_swing", 3);
        if (anim)
        {
            // 그 방향 3연격 그림이 없으면 기존 단일 공격 그림(2026-11-08 사용자: 동쪽만 3연격)
            string state = "attack" + (step + 1);
            anim.SetFps(state, B.comboFps);
            anim.PlayOnce(anim.Has(state) ? state : "attack");
        }

        float swingBonus = 0f;
        foreach (var (amount, until) in oneShot)
            if (Time.time < until) swingBonus += amount;
        oneShot.Clear();

        // 공격력 × (1 + 피해 증가% 합) × 치명타 + 고정 추가 피해 (CombatDecisions 2장)
        pendingCrit = UnityEngine.Random.value < B.critChance;
        float scaled = B.damage * hit.damageMultiplier * (1f + DamageBonus + ZoneDamageBonus + swingBonus) * (pendingCrit ? B.critMultiplier : 1f);
        pendingDamage = Health.Round(scaled + (HasBonus ? bonus : 0));
        pendingDir = controller.Aim;
        pendingStep = step;
        hitAt = Time.time + hit.hitDelay;
        if (hitAt <= Time.time) Strike();
    }

    // 내리치는 프레임에 판정. 그 전에 경직·사망하면 취소
    void Strike()
    {
        hitAt = -1f;
        if (!controller.CanAttack) return;
        LastHitCrit = pendingCrit;
        int total = pendingDamage;
        Vector2 center = (Vector2)transform.position + pendingDir * (transform.localScale.x + B.range.x) * 0.5f;
        swingCenter = center;
        swingAngle = Mathf.Atan2(pendingDir.y, pendingDir.x) * Mathf.Rad2Deg;
        swingShownUntil = Time.time + swingShowTime;
        bool landed = false;
        float stop = B.combo[Mathf.Clamp(pendingStep, 0, B.combo.Length - 1)].hitStop;
        foreach (var hit in Physics2D.OverlapBoxAll(center, B.range, swingAngle))
            if (hit.gameObject != gameObject && hit.TryGetComponent(out Health target) && !target.IsDead)
            {
                target.TakeHit(total, transform.position);
                SoundPlayer.Play("sfx_sword_hit", 3);
                landed = true;
                // 타격감: 맞은 적 애니메이션 정지(하얀 번쩍임은 그 적의 HitFlash가 피해를 받으면 처리)
                if (hit.TryGetComponent(out SpriteAnimator targetAnim)) targetAnim.Freeze(stop);
            }
        if (!landed) return;
        // 히트스톱: 내 애니메이션도 멈추고, 멈춘 만큼 다음 타 입력 시점·마감을 미룬다
        if (anim) anim.Freeze(stop);
        readyAt += stop;
        comboUntil += stop;
        bonus = 0;
        HitLanded?.Invoke(center, B.range, swingAngle, total);
    }
}
