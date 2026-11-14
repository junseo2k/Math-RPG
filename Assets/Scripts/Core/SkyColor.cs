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

    // 바닥 밖(시작 화면·보스방 여백)에 보이는 색. 탑다운 지하수로라 노을 주황 대신 가장 어두운 석재색에서
    // 시각마다 조금씩 더 어둡게 (2026-11-13 사용자 "주황색 없애"). 그 이후는 마지막 색 유지
    [SerializeField] Color[] hourColors =
    {
        new(0.078f, 0.102f, 0.149f),   // #141a26 석재 가장 어두운 단
        new(0.063f, 0.078f, 0.122f),
        new(0.047f, 0.059f, 0.094f),
        new(0.031f, 0.039f, 0.071f),
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
