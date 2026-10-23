using UnityEngine;
using UnityEngine.InputSystem;

// 시험판 임시 시작 능력 효과.
[RequireComponent(typeof(PlayerController), typeof(PlayerAttack))]
public class AbilityEffects : MonoBehaviour
{
    [Header("temp_a 순간 정지장")]
    [SerializeField] float freezeRadius = 3f;
    [SerializeField] float freezeDuration = 1.5f;
    [SerializeField] float freezeCooldown = 8f;

    [Header("temp_b 잔광")]
    [SerializeField] int afterglowBonus = 1;
    [SerializeField] float afterglowWindow = 1f;

    [Header("temp_c 가벼운 발걸음")]
    [SerializeField] float moveSpeedBonus = 0.2f;
    [SerializeField] float dashCooldownReduction = 0.3f;

    PlayerController controller;
    PlayerAttack attack;
    GameFlow flow;
    string active;
    float freezeReadyAt;

    void Awake()
    {
        controller = GetComponent<PlayerController>();
        attack = GetComponent<PlayerAttack>();
        controller.Dashed += OnDashed;
    }

    void Start()
    {
        flow = FindAnyObjectByType<GameFlow>();
        if (!flow) return;
        flow.AbilitySelected += Apply;
        flow.RunReset += Clear;
    }

    void OnDestroy()
    {
        if (!flow) return;
        flow.AbilitySelected -= Apply;
        flow.RunReset -= Clear;
    }

    void Apply(StartAbilityDef def)
    {
        Clear();
        active = def.id;
        if (active == "temp_c")
        {
            controller.MoveSpeedMultiplier = 1f + moveSpeedBonus;
            controller.DashCooldownMultiplier = 1f - dashCooldownReduction;
        }
    }

    void Clear()
    {
        active = null;
        freezeReadyAt = 0f;
        controller.MoveSpeedMultiplier = 1f;
        controller.DashCooldownMultiplier = 1f;
        attack.ClearBonus();
    }

    void OnDashed()
    {
        if (active == "temp_b") attack.GrantNextBonus(afterglowBonus, afterglowWindow);
    }

    void Update()
    {
        if (active != "temp_a" || !controller.CanAct) return;
        var mouse = Mouse.current;
        if (mouse == null || !mouse.rightButton.wasPressedThisFrame || Time.time < freezeReadyAt) return;

        freezeReadyAt = Time.time + freezeCooldown;
        foreach (var hit in Physics2D.OverlapCircleAll(transform.position, freezeRadius))
            if (hit.TryGetComponent(out IFreezable target))
                target.Freeze(freezeDuration);
        ShowField();
        SoundPlayer.Play("sfx_freeze");
    }

    void ShowField()
    {
        var view = new GameObject("FreezeField").AddComponent<SpriteRenderer>();
        view.sprite = AssetLibrary.SpriteUnitsOr("fx_freeze_field", 2f, AccumulationBoss.CircleSprite());
        view.color = new Color(0.4f, 0.7f, 1f, 0.3f);
        view.sortingOrder = -1;
        view.transform.position = transform.position;
        view.transform.localScale = Vector3.one * freezeRadius;
        Destroy(view.gameObject, 0.25f);
    }

    void OnGUI()
    {
        string text = active switch
        {
            "temp_a" => Time.time >= freezeReadyAt ? "정지장 [S] 준비" : $"정지장 {freezeReadyAt - Time.time:0.0}초",
            "temp_b" => attack.HasBonus ? "잔광 활성!" : "잔광: 대시 후 공격",
            "temp_c" => "가벼운 발걸음 적용 중",
            _ => null,
        };
        if (text != null) GUI.Label(new Rect(10, Screen.height - 55, 260, 20), text);
    }
}
