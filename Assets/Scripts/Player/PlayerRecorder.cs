using System;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

// 예지 보스 학습용 플레이어 이동 기록. F9로 켜고 끔.
// 사망·스테이지 변경마다 세션(파일)을 나눠 한 시퀀스에 순간이동이 섞이지 않게 한다.
public class PlayerRecorder : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (FindAnyObjectByType<PlayerRecorder>()) return;
        new GameObject("PlayerRecorder").AddComponent<PlayerRecorder>();
    }

    [SerializeField] float sampleInterval = 0.05f;
    [SerializeField] bool recording;

    const string Header = "t,x,y,vx,vy,grounded,facing,left,right,jump,attack,dash,has_boss,boss_dx,boss_dy,stage,day,hp";

    GameFlow flow;
    PlayerController player;
    Rigidbody2D body;
    ContactFilter2D groundFilter;
    StreamWriter writer;
    string runId;
    int sessionIndex;
    float nextSample;
    bool left, right, jump, attack, dash;

    static string Folder =>
#if UNITY_EDITOR
        Path.Combine(Application.dataPath, "..", "ML", "data", "raw");
#else
        Path.Combine(Application.persistentDataPath, "recordings");
#endif

    void Awake()
    {
        groundFilter.SetNormalAngle(45f, 135f);
        runId = DateTime.Now.ToString("yyyyMMdd_HHmmss");
    }

    void Start()
    {
        flow = FindAnyObjectByType<GameFlow>();
        if (!flow) return;
        flow.StageChanged += OnStageChanged;
        flow.StateChanged += OnStateChanged;
    }

    void OnDestroy()
    {
        if (flow)
        {
            flow.StageChanged -= OnStageChanged;
            flow.StateChanged -= OnStateChanged;
        }
        EndSession();
    }

    void OnStageChanged(int _) => EndSession();
    void OnStateChanged(GameState _) => EndSession();

    void Update()
    {
        var kb = Keyboard.current;
        if (kb != null && kb.f9Key.wasPressedThisFrame)
        {
            recording = !recording;
            if (!recording) EndSession();
        }
        if (!recording || !flow || flow.State != GameState.Playing) return;
        if (!player)
        {
            player = FindAnyObjectByType<PlayerController>();
            if (!player) return;
            body = player.GetComponent<Rigidbody2D>();
        }

        // 샘플 사이에 눌린 입력도 놓치지 않게 누적
        if (kb != null)
        {
            left |= kb.leftArrowKey.isPressed;
            right |= kb.rightArrowKey.isPressed;
            jump |= kb.spaceKey.wasPressedThisFrame;
            attack |= kb.aKey.wasPressedThisFrame;
            dash |= kb.shiftKey.wasPressedThisFrame;
        }

        if (Time.time < nextSample) return;
        nextSample = Time.time + sampleInterval;
        if (writer == null) BeginSession();
        WriteRow();
        left = right = jump = attack = dash = false;
    }

    void BeginSession()
    {
        Directory.CreateDirectory(Folder);
        string path = Path.Combine(Folder, $"{runId}_{sessionIndex++:000}.csv");
        writer = new StreamWriter(path);
        writer.WriteLine(Header);
    }

    void EndSession()
    {
        writer?.Dispose();
        writer = null;
    }

    void WriteRow()
    {
        Vector2 pos = player.transform.position;
        Vector2 vel = body.linearVelocity;
        // ponytail: 보스 공통 타입이 생기면 교체. 지금은 축적 보스만 존재
        var boss = FindAnyObjectByType<AccumulationBoss>();
        Vector2 rel = boss ? (Vector2)boss.transform.position - pos : Vector2.zero;

        writer.WriteLine(string.Join(",",
            F(Time.time), F(pos.x), F(pos.y), F(vel.x), F(vel.y),
            B(body.IsTouching(groundFilter)), player.Facing,
            B(left), B(right), B(jump), B(attack), B(dash),
            B(boss), F(rel.x), F(rel.y),
            flow.Stage, flow.Persistent.day, player.Health.Hp));
    }

    static string F(float v) => v.ToString("F4", CultureInfo.InvariantCulture);
    static int B(bool v) => v ? 1 : 0;

    void OnGUI()
    {
        if (recording) GUI.Label(new Rect(10, Screen.height - 30, 200, 20), writer != null ? "● REC" : "○ REC 대기");
    }
}
