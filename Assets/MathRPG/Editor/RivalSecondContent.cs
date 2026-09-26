using System.IO;
using UnityEditor;
using UnityEngine;

namespace MathRPG.Editor
{
    public static class RivalSecondContent
    {
        const string Folder = "Assets/MathRPG/Content/RivalSecond/";
        const string Path = "Assets/MathRPG/Resources/MathRPG/RivalSecond.asset";
        public static void Ensure()
        {
            ChapterFourContent.Ensure();
            if (AssetDatabase.LoadAssetAtPath<RivalSecondDefinition>(Path) != null) return;
            Directory.CreateDirectory(Folder);
            Directory.CreateDirectory("Assets/MathRPG/Resources/MathRPG");
            AssetDatabase.Refresh();
            var data = ScriptableObject.CreateInstance<RivalSecondDefinition>();
            data.introduction = "뒤늦게 도착한 친구가 주인공의 손에서 인장을 가져갔다.\n라이벌2: 공적은 더 강한 사람이 갖는 거야. 왕국의 규칙이잖아.\n\n주민들은 아무 말도 하지 못했다.\n주인공: …돌려달라.\n라이벌2: 그럼 증명해. 여기서.";
            data.breakdown = "답은 알고 있다. 그런데 손이 따라오지 않는다.\n\n아무도 건드리지 않았는데 식이 흔들려 보였다.\n정령: 내 목소리 들려? 숨을…\n\n주인공은 룬 장갑에 힘을 주었다.\n부모님에게 돌아가야 한다. 인장이 있어야 한다.";
            data.defeat = "장갑에는 빛이 맺히지 않았다. 검이 바닥에 떨어졌다.\n\n라이벌2: 그럼 그 자리는 내가 받겠군.\n\n그는 인장을 챙겨 떠났다.\n주인공은 뜯지 못한 답장이 든 품을 움켜쥐었다.";
            data.departure = "주인공은 사람들 사이를 밀치고 뛰쳐나갔다.\n정령: 기다려…!\n\n그날 이후, 마을에서 주인공을 본 사람은 없었다.\n\n답장은 여전히 뜯지 않은 채였다.\n챕터3 — 끝";
            data.questions = new[]
            {
                Question("01_Balance", "rival-second-balance", "붙잡은 균형",
                    Step("조준", "2x + 3 = 9에서 양변에 3을 빼면?", new[] { "2x=6", "2x=12", "x=6", "2x=9" }, 0, "양변에서 같은 수를 빼면 균형이 유지돼. 9-3=6이야."),
                    Step("차징", "2x = 6의 해는?", new[] { "x=2", "x=6", "x=3", "x=12" }, 2, "양변을 2로 나누면 x=3이야.")),
                Question("02_Root", "rival-second-roots", "좁아지는 흐름",
                    Step("조준", "√16의 값은?", new[] { "±4", "4", "-4", "8" }, 1, "√16은 음이 아닌 제곱근 하나를 가리켜. 4야."),
                    Step("차징", "x² = 16의 모든 실수 해는?", new[] { "4만", "-4만", "8과 -8", "4와 -4" }, 3, "4²도 16, (-4)²도 16이야.")),
                Question("03_Vertex", "rival-second-vertex", "알고 있는 답",
                    Step("조준", "y = (x - 1)² + 2의 꼭짓점은?", new[] { "(-1, 2)", "(2, 1)", "(1, 2)", "(1, -2)" }, 2, "x=1일 때 제곱 부분이 0이고 y=2야."),
                    Step("차징", "같은 함수에서 y의 최솟값은?", new[] { "2", "1", "0", "없다" }, 0, "제곱은 0 이상이므로 (x-1)²+2는 2 이상이야."))
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
            question.conceptId = concept; question.title = title; question.steps = steps;
            AssetDatabase.CreateAsset(question, path);
            return question;
        }
    }
}
