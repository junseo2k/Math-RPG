using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerController))]
public class PlayerAttack : MonoBehaviour
{
    [SerializeField] int damage = 1;
    [SerializeField] float interval = 0.35f;
    [SerializeField] Vector2 range = new(1.2f, 1f);

    PlayerController controller;
    float readyAt;
    int bonus;
    float bonusUntil;

    public bool HasBonus => bonus > 0 && Time.time < bonusUntil;

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
        if (kb == null || !controller.CanAct || !kb.aKey.wasPressedThisFrame || Time.time < readyAt) return;
        readyAt = Time.time + interval;
        SoundPlayer.Play("sfx_sword_swing", 3);
        if (anim) anim.PlayOnce("attack");
        int total = damage + (HasBonus ? bonus : 0);
        bonus = 0;

        Vector2 center = (Vector2)transform.position + new Vector2(controller.Facing * (transform.localScale.x + range.x) * 0.5f, 0f);
        foreach (var hit in Physics2D.OverlapBoxAll(center, range, 0f))
            if (hit.gameObject != gameObject && hit.TryGetComponent(out Health target))
            {
                target.TakeDamage(total);
                SoundPlayer.Play("sfx_sword_hit", 3);
            }
    }
}
