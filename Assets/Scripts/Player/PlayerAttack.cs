using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerController))]
public class PlayerAttack : MonoBehaviour
{
    [SerializeField] int damage = 10;
    [SerializeField] float interval = 0.4f;
    [SerializeField] Vector2 range = new(1.5f, 1f);
    [SerializeField, Range(0f, 1f)] float critChance = 0.05f;
    [SerializeField] float critMultiplier = 1.5f;

    PlayerController controller;
    float readyAt;
    int bonus;
    float bonusUntil;

    public bool HasBonus => bonus > 0 && Time.time < bonusUntil;
    // 피해 증가% 합(0.08 = +8%). 특성·유물이 더한다
    public float DamageBonus { get; set; }
    public bool LastHitCrit { get; private set; }

    // 다음 공격 1회에만 적용, 휘두르면 소모
    public void GrantNextBonus(int amount, float duration)
    {
        bonus = amount;
        bonusUntil = Time.time + duration;
    }

    public void ClearBonus() => bonus = 0;

    SpriteAnimator anim;

    void Awake()
    {
        controller = GetComponent<PlayerController>();
        anim = GetComponent<SpriteAnimator>();
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null || !controller.CanAttack || !kb.aKey.wasPressedThisFrame || Time.time < readyAt) return;
        readyAt = Time.time + interval;
        SoundPlayer.Play("sfx_sword_swing", 3);
        if (anim) anim.PlayOnce("attack");
        // 공격력 × (1 + 피해 증가% 합) × 치명타 + 고정 추가 피해 (CombatDecisions 2장)
        LastHitCrit = Random.value < critChance;
        float scaled = damage * (1f + DamageBonus) * (LastHitCrit ? critMultiplier : 1f);
        int total = Health.Round(scaled + (HasBonus ? bonus : 0));
        bonus = 0;

        Vector2 center = (Vector2)transform.position + new Vector2(controller.Facing * (transform.localScale.x + range.x) * 0.5f, 0f);
        foreach (var hit in Physics2D.OverlapBoxAll(center, range, 0f))
            if (hit.gameObject != gameObject && hit.TryGetComponent(out Health target))
            {
                target.TakeHit(total, transform.position);
                SoundPlayer.Play("sfx_sword_hit", 3);
            }
    }
}
