using System;
using System.Collections.Generic;
using UnityEngine;

public class Health : MonoBehaviour, IRewindable
{
    struct Snapshot
    {
        public int hp;
        public int shield;
        public float invincibleLeft;
        public bool invincible;
        public bool dodging;
        public (float amount, float left)[] reductions;
    }

    [SerializeField] int maxHp = 5;
    [SerializeField] float hitInvincibleTime;

    public int Hp { get; private set; }
    public int MaxHp => maxHp;
    public bool IsDead => Hp <= 0;
    // 지속 시간 없음, HP보다 먼저 소모 (CombatDecisions 2장). 스테이지 종료 시 소멸은 RewardEffects가 처리
    public int Shield { get; private set; }
    // 대시처럼 외부에서 켜는 무적
    public bool Invincible { get; set; }
    // 회피 무적 중 맞으면 회피 성공 (Invincible과 함께 켠다)
    public bool Dodging { get; set; }
    // 회피 무적 + 피격·역행 직후 무적. 표시용
    public bool IsInvincible => Invincible || Time.time < invincibleUntil;

    public event Action<int> Damaged;
    public event Action Died;
    // 피격 위치(넉백 방향용). 사망한 피격에는 호출하지 않음
    public event Action<Vector2> Hit;
    public event Action DodgeSucceeded;
    // 모든 Health의 피해(감소 적용 후, 보호막 흡수 포함). 역행의 취소된 피해 기록용
    public static event Action<Health, int> AnyDamaged;

    // HP는 정수. 최종 피해만 반올림, 최소 1 (CombatDecisions 2장 승인)
    public static int Round(float damage) => Mathf.Max(1, Mathf.RoundToInt(damage));

    float invincibleUntil;
    // 일시적인 받는 피해 감소(%, 만료 시각)
    readonly List<(float amount, float until)> reductions = new();

    void Awake()
    {
        Hp = maxHp;
        Rewindables.All.Add(this);
    }

    void OnDestroy() => Rewindables.All.Remove(this);

    public object Capture()
    {
        var r = new (float, float)[reductions.Count];
        for (int i = 0; i < r.Length; i++) r[i] = (reductions[i].amount, Mathf.Max(reductions[i].until - Time.time, 0f));
        return new Snapshot
        {
            hp = Hp,
            shield = Shield,
            invincibleLeft = Mathf.Max(invincibleUntil - Time.time, 0f),
            invincible = Invincible,
            dodging = Dodging,
            reductions = r,
        };
    }

    public void Restore(object state)
    {
        var s = (Snapshot)state;
        Hp = s.hp;
        Shield = s.shield;
        invincibleUntil = Time.time + s.invincibleLeft;
        Invincible = s.invincible;
        Dodging = s.dodging;
        reductions.Clear();
        foreach (var (amount, left) in s.reductions) reductions.Add((amount, Time.time + left));
    }

    // 회피가 아닌 무적(역행 직후 등). 회피 성공은 발생하지 않음
    public void GrantInvincible(float seconds) => invincibleUntil = Mathf.Max(invincibleUntil, Time.time + seconds);

    public void AddShield(int amount) => Shield += Mathf.Max(amount, 0);
    public void ClearShield() => Shield = 0;
    public void AddReduction(float amount, float duration) => reductions.Add((amount, Time.time + duration));
    // 범위 안에 있는 동안 켜지는 받는 피해 감소%(태양핵). 시간 제한 감소와 합산
    public float ZoneReduction { get; set; }

    public void Heal(int amount)
    {
        if (IsDead) return;
        Hp = Mathf.Min(Hp + amount, maxHp);
    }

    // 최대 HP가 늘면 늘어난 만큼 현재 HP도 증가
    public void SetMaxHp(int max)
    {
        max = Mathf.Max(max, 1);
        int delta = max - maxHp;
        maxHp = max;
        if (delta > 0 && !IsDead) Hp += delta;
        Hp = Mathf.Min(Hp, maxHp);
    }

    public void Init(int max, float hitInvincible)
    {
        maxHp = max;
        hitInvincibleTime = hitInvincible;
        ResetHealth();
    }

    public void TakeDamage(int amount)
    {
        if (IsDead || Invincible || Time.time < invincibleUntil) return;
        invincibleUntil = Time.time + hitInvincibleTime;
        Apply(amount);
    }

    public void TakeHit(int amount, Vector2 from)
    {
        if (IsDead) return;
        if (Dodging) DodgeSucceeded?.Invoke();
        if (Invincible || Time.time < invincibleUntil) return;
        TakeDamage(amount);
        if (!IsDead) Hit?.Invoke(from);
    }

    // 무적을 무시하는 피해(유예 피해 적용 등)
    public void ApplyDirect(int amount)
    {
        if (IsDead) return;
        Apply(amount);
    }

    // 받는 피해 = 피해 × (1 − 감소% 합) → 보호막 먼저 → HP (CombatDecisions 2장)
    void Apply(int amount)
    {
        float reduction = ZoneReduction;
        reductions.RemoveAll(r => Time.time >= r.until);
        foreach (var r in reductions) reduction += r.amount;
        if (reduction > 0f) amount = Round(amount * Mathf.Max(1f - reduction, 0f));

        int absorbed = Mathf.Min(Shield, amount);
        Shield -= absorbed;
        Hp = Mathf.Max(Hp - (amount - absorbed), 0);
        AnyDamaged?.Invoke(this, amount);
        Damaged?.Invoke(amount);
        if (Hp == 0) Died?.Invoke();
    }

    public void ResetHealth()
    {
        Hp = maxHp;
        Shield = 0;
        Invincible = false;
        Dodging = false;
        invincibleUntil = 0f;
        reductions.Clear();
    }
}
