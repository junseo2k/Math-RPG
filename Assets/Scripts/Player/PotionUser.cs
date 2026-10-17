using UnityEngine;
using UnityEngine.InputSystem;

// 체력 회복 포션. Q키, 동작 없이 즉시, 경직 중 사용 가능 (CombatDecisions 1·11장)
[RequireComponent(typeof(PlayerController))]
public class PotionUser : MonoBehaviour, IRewindable
{
    struct Snapshot
    {
        public int count;
        public float readyLeft;
    }

    [SerializeField] int heal = 40;
    [SerializeField] int maxCount = 3;
    [SerializeField] float cooldown = 10f;
    [SerializeField] int startCount = 1;

    public int Count { get; private set; }
    public int MaxCount => maxCount;
    public bool CanAdd => Count < maxCount;
    public float CooldownLeft => Mathf.Max(readyAt - Time.time, 0f);

    PlayerController controller;
    GameFlow flow;
    float readyAt;

    void Awake()
    {
        controller = GetComponent<PlayerController>();
        Count = startCount;
        Rewindables.All.Add(this);
    }

    void Start()
    {
        flow = FindAnyObjectByType<GameFlow>();
        if (flow) flow.RunReset += ResetCount;
    }

    void OnDestroy()
    {
        Rewindables.All.Remove(this);
        if (flow) flow.RunReset -= ResetCount;
    }

    // 순환마다 시작 보유 수로 초기화
    void ResetCount()
    {
        Count = startCount;
        readyAt = 0f;
    }

    public void Add() => Count = Mathf.Min(Count + 1, maxCount);

    public object Capture() => new Snapshot { count = Count, readyLeft = CooldownLeft };

    public void Restore(object state)
    {
        var s = (Snapshot)state;
        Count = s.count;
        readyAt = Time.time + s.readyLeft;
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null || !kb.qKey.wasPressedThisFrame || !controller.CanAct) return;
        var health = controller.Health;
        if (Count <= 0 || health.Hp >= health.MaxHp || Time.time < readyAt) return;

        Count--;
        readyAt = Time.time + cooldown;
        float efficiency = flow && flow.Rewards ? flow.Rewards.GrowthValue(GrowthStat.PotionHeal) : 0f;
        health.Heal(Health.Round(heal * (1f + efficiency)));
        SoundPlayer.Play("sfx_potion");
    }
}
