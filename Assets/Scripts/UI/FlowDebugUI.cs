using UnityEngine;
using UnityEngine.InputSystem;

// 디버그 화면(F1로 켜고 끔): 상태 확인, 방 건너뛰기, 강제 사망
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
        GUILayout.BeginArea(new Rect(Screen.width - 270, Screen.height - 310, 260, 300), GUI.skin.box);
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
                GUILayout.Label($"능력: {flow.SelectedAbility.displayName}");
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

        GUILayout.EndArea();
    }
}
