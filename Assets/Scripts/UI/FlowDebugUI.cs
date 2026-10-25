using UnityEngine;
using UnityEngine.InputSystem;

// 디버그 화면(F1로 켜고 끔): 상태 확인, 방 건너뛰기, 강제 사망, 스테이지 이동
[RequireComponent(typeof(GameFlow))]
public class FlowDebugUI : MonoBehaviour
{
    [SerializeField] GameFlow flow;
    [SerializeField] bool visible;

    void Awake()
    {
        if (!flow) flow = GetComponent<GameFlow>();
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb != null && kb.f1Key.wasPressedThisFrame) visible = !visible;
    }

    void OnGUI()
    {
        if (!visible) return;
        GUILayout.BeginArea(new Rect(Screen.width - 290, Screen.height - 410, 280, 400), GUI.skin.box);
        GUILayout.Label($"{flow.Persistent.day}일째  {flow.TimeText}");
        GUILayout.Label($"사망 {flow.Persistent.deathCount}회  보스 만남 [{string.Join(", ", flow.Persistent.bossEncounters)}]");
        GUILayout.Label($"상태: {flow.State}");

        switch (flow.State)
        {
            case GameState.AbilitySelect:
                for (int i = 0; i < flow.Choices.Count; i++)
                    if (GUILayout.Button($"{flow.Choices[i].displayName} ({flow.Choices[i].kind})")) flow.SelectAbility(i);
                break;
            case GameState.Playing:
                if (flow.SelectedAbility != null) GUILayout.Label($"능력: {flow.SelectedAbility.displayName}");
                GUILayout.Label($"스테이지 {flow.Stage}/{flow.StageCount}{(flow.IsBossStage ? " (보스)" : "")}");
                if (GUILayout.Button(flow.IsBossStage ? "보스 처치" : "방 클리어")) flow.ClearStage();
                if (GUILayout.Button("사망")) flow.PlayerDied();
                break;
            case GameState.Dead:
                GUILayout.Label("재탄생 대기 중...");
                break;
            case GameState.Cleared:
                GUILayout.Label("시험판 클리어");
                break;
        }

        // 보스 단위로 한 줄씩. 누르면 특성·유물·G를 초기화한 새 판으로 그 스테이지부터 시작
        GUILayout.Space(8);
        GUILayout.Label("스테이지 이동 (새 판)");
        int boss = 0;
        GUILayout.BeginHorizontal();
        for (int s = 1; s <= flow.StageCount; s++)
        {
            bool isBoss = flow.IsBossStageAt(s);
            if (GUILayout.Button(isBoss ? $"보스{++boss}" : s.ToString())) flow.DebugStartAt(s);
            if (isBoss && s < flow.StageCount)
            {
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
            }
        }
        GUILayout.EndHorizontal();

        GUILayout.EndArea();
    }
}
