using UnityEngine;

// 스트립 애니메이션: Resources/Art/{BaseName}_{상태}.png. 스트립이 없으면 현재 그림을 그대로 둔다.
// 상태 이름: Docs/AssetProductionSheet.md 3-1
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteAnimator : MonoBehaviour
{
    enum Mode { Loop, Once, Hold }

    [SerializeField] float fps = 10f;

    public string BaseName { get; set; }

    SpriteRenderer view;
    Sprite[] frames;
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
        if (dir != 0) view.flipX = dir < 0;
    }

    void Begin(string next, Mode nextMode)
    {
        state = next;
        mode = nextMode;
        startTime = Time.time;
        frames = string.IsNullOrEmpty(BaseName) ? null
            : AssetLibrary.LoadFrames($"{BaseName}_{next}", Mathf.Abs(transform.lossyScale.x));
    }

    void Update()
    {
        int index = Mathf.FloorToInt((Time.time - startTime) * fps);
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
