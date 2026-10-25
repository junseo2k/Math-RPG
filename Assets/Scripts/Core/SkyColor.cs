using UnityEngine;

// 시각에 따른 밤하늘 색(카메라 배경). 규칙: Docs/Art/ArtSpecification.md 3장
[RequireComponent(typeof(Camera))]
public class SkyColor : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap() => GameScene.WhenLoaded(() =>
    {
        var cam = Camera.main;
        if (cam && !cam.GetComponent<SkyColor>()) cam.gameObject.AddComponent<SkyColor>();
    });

    // 18:00 노을 → 19:00 → 20:00 → 21:00 남색 (그 이후는 마지막 색 유지)
    [SerializeField] Color[] hourColors =
    {
        new(0.85f, 0.45f, 0.35f),
        new(0.5f, 0.3f, 0.5f),
        new(0.22f, 0.2f, 0.42f),
        new(0.08f, 0.08f, 0.22f),
    };
    [SerializeField] float transition = 1f;

    Camera cam;
    GameFlow flow;
    Color from;
    Color to;
    float changedAt;
    int lastIndex = -1;

    void Awake()
    {
        cam = GetComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
    }

    void Update()
    {
        if (!flow) flow = FindAnyObjectByType<GameFlow>();
        if (!flow || hourColors.Length == 0) return;

        int index = Mathf.Clamp(flow.Run.elapsedHours, 0, hourColors.Length - 1);
        if (index != lastIndex)
        {
            from = lastIndex < 0 ? hourColors[index] : cam.backgroundColor;
            to = hourColors[index];
            changedAt = Time.time;
            lastIndex = index;
        }
        cam.backgroundColor = Color.Lerp(from, to, transition > 0f ? (Time.time - changedAt) / transition : 1f);
    }
}
