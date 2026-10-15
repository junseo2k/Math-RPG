using UnityEngine;

// 좌우만 추적, 방 양 끝에서 멈춤
[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    [SerializeField] float height = 4f;

    public Transform Target { get; set; }
    public float MinX { get; set; }
    public float MaxX { get; set; }

    Camera cam;

    void Awake() => cam = GetComponent<Camera>();

    void LateUpdate()
    {
        if (!Target) return;
        float half = cam.orthographicSize * cam.aspect;
        float x = MaxX - MinX <= half * 2f
            ? (MinX + MaxX) * 0.5f
            : Mathf.Clamp(Target.position.x, MinX + half, MaxX - half);
        transform.position = new Vector3(x, height, -10f);
    }
}
