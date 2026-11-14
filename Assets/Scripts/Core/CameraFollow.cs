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
    UnityEngine.Rendering.Universal.PixelPerfectCamera ppc;

    void Awake() => cam = GetComponent<Camera>();

    // 픽셀 퍼펙트 카메라는 그릴 때 화면 크기에 맞춰 orthographicSize를 바꾼다. 첫 프레임엔 아직 씬 값이라
    // 그 값으로 경계를 잡으면 실제 화면이 더 넓어 방 밖이 잠깐 보였다(2026-11-13) → 같은 계산을 미리 한다
    float HalfHeight()
    {
        if (!ppc) ppc = GetComponent<UnityEngine.Rendering.Universal.PixelPerfectCamera>();
        if (!ppc || !ppc.enabled || ppc.refResolutionX <= 0 || ppc.refResolutionY <= 0) return cam.orthographicSize;
        int zoom = Mathf.Max(1, Mathf.Min(cam.pixelWidth / ppc.refResolutionX, cam.pixelHeight / ppc.refResolutionY));
        return cam.pixelHeight / (2f * ppc.assetsPPU * zoom);
    }

    void LateUpdate()
    {
        if (!Target) return;
        float halfH = HalfHeight();
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
