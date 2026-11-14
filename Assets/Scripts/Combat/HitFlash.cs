using UnityEngine;

// 피격 번쩍임 + 무적 중 깜빡임(플레이어·보스 공용, 표시 전용). 다른 스크립트가 몸 색을 칠한 뒤에 덮어쓰도록 늦게 실행
// 역행하면 취소된 피격의 깜빡임을 지운다
[DefaultExecutionOrder(100)]
[RequireComponent(typeof(SpriteRenderer), typeof(Health))]
public class HitFlash : MonoBehaviour
{
    [SerializeField] Color flashColor = Color.white;
    [SerializeField] float flashTime = 0.1f;
    // 무적인 동안 켜고 끄는 주기(초)
    [SerializeField] float blinkInterval = 0.08f;

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
        view.enabled = health.IsDead || !health.IsInvincible || Mathf.FloorToInt(Time.time / blinkInterval) % 2 == 0;

        if (Time.time < flashUntil && !health.IsDead)
        {
            if (!overriding)
            {
                saved = view.color;
                savedMaterial = view.sharedMaterial;
                overriding = true;
            }
            view.color = flashColor;
            if (Silhouette) view.sharedMaterial = Silhouette;
            return;
        }
        if (!overriding) return;
        overriding = false;
        view.color = saved;
        view.sharedMaterial = savedMaterial;
    }

    // 그림이 있는 스프라이트는 흰색을 곱해도 그대로라 번쩍임이 안 보인다 → 알파 모양을 색 하나로 칠하는 내장 셰이더(2026-11-08 타격감).
    // 빌드에서 빠져 없으면 색만 바꾼다
    static Material silhouette;
    static Material Silhouette
    {
        get
        {
            if (!silhouette && Shader.Find("GUI/Text Shader") is { } shader) silhouette = new Material(shader);
            return silhouette;
        }
    }

    Material savedMaterial;
}
