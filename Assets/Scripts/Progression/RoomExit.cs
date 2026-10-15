using UnityEngine;

// 잠김: 닫힌 문 + 남은 적 수 / 개방: 밝게 빛남. 개방 상태에서 플레이어가 닿으면 다음 스테이지
[RequireComponent(typeof(BoxCollider2D), typeof(SpriteRenderer))]
public class RoomExit : MonoBehaviour
{
    static readonly Color LockedColor = new(0.35f, 0.25f, 0.2f);
    static readonly Color OpenColor = new(1f, 0.9f, 0.4f);

    public bool IsOpen { get; private set; }
    public int Remaining { get; set; }

    SpriteRenderer view;
    GameFlow flow;

    void Awake()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
        view = GetComponent<SpriteRenderer>();
        flow = FindAnyObjectByType<GameFlow>();
        SetOpen(false);
    }

    public void SetOpen(bool open)
    {
        IsOpen = open;
        view.color = open ? OpenColor : LockedColor;
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (!IsOpen || !other.GetComponent<PlayerController>()) return;
        IsOpen = false;
        if (flow) flow.ClearStage();
    }

    void OnGUI()
    {
        var cam = Camera.main;
        if (!cam) return;
        Vector3 s = cam.WorldToScreenPoint(transform.position + Vector3.up * 1.6f);
        if (s.z < 0f) return;
        GUI.Label(new Rect(s.x - 50f, Screen.height - s.y - 10f, 100f, 20f), IsOpen ? "출구" : $"남은 적 {Remaining}");
    }
}
