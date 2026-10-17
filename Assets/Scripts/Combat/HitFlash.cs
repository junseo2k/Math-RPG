using UnityEngine;

// 피격 깜빡임(플레이어·보스 공용, 표시 전용). 다른 스크립트가 몸 색을 칠한 뒤에 덮어쓰도록 늦게 실행
// 역행하면 취소된 피격의 깜빡임을 지운다
[DefaultExecutionOrder(100)]
[RequireComponent(typeof(SpriteRenderer), typeof(Health))]
public class HitFlash : MonoBehaviour
{
    [SerializeField] Color flashColor = Color.white;
    [SerializeField] float flashTime = 0.1f;

    SpriteRenderer view;
    Health health;
    RewindSystem rewind;
    float flashUntil;
    bool overriding;
    Color saved;

    void Awake()
    {
        view = GetComponent<SpriteRenderer>();
        health = GetComponent<Health>();
        health.Damaged += OnDamaged;
    }

    void Start()
    {
        rewind = FindAnyObjectByType<RewindSystem>();
        if (rewind) rewind.Rewound += OnRewound;
    }

    void OnDestroy()
    {
        if (rewind) rewind.Rewound -= OnRewound;
    }

    void OnDamaged(int _) => flashUntil = Time.time + flashTime;

    void OnRewound(RewindSystem.RewindInfo _) => flashUntil = 0f;

    void LateUpdate()
    {
        if (Time.time < flashUntil && !health.IsDead)
        {
            if (!overriding)
            {
                saved = view.color;
                overriding = true;
            }
            view.color = flashColor;
            return;
        }
        if (!overriding) return;
        overriding = false;
        view.color = saved;
    }
}
