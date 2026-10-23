using UnityEngine;

// 상하좌우 추적, 방 경계에서 멈춤
[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    public Transform Target { get; set; }
    public float MinX { get; set; }
    public float MaxX { get; set; }
    public float MinY { get; set; }
    public float MaxY { get; set; }

    Camera cam;

    void Awake() => cam = GetComponent<Camera>();

    void LateUpdate()
    {
        if (!Target) return;
        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;
        transform.position = new Vector3(
            Follow(Target.position.x, MinX, MaxX, halfW),
            Follow(Target.position.y, MinY, MaxY, halfH),
            -10f);
    }

    // 방이 화면보다 작으면 가운데 고정
    static float Follow(float target, float min, float max, float half) =>
        max - min <= half * 2f ? (min + max) * 0.5f : Mathf.Clamp(target, min + half, max - half);
}
