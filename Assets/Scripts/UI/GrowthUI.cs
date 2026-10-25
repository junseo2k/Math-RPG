using UnityEngine;

// 영구 성장 1단계 "희미한 기억" (최소 기능, 임시 IMGUI). 규칙: Docs/SliceA/CombatDecisions.md 12장
public class GrowthUI : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap() => GameScene.WhenLoaded(() =>
    {
        if (FindAnyObjectByType<GrowthUI>()) return;
        new GameObject("GrowthUI").AddComponent<GrowthUI>();
    });

    GameFlow flow;
    GUIStyle text;
    GUIStyle button;

    void Start() => flow = FindAnyObjectByType<GameFlow>();

    void OnGUI()
    {
        if (!flow || flow.State != GameState.Growth) return;
        text ??= new GUIStyle(GUI.skin.label) { fontSize = 22, wordWrap = true };
        button ??= new GUIStyle(GUI.skin.button) { fontSize = 20 };
        var rewards = flow.Rewards;

        GUILayout.BeginArea(new Rect(Screen.width / 2f - 300f, Screen.height / 2f - 230f, 600f, 460f), GUI.skin.box);
        GUILayout.Label("영구 성장 — 희미한 기억", text);
        GUILayout.Label($"기억 조각 {flow.Persistent.memoryShards}   누적 투자 {flow.Persistent.invested}", text);
        GUILayout.Label("구매한 성장은 다음 순환부터 적용", text);
        GUILayout.Space(10f);

        foreach (var def in rewards.GrowthItems)
        {
            int lv = rewards.GrowthLevel(def.stat);
            int cost = rewards.GrowthCost(def.stat);
            string effect = string.Format(def.format, def.perLevel);
            string label = cost < 0
                ? $"{def.displayName} {effect}/Lv   Lv.{lv}/{def.costs.Length} (최대)"
                : $"{def.displayName} {effect}/Lv   Lv.{lv}/{def.costs.Length}   비용 {cost}";
            GUI.enabled = cost >= 0 && flow.Persistent.memoryShards >= cost;
            if (GUILayout.Button(label, button, GUILayout.Height(50f))) rewards.TryBuyGrowth(def.stat);
            GUI.enabled = true;
        }

        GUILayout.Space(16f);
        if (GUILayout.Button("다음 순환 시작 (18:00)", button, GUILayout.Height(56f))) flow.StartNextCycle();
        GUILayout.EndArea();
    }
}
