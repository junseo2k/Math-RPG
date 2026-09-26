using System.IO;
using UnityEditor;
using UnityEngine;

namespace MathRPG.Editor
{
    public static class RivalSecondRematchContent
    {
        const string Folder = "Assets/MathRPG/Content/RivalSecondRematch/";
        const string Path = "Assets/MathRPG/Resources/MathRPG/RivalSecondRematch.asset";
        public static void Ensure()
        {
            if (AssetDatabase.LoadAssetAtPath<RivalSecondRematchDefinition>(Path) != null) return;
            Directory.CreateDirectory(Folder);
            Directory.CreateDirectory("Assets/MathRPG/Resources/MathRPG");
            AssetDatabase.Refresh();
            var data = ScriptableObject.CreateInstance<RivalSecondRematchDefinition>();
            data.introduction = "주인공은 검을 거두고 재앙이 번지는 길로 발을 돌렸다.\n라이벌1: …그래. 네가 정한 길이면, 가 봐.\n\n부상자들을 돌려보낸 총사령관이 앞을 막았다.\n라이벌2: 넌 나한테 졌다. 나조차 막지 못한 재앙에\n너를 보낼 순 없어. 가고 싶다면 증명해.\n주인공: 이번에는 내가 원해서 가는 거야.";
            data.lesson = "정령: 변질된 흐름도 한 줄씩 보면 돼.\n\n지수: 2³은 2를 세 번 곱한 값, 8이야.\n같은 밑을 곱하면 지수를 더해. 2² × 2³ = 2⁵.\n로그: log₂8은 ‘2의 몇 제곱이 8인가’, 그래서 3이야.\n밑은 양수이며 1이 아니어야 하고, 로그 안의 수도 양수야.\nlog₂a + log₂b = log₂(ab)  (a, b > 0).\n\n문제에는 시간 제한이 없어. 장판은 점프 대신 표시 밖으로 피하자.";
            data.phaseDialogue = "라이벌2가 검을 다시 고쳐 쥐었다.\n라이벌2: …방금도 식은 맞았어. 왜 손이…\n\n이번에는 낮은 검격을 연이어 섞으며 거리를 좁혔다.\n라이벌2: 아직 끝난 게 아니야. 널 보내는 건 내 책임이니까.\n\n두 번째 페이즈 — 검격 순서와 간격이 바뀐다.";
            data.victory = "라이벌2의 검끝이 내려갔다.\n라이벌2: …됐어. 이제 내가 막을 명분은 없군.\n\n주인공: 난 내가 할 수 있는 걸 해볼 거야.\n라이벌2: 마음대로 해. 쓰러지지만 마.\n\n그는 끝내 시선을 마주치지 않고 옆으로 비켜섰다.";
            data.departure = "라이벌2는 남은 병사들에게 돌아갔다.\n주인공 앞에는 재앙을 향한 길이 남았다.\n\n정령: 갈 거지?\n주인공: 응.\n\n대답은 짧았다. 이번에는 발걸음이 멈추지 않았다.";
            data.questions = new[]
            {
                Question("01_Exponent", "rematch-exponent", "같은 밑의 흐름",
                    Step("조준", "2² × 2³을 하나의 거듭제곱으로 나타내면?", new[] { "2⁵", "2⁶", "4⁵", "4⁶" }, 0, "밑 2는 그대로 두고 지수를 더해. 2+3=5야."),
                    Step("발동", "2ˣ = 2² × 2³일 때 x는?", new[] { "6", "3", "5", "32" }, 2, "오른쪽은 2⁵이야. 밑이 같으므로 x=5야.")),
                Question("02_Log", "rematch-log", "몇 번 곱했을까",
                    Step("조준", "log₂8 = x를 지수식으로 바꾸면?", new[] { "8ˣ=2", "2ˣ=8", "x²=8", "2×x=8" }, 1, "로그는 지수를 묻는 표현이야. 2를 x번 곱해 8이 된다는 뜻이야."),
                    Step("발동", "2ˣ = 8일 때 x는?", new[] { "2", "4", "8", "3" }, 3, "2×2×2=8이므로 x=3이야.")),
                Question("03_Combine", "rematch-log-product", "합을 곱으로 잇기",
                    Step("조준", "log₂4 + log₂8을 하나의 로그로 나타내면?", new[] { "log₂12", "log₂2", "log₂32", "log₂64" }, 2, "같은 밑의 로그를 더하면 안의 양수를 곱해. 4×8=32야."),
                    Step("발동", "log₂32의 값은?", new[] { "5", "16", "4", "6" }, 0, "2⁵=32이므로 log₂32=5야."))
            };
            AssetDatabase.CreateAsset(data, Path);
            AssetDatabase.SaveAssets();
        }
        static QuestionStep Step(string phase, string prompt, string[] choices, int correct, string hint)
            => new QuestionStep { phase = phase, prompt = prompt, choices = choices, correctIndex = correct, hint = hint };
        static QuestionDefinition Question(string file, string concept, string title, params QuestionStep[] steps)
        {
            string path = Folder + file + ".asset";
            var data = AssetDatabase.LoadAssetAtPath<QuestionDefinition>(path);
            if (data != null) return data;
            data = ScriptableObject.CreateInstance<QuestionDefinition>();
            data.conceptId = concept; data.title = title; data.steps = steps;
            AssetDatabase.CreateAsset(data, path);
            return data;
        }
    }
}
