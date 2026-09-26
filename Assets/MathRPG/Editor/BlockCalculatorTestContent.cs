using System.IO;
using UnityEditor;
using UnityEngine;

namespace MathRPG.Editor
{
    public static class BlockCalculatorTestContent
    {
        const string Folder = "Assets/MathRPG/Resources/MathRPG/BlockCalculatorTests";

        [MenuItem("Math RPG/Create Block Calculator Test Questions")]
        public static void Create()
        {
            Directory.CreateDirectory(Folder);
            AssetDatabase.Refresh();
            CreateQuestion("Arithmetic", "block-test-arithmetic", "세 묶음의 룬",
                Choice("조준", "한 묶음에 빨간 룬 4개와 파란 룬 2개, 합해서 6개가 있다.\n같은 묶음 3개의 전체 개수를 구하려면?",
                    new[] { "6과 3을 곱한다", "6에서 3을 뺀다", "6을 3으로 나눈다", "6과 3을 더한다" }, 0,
                    "한 묶음의 개수가 같은 묶음 수만큼 반복돼."),
                new QuestionStep
                {
                    type = QuestionStepType.BlockInput, phase = "차징",
                    prompt = "한 묶음은 4 + 2 = 6개, 모두 3묶음이다.\n블록과 연산 연결을 골라 전체 개수를 구하자.",
                    hint = "한 묶음의 개수와 묶음 수를 연결해봐. 4 + 2 식 블록도 쓸 수 있어.",
                    calculation = new BlockCalculationDefinition
                    {
                        function = CalculationFunction.Arithmetic, operandCount = 2,
                        blocks = new[]
                        {
                            Number("6", 6), Number("3", 3), Number("4", 4), Number("2", 2),
                            new CalculationBlock { label = "(4 + 2)", kind = CalculationBlockKind.Expression, constant = 6 },
                            Number("0", 0)
                        },
                        acceptedPaths = new[]
                        {
                            Product(0, 1), Product(1, 0), Product(4, 1), Product(1, 4)
                        },
                        expectedResults = new[] { 18d }
                    }
                },
                Choice("발동", "구한 룬을 두 사람에게 똑같이 나눠준다.\n한 사람이 받는 개수는?", new[] { "6개", "9개", "18개", "3개" }, 1,
                    "계산기 결과를 두 사람이 나눠 가져."));

            CreateQuestion("Quadratic", "block-test-quadratic", "두 갈래의 근",
                Choice("조준", "x² − 5x + 6 = 0의 근을 계수로부터 구하려 한다.\n어떤 기능을 사용할까?",
                    new[] { "삼각비", "평균", "근의공식", "기울기" }, 2, "이차방정식의 계수 a, b, c를 사용하는 기능이야."),
                new QuestionStep
                {
                    type = QuestionStepType.BlockInput, phase = "차징",
                    prompt = "x² − 5x + 6 = 0\n계수 a·b·c를 직접 배치한 뒤 [근의공식]을 누르자.",
                    hint = "ax² + bx + c = 0과 비교해봐. 계수에는 부호도 포함돼.",
                    calculation = new BlockCalculationDefinition
                    {
                        function = CalculationFunction.QuadraticFormula,
                        blocks = new[]
                        {
                            Number("1", 1), Number("−5", -5), Number("6", 6), Number("5", 5), Number("0", 0),
                            new CalculationBlock { label = "x²", kind = CalculationBlockKind.Expression, quadratic = 1 },
                            new CalculationBlock { label = "−5x", kind = CalculationBlockKind.Expression, linear = -5 }
                        },
                        acceptedPaths = new[] { new BlockAnswerPath { blocks = new[] { 0, 1, 2 } } },
                        expectedResults = new[] { 2d, 3d }
                    }
                },
                Choice("발동", "계산기로 구한 두 근 중 더 작은 값은?", new[] { "−3", "5", "6", "2" }, 3,
                    "결과에 나온 두 수의 크기를 비교해봐."));
            AssetDatabase.SaveAssets();
            Debug.Log("계산기 임시 문항 2개 생성 완료. 첫 훈련소에서 테스트 · 사칙연산 / 근의공식 버튼을 사용하세요.");
        }

        static CalculationBlock Number(string label, double value)
            => new CalculationBlock { label = label, kind = CalculationBlockKind.Number, number = value };

        static BlockAnswerPath Product(int left, int right)
            => new BlockAnswerPath { blocks = new[] { left, right }, links = new[] { ArithmeticLink.Multiply } };

        static QuestionStep Choice(string phase, string prompt, string[] choices, int correct, string hint)
            => new QuestionStep { phase = phase, prompt = prompt, choices = choices, correctIndex = correct, hint = hint };

        static void CreateQuestion(string file, string concept, string title, params QuestionStep[] steps)
        {
            string path = Folder + "/" + file + ".asset";
            var question = AssetDatabase.LoadAssetAtPath<QuestionDefinition>(path);
            bool create = question == null;
            if (create) question = ScriptableObject.CreateInstance<QuestionDefinition>();
            else Undo.RecordObject(question, "Update calculator test question");
            question.conceptId = concept;
            question.title = title;
            question.steps = steps;
            if (!question.IsValid(out var reason))
            {
                if (create) Object.DestroyImmediate(question);
                throw new System.InvalidOperationException(reason);
            }
            if (create) AssetDatabase.CreateAsset(question, path);
            else EditorUtility.SetDirty(question);
        }
    }
}
