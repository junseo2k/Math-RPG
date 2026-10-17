using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

// 시험판 클리어 결과 화면: 도달 시각, 며칠째, 플레이 시간 + 영구 성장으로 / 종료. 규칙: Docs/VerticalSliceSpec.md F7
public class ResultUI : MonoBehaviour
{
    GameFlow flow;
    RectTransform content;
    TextMeshProUGUI title;
    TextMeshProUGUI detail;
    GameObject firstButton;
    RunState trackedRun;
    float runStartTime;
    bool shown;

    void Start()
    {
        flow = FindAnyObjectByType<GameFlow>();
        content = GameUI.MakeRect("Content", transform);
        GameUI.Fill(content);

        var dim = GameUI.MakeImage(content, "Dim", new Color(0f, 0f, 0f, 0.7f));
        GameUI.Fill(dim.rectTransform);

        title = GameUI.MakeText(content, "", 72f, GameUI.Gold);
        GameUI.Place(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 220f), new Vector2(1200f, 100f));
        var sub = GameUI.MakeText(content, "시험판 클리어", 40f, Color.white);
        GameUI.Place(sub.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 130f), new Vector2(1200f, 60f));
        detail = GameUI.MakeText(content, "", 36f, new Color(0.85f, 0.85f, 0.9f));
        GameUI.Place(detail.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(1200f, 120f));

        // 시험판 A: 클리어 → 영구 성장 → 다음 순환 (CombatDecisions 12장)
        firstButton = GameUI.MakeButton(content, "영구 성장으로", new Vector2(-200f, -160f), new Vector2(340f, 90f), ToGrowth).gameObject;
        GameUI.MakeButton(content, "종료", new Vector2(200f, -160f), new Vector2(340f, 90f), Quit);

        content.gameObject.SetActive(false);
    }

    void Update()
    {
        if (!flow) return;
        // 플레이 시간 = 판 시작(RunState 교체) ~ 클리어
        if (trackedRun != flow.Run)
        {
            trackedRun = flow.Run;
            runStartTime = Time.time;
        }

        bool cleared = flow.State == GameState.Cleared;
        if (cleared && !shown) Show();
        if (!cleared && shown)
        {
            shown = false;
            content.gameObject.SetActive(false);
        }
    }

    void Show()
    {
        shown = true;
        int seconds = Mathf.FloorToInt(Time.time - runStartTime);
        title.text = $"{flow.TimeText} 도달";
        detail.text = $"{flow.Persistent.day}일째\n플레이 시간 {seconds / 60:00}:{seconds % 60:00}";
        content.gameObject.SetActive(true);
        if (EventSystem.current) EventSystem.current.SetSelectedGameObject(firstButton);
    }

    void ToGrowth() => flow.OpenGrowth();

    void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
