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

    public event Action<int> Damaged;
    public event Action Died;

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
        invincibleUntil = 0f;
    }
}
