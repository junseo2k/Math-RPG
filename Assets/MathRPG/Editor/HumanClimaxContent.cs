using System.IO;
using UnityEditor;
using UnityEngine;

namespace MathRPG.Editor
{
    public static class HumanClimaxContent
    {
        const string Folder = "Assets/MathRPG/Content/HumanClimax/";
        const string Path = "Assets/MathRPG/Resources/MathRPG/HumanClimax.asset";

        public static void Ensure()
        {
            if (AssetDatabase.LoadAssetAtPath<HumanClimaxDefinition>(Path) != null) return;
            Directory.CreateDirectory(Folder);
            Directory.CreateDirectory("Assets/MathRPG/Resources/MathRPG");
            AssetDatabase.Refresh();
            var data = ScriptableObject.CreateInstance<HumanClimaxDefinition>();
            data.letter = "창고지기: 너에게 온 답장이야. 먼 길을 돌아 도착했어.\n\n봉투에는 익숙한 부모님의 글씨가 적혀 있었다.\n주인공은 봉투 끝에 손을 댔다.\n\n정령: 이제 잠깐 읽어도…";
            data.disaster = "땅이 울렸다. 숲 쪽에서 짙은 덩어리가 밀려왔다.\n주민: 예언의 재난이야! 모두 안으로 들어가!\n\n주인공은 뜯지 못한 답장을 품에 넣었다.\n주인공: 저 색… 왕이 말한 것과 같아.\n정령: 앞의 흐름부터 보자. 내가 곁에 있을게.";
            data.victory = "검은 덩어리가 흩어진 자리에 왕의 인장이 남았다.\n\n주민: 인장이야! 재난을 막고 인장까지 찾았어!\n창고지기: 올해 총사령관은 네가 되겠구나.\n\n주인공: 이제… 돌아갈 수 있겠지.\n품속의 답장은 여전히 뜯지 않은 채였다.";
            data.questions = new[]
            {
                Question("01_Roots", "climax-quadratic-roots", "갈라진 핵",
                    Step("조준", "x² - 5x + 6을 인수분해하면?", new[] { "(x-2)(x-3)", "(x+2)(x+3)", "(x-1)(x-6)", "(x-2)(x+3)" }, 0, "곱해서 6, 더해서 -5인 수는 -2와 -3이야."),
                    Step("차징", "x² - 5x + 6 = 0의 두 해는?", new[] { "-2, -3", "1, 6", "2, 3", "-2, 3" }, 2, "(x-2)(x-3)=0이므로 둘 중 한 인수가 0이야. x=2 또는 x=3이야.")),
                Question("02_Vertex", "climax-quadratic-vertex", "범람의 중심",
                    Step("조준", "y = -(x - 2)² + 4의 꼭짓점은?", new[] { "(-2, 4)", "(2, 4)", "(4, 2)", "(2, -4)" }, 1, "제곱 부분이 0인 x=2를 넣으면 y=4야."),
                    Step("차징", "같은 함수에서 x가 모든 실수일 때 y의 최댓값은?", new[] { "2", "0", "없다", "4" }, 3, "-(x-2)²는 0 이하이므로 y는 4를 넘지 않아. x=2일 때 4가 돼.")),
                Question("03_Conditions", "climax-function-conditions", "남겨진 길",
                    Step("조준", "실수 범위에서 √(x - 1)과 1/(x - 2)를\n둘 다 계산할 수 있는 x의 조건은?", new[] { "x>2", "x≥1, x≠2", "x≥1", "x≤1, x≠2" }, 1, "제곱근은 x-1≥0, 분모는 x-2≠0이어야 해. 두 조건을 함께 지켜야 해."),
                    Step("차징", "직선 y=2x-1에서 y≥5가 되는 x의 범위는?", new[] { "x≤3", "x≥2", "x>3", "x≥3" }, 3, "2x-1≥5에서 2x≥6이므로 x≥3이야. 양수로 나누므로 부등호는 그대로야."))
            };
            AssetDatabase.CreateAsset(data, Path);
        }

        static QuestionStep Step(string phase, string prompt, string[] choices, int answer, string hint)
            => new QuestionStep { phase = phase, prompt = prompt, choices = choices, correctIndex = answer, hint = hint };

        static QuestionDefinition Question(string file, string concept, string title, params QuestionStep[] steps)
        {
            string path = Folder + file + ".asset";
            var question = AssetDatabase.LoadAssetAtPath<QuestionDefinition>(path);
            if (question != null) return question;
            question = ScriptableObject.CreateInstance<QuestionDefinition>();
            question.conceptId = concept;
            question.title = title;
            question.steps = steps;
            AssetDatabase.CreateAsset(question, path);
            return question;
        }
    }
}
