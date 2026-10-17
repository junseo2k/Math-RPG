using UnityEngine;

// 시간이 정지된 상점 (최소 기능, 임시 IMGUI). 규칙: Docs/CombatDecisions.md 11장
public class ShopUI : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (FindAnyObjectByType<ShopUI>()) return;
        new GameObject("ShopUI").AddComponent<ShopUI>();
    }

    GameFlow flow;
    PotionUser potion;
    string message = "";
    GUIStyle text;
    GUIStyle button;

    void Start() => flow = FindAnyObjectByType<GameFlow>();

    void OnGUI()
    {
        if (!flow || flow.State != GameState.Shop) { message = ""; return; }
        if (!potion) potion = FindAnyObjectByType<PotionUser>();
        text ??= new GUIStyle(GUI.skin.label) { fontSize = 22, wordWrap = true };
        button ??= new GUIStyle(GUI.skin.button) { fontSize = 20, wordWrap = true };
        var rewards = flow.Rewards;
        var run = flow.Run;

        GUILayout.BeginArea(new Rect(Screen.width / 2f - 300f, Screen.height / 2f - 260f, 600f, 520f), GUI.skin.box);
        GUILayout.Label("시간이 멈춘 상점", text);
        GUILayout.Label($"보유 {run.gold}G   포션 {(potion ? potion.Count : 0)}/{(potion ? potion.MaxCount : 0)}", text);
        GUILayout.Space(10f);

        if (GUILayout.Button($"체력 회복 포션  {rewards.PotionPrice}G", button, GUILayout.Height(50f)))
            message = rewards.TryBuyPotion(potion) ? "포션 구매" : "구매 불가 (G 부족 또는 보유 한도)";

        foreach (var id in run.shopStock.ToArray())
        {
            var def = rewards.Relic(id);
            if (def == null) continue;
            string grade = def.rare ? "희귀" : "일반";
            if (GUILayout.Button($"[{grade}] {def.displayName}  {rewards.RelicPrice(def)}G\n{def.description}", button, GUILayout.Height(70f)))
                message = rewards.TryBuyRelic(id) ? $"{def.displayName} 구매" : "G 부족";
        }
        if (run.shopStock.Count == 0) GUILayout.Label("판매할 유물 없음", text);

        GUILayout.Space(10f);
        GUILayout.Label(message, text);
        if (GUILayout.Button("나가기", button, GUILayout.Height(50f))) flow.ExitShop();
        GUILayout.EndArea();
    }
}
