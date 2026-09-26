using System.IO;
using UnityEditor;
using UnityEngine;

namespace MathRPG.Editor
{
    public static class RivalDuelContent
    {
        const string Folder = "Assets/MathRPG/Content/Rival/";
        const string Path = "Assets/MathRPG/Resources/MathRPG/RivalDuel.asset";

        public static void Ensure(TrainingDefinition training)
        {
            if (training.rival != null) return;
            Directory.CreateDirectory(Folder);
            Directory.CreateDirectory("Assets/MathRPG/Resources/MathRPG");
            AssetDatabase.Refresh();
            var data = AssetDatabase.LoadAssetAtPath<RivalDuelDefinition>(Path);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<RivalDuelDefinition>();
                data.introduction = "라이벌: 이제야 여기까지 왔네. 난 벌써 다음 길을 보고 있어.\n\n주인공: …비켜줄 생각은 없어 보이는데.\n\n라이벌: 같은 집에서 배웠어도, 검까지 같지는 않겠지. 보여줘.";
                data.patternChangeLine = "라이벌: …제법이네. 그럼, 이 흐름도 따라와 봐.";
                data.victory = "라이벌: …내가 먼저 배운 것만 믿고 있었나.\n네 검, 전보다 훨씬 단단해졌네.\n\n주인공: 이번에는 내가 먼저 갈게.\n정령: 좋아. 다음은 정령의 숲이야.";
                data.questions = new[]
                {
                    Question("01_BalancedBlade", "distribution-equation", "두 겹의 균형",
                        Step("조준", "2(x + 1) = 8. 괄호 안의 흐름을 먼저 드러내려면?", new[] { "양쪽에 2를 더한다", "양쪽을 2로 나눈다", "왼쪽에서만 1을 뺀다", "오른쪽에만 2를 곱한다" }, 1, "괄호 전체에 곱해진 2를 풀려면, 양쪽을 같은 수 2로 나눠봐."),
                        Step("차징", "양쪽을 2로 나누면 x + 1 = 4.\n검기에 담을 x는?", new[] { "5", "4", "3", "2" }, 2, "양쪽에서 1을 빼면 돼. 3 + 1이 4인지 확인해도 좋아.")),
                    Question("02_LightLayers", "exponent-substitution", "겹쳐지는 검기",
                        Step("조준", "a² × a³의 빛을 하나로 모으면?", new[] { "a⁶", "a⁵", "2a⁵", "a" }, 1, "같은 a가 두 번, 세 번 곱해졌어. 모두 다섯 번이야."),
                        Step("차징", "a = 2일 때 a⁵는 어떤 계산일까?", new[] { "2 × 5", "2 + 5", "5 × 5", "2 × 2 × 2 × 2 × 2" }, 3, "지수 5는 밑 2를 다섯 번 곱하라는 뜻이야.")),
                    Question("03_SteadyFlow", "linear-function", "흔들리지 않는 흐름",
                        Step("조준", "y = 2x + 1에서 x가 1 커지면 y는?", new[] { "1 커진다", "2 작아진다", "2 커진다", "변하지 않는다" }, 2, "x 앞의 수가 2야. x가 하나 커질 때 y는 두 개 커져."),
                        Step("차징", "같은 식에서 x = 1이면, y는?", new[] { "3", "2", "1", "4" }, 0, "2 × 1에 1을 더한 값을 읽어봐."))
                };
                AssetDatabase.CreateAsset(data, Path);
            }
            training.rival = data;
            EditorUtility.SetDirty(training);
            AssetDatabase.SaveAssets();
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
