using System.Collections.Generic;
using UnityEngine;

// 스트립 애니메이션: Resources/Art/{BaseName}_{상태}_{방향}.png를 먼저, 없으면 {BaseName}_{상태}.png(좌우 반전).
// 스트립이 없으면 현재 그림을 그대로 둔다. 상태 이름: Docs/Art/AssetProductionSheet.md 3-1
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteAnimator : MonoBehaviour
{
    enum Mode { Loop, Once, Hold }

    [SerializeField] float fps = 10f;

    public string BaseName { get; set; }
    public float Fps { get => fps; set => fps = value; }

    // 상태별 재생 속도. 지정하지 않은 상태는 fps
    readonly Dictionary<string, float> stateFps = new();
    public void SetFps(string stateName, float value) => stateFps[stateName] = value;
    float CurrentFps => state != null && stateFps.TryGetValue(state, out var f) ? f : fps;

    SpriteRenderer view;
    Sprite[] frames;
    // 방향별 스트립을 쓰는 중이면 좌우 반전 안 함
    bool directional;
    string direction;
    string state;
    string loopState = "idle";
    Mode mode;
    float startTime;

    void Awake() => view = GetComponent<SpriteRenderer>();

    // 아무 상태도 지정되지 않았으면 대기부터
    void Start()
    {
        if (state == null) Begin(loopState, Mode.Loop);
    }

    // 반복 상태. 1회 재생·유지 중에는 기억만 했다가 끝난 뒤 적용
    public void SetLoop(string next)
    {
        loopState = next;
        if (mode == Mode.Loop && next != state) Begin(next, Mode.Loop);
    }

    // 1회 재생 후 반복 상태로 복귀
    public void PlayOnce(string next) => Begin(next, Mode.Once);

    // 1회 재생 후 마지막 프레임 유지(사망 등). Release로 해제
    public void PlayHold(string next) => Begin(next, Mode.Hold);

    public void Release() => Begin(loopState, Mode.Loop);

    public void Face(int dir)
    {
        if (dir != 0 && !directional) view.flipX = dir < 0;
    }

    // 8방향 이름(s, sw, w, nw, n, ne, e, se). 재생 위치는 유지하고 그림만 바꾼다
    public void SetDirection(string dir)
    {
        if (dir == direction) return;
        direction = dir;
        if (state != null) LoadFrames();
    }

    void Begin(string next, Mode nextMode)
    {
        state = next;
        mode = nextMode;
        startTime = Time.time;
        LoadFrames();
    }

    void LoadFrames()
    {
        frames = null;
        if (string.IsNullOrEmpty(BaseName)) return;
        float scale = Mathf.Abs(transform.lossyScale.x);
        // 그 상태의 방향 그림이 아직 없으면 같은 방향의 대기 그림으로 대신한다
        if (direction != null)
            frames = AssetLibrary.LoadFrames($"{BaseName}_{state}_{direction}", scale)
                ?? AssetLibrary.LoadFrames($"{BaseName}_idle_{direction}", scale);
        directional = frames != null;
        if (directional)
        {
            view.flipX = false;
            return;
        }
        frames = AssetLibrary.LoadFrames($"{BaseName}_{state}", scale);
    }

    void Update()
    {
        int index = Mathf.FloorToInt((Time.time - startTime) * CurrentFps);
        if (frames == null || frames.Length == 0)
        {
            // 스트립 없는 1회 상태는 바로 반복 상태로
            if (mode == Mode.Once) Release();
            return;
        }

        if (index >= frames.Length)
        {
            if (mode == Mode.Once)
            {
                Release();
                return;
            }
            index = mode == Mode.Hold ? frames.Length - 1 : index % frames.Length;
        }
        view.sprite = frames[index];
    }
}
