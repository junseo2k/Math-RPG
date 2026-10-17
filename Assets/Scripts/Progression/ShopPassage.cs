using UnityEngine;
using UnityEngine.InputSystem;

// 숨겨진 시간의 통로: 보스 직전 일반 스테이지 클리어 후 입구 뒤쪽에 열림. 위 방향키로 진입 (CombatDecisions 11장)
// 상점에서 나오면 닫히고, 다음 스테이지로 가면 방과 함께 사라진다(이번 순환 기회 상실)
[RequireComponent(typeof(BoxCollider2D), typeof(SpriteRenderer))]
public class ShopPassage : MonoBehaviour
{
    public static readonly Color HiddenColor = new(0.6f, 0.85f, 1f, 0.35f);

    [SerializeField] float pulseSpeed = 2f;

    GameFlow flow;
    SpriteRenderer view;
    bool playerInside;

    void Awake()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
        view = GetComponent<SpriteRenderer>();
        flow = FindAnyObjectByType<GameFlow>();
        if (flow) flow.StateChanged += OnStateChanged;
        SoundPlayer.Play("sfx_passage_open");
    }

    void OnDestroy()
    {
        if (flow) flow.StateChanged -= OnStateChanged;
    }

    void OnStateChanged(GameState state)
    {
        // 상점에서 나와 전투 화면으로 돌아오면 닫힘
        if (state == GameState.Playing && entered) Destroy(gameObject);
    }

    bool entered;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<PlayerController>()) playerInside = true;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponent<PlayerController>()) playerInside = false;
    }

    void Update()
    {
        // 은은한 깜빡임 단서
        var c = HiddenColor;
        c.a = HiddenColor.a * (0.6f + 0.4f * Mathf.Sin(Time.time * pulseSpeed));
        view.color = c;

        var kb = Keyboard.current;
        if (!playerInside || entered || kb == null || !kb.upArrowKey.wasPressedThisFrame || !flow) return;
        if (flow.State != GameState.Playing) return;
        entered = true;
        flow.EnterShop();
    }

    void OnGUI()
    {
        if (!playerInside || entered) return;
        var cam = Camera.main;
        if (!cam) return;
        Vector3 s = cam.WorldToScreenPoint(transform.position + Vector3.up * 1.6f);
        GUI.Label(new Rect(s.x - 70f, Screen.height - s.y - 10f, 140f, 20f), "↑ 시간의 통로");
    }
}
