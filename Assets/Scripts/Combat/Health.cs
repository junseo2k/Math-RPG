using System;
using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField] int maxHp = 5;
    [SerializeField] float hitInvincibleTime;

    public int Hp { get; private set; }
    public int MaxHp => maxHp;
    public bool IsDead => Hp <= 0;
    // 대시처럼 외부에서 켜는 무적
    public bool Invincible { get; set; }
    // 회피 무적 중 맞으면 회피 성공 (Invincible과 함께 켠다)
    public bool Dodging { get; set; }

    public event Action<int> Damaged;
    public event Action Died;
    // 피격 위치(넉백 방향용). 사망한 피격에는 호출하지 않음
    public event Action<Vector2> Hit;
    public event Action DodgeSucceeded;

    // HP는 정수. 최종 피해만 반올림, 최소 1 (CombatDecisions 2장 승인)
    public static int Round(float damage) => Mathf.Max(1, Mathf.RoundToInt(damage));

    float invincibleUntil;

    void Awake() => Hp = maxHp;

    public void Init(int max, float hitInvincible)
    {
        maxHp = max;
        hitInvincibleTime = hitInvincible;
        ResetHealth();
    }

    public void TakeDamage(int amount)
    {
        if (IsDead || Invincible || Time.time < invincibleUntil) return;
        Hp = Mathf.Max(Hp - amount, 0);
        invincibleUntil = Time.time + hitInvincibleTime;
        Damaged?.Invoke(amount);
        if (Hp == 0) Died?.Invoke();
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
        Hp = Mathf.Max(Hp - amount, 0);
        Damaged?.Invoke(amount);
        if (Hp == 0) Died?.Invoke();
    }

    public void ResetHealth()
    {
        Hp = maxHp;
        Invincible = false;
        Dodging = false;
        invincibleUntil = 0f;
    }
}
