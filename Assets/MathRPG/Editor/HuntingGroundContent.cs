using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MathRPG.Editor
{
    public static class HuntingGroundContent
    {
        const string ResourcePath = "Assets/MathRPG/Resources/MathRPG/HuntingGrounds.asset";
        const string QuestionsFolder = "Assets/MathRPG/Content/Hunting/";
        static readonly string[] Introductions =
        {
            "숫자의 방향에서 시작해 식과 그래프까지 익히는 길이다.\n작은 흐름을 읽는 힘이 다음 훈련소로 가는 길을 연다.",
            "정령의 숲에서는 중3 계산이 장갑의 힘을 안정시킨다.\n제곱근에서 인수분해, 이차함수까지 차례로 익히자.",
            "인간 마을에서는 모양과 관계가 힘이 된다.\n삼각형에서 사각형, 원으로 범위를 넓히며 연결을 읽자.",
            "재앙으로 변한 길에는 안전한 마을이 없다.\n반복되는 힘과 회전, 변화의 규칙을 익히며 앞으로 나아가자."
        };

        [MenuItem("Math RPG/Create Hunting Ground Content")]
        public static void Ensure()
        {
            var existing = AssetDatabase.LoadAssetAtPath<HuntingGroundDefinition>(ResourcePath);
            Directory.CreateDirectory(Path.GetDirectoryName(ResourcePath));
            Directory.CreateDirectory(QuestionsFolder);
            AssetDatabase.Refresh();
            var definition = existing != null ? existing : ScriptableObject.CreateInstance<HuntingGroundDefinition>();
            definition.areas = BuildAreas();
            if (!definition.IsValid(out var error))
            {
                UnityEngine.Object.DestroyImmediate(definition);
                throw new InvalidOperationException(error);
            }
            if (existing == null) AssetDatabase.CreateAsset(definition, ResourcePath);
            else EditorUtility.SetDirty(definition);
            AssetDatabase.SaveAssets();
            Debug.Log("보스방과 분리된 사냥터 12개와 개념 안내를 생성했습니다.");
        }

        static HuntingAreaDefinition[] BuildAreas()
        {
            return new[]
            {
                Area(1, "여명 들판",
                    Lesson("01_Integer", "서로 반대인 힘", "양수와 음수는 반대 방향을 나타낸다. 서로 다른 부호를 더하면 큰 쪽의 방향이 남는다.",
                        "오른쪽 +5와 왼쪽 −3이 만나면 +2가 남는다.",
                        "+7과 −4의 힘이 만나 남는 값은?", new[] { "−11", "11", "3", "−3" }, 2,
                        "반대 방향의 힘 4만큼이 서로 상쇄돼."),
                    Lesson("02_Rational", "나눈 만큼의 크기", "분수는 전체를 같은 크기로 나눈 조각의 수다. 더할 때는 조각의 크기인 분모를 맞춘다.",
                        "1/2은 2/4와 같다. 그래서 1/2 + 1/4 = 3/4이다.",
                        "1/3 + 1/6을 계산할 때 1/3을 어떻게 바꾸면 좋을까?", new[] { "1/6", "2/6", "3/6", "1/9" }, 1,
                        "분모 3을 6으로 바꿀 때 분자도 같은 배로 바꿔."),
                    Lesson("03_Prime", "가장 작은 곱의 조각", "소수는 1보다 큰 자연수 중 약수가 1과 자기 자신뿐인 수다. 소인수분해는 수를 소수들의 곱으로 나타낸다.",
                        "12 = 2 × 2 × 3. 4는 2 × 2로 더 나눌 수 있다.",
                        "18을 소수들의 곱으로만 나타낸 것은?", new[] { "2 × 9", "3 × 6", "1 × 18", "2 × 3 × 3" }, 3,
                        "9와 6은 아직 소수가 아니야.")),
                Area(1, "바람길 구릉",
                    Lesson("04_Letter", "수를 대신하는 이름", "문자는 아직 정하지 않은 수를 대신한다. 3x는 3과 x를 곱한다는 뜻이다.",
                        "x = 2라면 3x + 1 = 3 × 2 + 1 = 7이다.",
                        "x = 4일 때 2x + 1의 값은?", new[] { "7", "9", "6", "24" }, 1,
                        "2x는 2 + x가 아니라 2 × x야."),
                    Lesson("05_Equation", "균형을 지키는 식", "등식의 양쪽에 같은 수를 더하거나 빼면 균형이 유지된다. 곱하거나 나눌 때도 양쪽에 같은 일을 한다. 0으로 나눌 수는 없다.",
                        "x + 3 = 7에서 양쪽에 3을 빼면 x = 4이다.",
                        "x + 5 = 9에서 x만 남기려면?", new[] { "왼쪽에만 5를 더한다", "양쪽에 5를 더한다", "양쪽에서 5를 뺀다", "오른쪽에서만 5를 뺀다" }, 2,
                        "양쪽의 균형을 유지하면서 +5를 없애봐.")),
                Area(1, "별빛 비탈",
                    Lesson("06_Exponent", "반복되는 곱", "지수는 같은 수를 몇 번 곱하는지 나타낸다. 같은 밑의 거듭제곱을 곱하면 지수를 더한다.",
                        "a² × a³ = a⁵. a 두 개와 세 개를 이어서 곱하기 때문이다.",
                        "b³ × b⁴를 하나의 거듭제곱으로 쓰면?", new[] { "b¹²", "2b⁷", "b⁷", "b" }, 2,
                        "지수를 곱하지 말고, b가 모두 몇 개인지 세어봐."),
                    Lesson("07_Coordinate", "두 수로 읽는 위치", "좌표 (x, y)는 가로 위치와 세로 위치를 순서대로 나타낸다. 오른쪽 이동은 x, 위쪽 이동은 y가 커진다.",
                        "(2, 1)에서 오른쪽으로 3 이동하면 (5, 1)이다.",
                        "(−1, 2)에서 위로 3 이동한 좌표는?", new[] { "(2, 2)", "(−1, 5)", "(−1, −1)", "(3, 2)" }, 1,
                        "가로 위치는 그대로 두고 세로 위치만 바꿔."),
                    Lesson("08_Linear", "일정하게 바뀌는 높이", "y = ax + b에서 a는 기울기, b는 y절편이다. x가 1 늘어날 때 y가 얼마나 바뀌는지 a가 알려준다.",
                        "y = 2x + 1은 x가 1 늘면 y가 2 늘고, y축을 (0, 1)에서 만난다.",
                        "y = −3x + 2에서 x가 1 늘면 y는?", new[] { "2 늘어난다", "3 늘어난다", "2 줄어든다", "3 줄어든다" }, 3,
                        "x 앞의 수가 변화하는 양을 알려줘.")),
                Area(2, "반디샘 바깥길",
                    Lesson("09_Root", "제곱 이전의 수", "어떤 수를 제곱해 a가 되면 그 수는 a의 제곱근이다. √a는 양의 제곱근이다.",
                        "9의 제곱근은 3과 −3이고 √9는 3이다.", "√64의 값은?", new[] { "6", "7", "8", "16" }, 2, "8×8=64야."),
                    Lesson("10_Radical", "뿌리의 계산", "제곱인 인수는 근호 밖으로 꺼낼 수 있고 같은 근호끼리 계수를 계산한다.",
                        "√12=2√3이고 2√3+√3=3√3이다.", "√27을 간단히 하면?", new[] { "3√3", "9√3", "3√9", "27" }, 0, "27=9×3으로 보자.")),
                Area(2, "솔그늘 오솔길",
                    Lesson("11_Factor", "곱으로 되돌리기", "인수분해는 다항식을 곱의 형태로 바꾸는 것이다.",
                        "x²+5x+6=(x+2)(x+3).", "x²−9를 인수분해하면?", new[] { "(x−3)(x+3)", "(x−9)(x+1)", "(x−3)²", "x(x−9)" }, 0, "두 제곱의 차를 이용해."),
                    Lesson("12_Quadratic", "두 근 찾기", "이차방정식은 인수분해하거나 근의 공식을 써서 푼다.",
                        "(x−2)(x−4)=0이면 x=2 또는 x=4다.", "(x+1)(x−5)=0의 해는?", new[] { "1,−5", "−1,5", "1,5", "−1,−5" }, 1, "각 인수를 0으로 놓아.")),
                Area(2, "안개뜰 비탈",
                    Lesson("13_Opening", "포물선의 방향", "y=a(x−p)²+q에서 a가 양수이면 위로, 음수이면 아래로 열린다.",
                        "y=−2x²은 아래로 열린다.", "y=3(x−1)²+2는 어느 방향으로 열릴까?", new[] { "위", "아래", "왼쪽", "오른쪽" }, 0, "제곱항의 계수가 양수야."),
                    Lesson("14_Vertex", "포물선의 꼭짓점", "y=a(x−p)²+q의 꼭짓점은 (p,q)다.",
                        "y=(x−2)²+1의 꼭짓점은 (2,1)이다.", "y=(x+3)²−2의 꼭짓점은?", new[] { "(3,−2)", "(−3,−2)", "(−2,3)", "(3,2)" }, 1, "x+3은 x−(−3)이야.")),
                Area(3, "잿빛 둔덕",
                    Lesson("16_Root", "제곱 이전의 수", "어떤 수를 제곱해 a가 되면 그 수는 a의 제곱근이다. 양수 a의 제곱근은 두 개이며 √a는 그중 양수인 값이다.",
                        "9의 제곱근은 3과 −3이지만, √9는 3이다.",
                        "16의 제곱근을 모두 고른 것은?", new[] { "4만", "−4만", "8과 −8", "4와 −4" }, 3,
                        "양수와 음수 모두 제곱하면 양수가 돼."),
                    Lesson("17_Factors", "곱으로 되돌리기", "인수분해는 다항식을 곱의 형태로 바꾸는 것이다. 공통인수를 찾거나 곱셈공식을 거꾸로 읽는다.",
                        "x² − 9 = (x − 3)(x + 3). 두 제곱의 차를 곱으로 바꾼 모습이다.",
                        "x² − 16을 인수분해하면?", new[] { "(x − 4)²", "(x − 4)(x + 4)", "(x − 8)(x + 2)", "x(x − 16)" }, 1,
                        "16은 4의 제곱이야."),
                    Lesson("18_Quadratic", "계수에서 두 근으로", "ax² + bx + c = 0에서 a ≠ 0이면 이차방정식이다. 근의공식을 사용할 때는 부호를 포함한 a, b, c를 먼저 읽는다.",
                        "x² − 5x + 6 = 0의 계수는 a=1, b=−5, c=6이다. 계산기는 이 계수로 근을 구한다.",
                        "2x² − 3x − 1 = 0의 계수 (a, b, c)는?", new[] { "(2, 3, 1)", "(2, −3, 1)", "(2, −3, −1)", "(0, −3, −1)" }, 2,
                        "각 항 앞의 부호까지 계수에 포함해.")),
                Area(3, "바람고개 사면",
                    Lesson("19_Opening", "포물선의 방향", "y = a(x − p)² + q에서 a가 양수이면 위로, 음수이면 아래로 열린 포물선이다.",
                        "y = −2x²는 아래로 열린다. 계수의 부호가 방향을 정한다.",
                        "y = 3(x − 1)² + 2의 그래프가 열리는 방향은?", new[] { "위", "아래", "왼쪽", "직선이다" }, 0,
                        "제곱 앞의 계수 3은 양수야."),
                    Lesson("20_Vertex", "방향이 바뀌는 자리", "y = a(x − p)² + q의 꼭짓점은 (p, q)다. 위로 열리면 꼭짓점에서 최솟값, 아래로 열리면 최댓값을 갖는다.",
                        "y = (x − 2)² + 1의 꼭짓점은 (2, 1), 최솟값은 1이다.",
                        "y = −(x − 3)² + 4의 최댓값은?", new[] { "−4", "3", "없다", "4" }, 3,
                        "제곱에 음수를 붙이면 0보다 커질 수 없어.")),
                Area(3, "돌빛 폐도",
                    Lesson("21_Polynomial", "같은 항끼리 모으기", "문자와 차수가 같은 항끼리 계수를 더하거나 뺀다. 괄호 앞의 음수는 괄호 안의 모든 항에 적용한다.",
                        "(3x + 2) − (x − 4) = 3x + 2 − x + 4 = 2x + 6.",
                        "(4x + 1) − (2x − 3)을 정리하면?", new[] { "2x − 2", "2x + 4", "6x − 2", "6x + 4" }, 1,
                        "괄호 앞의 −를 안쪽 두 항에 모두 적용해."),
                    Lesson("22_Line", "두 점 사이의 변화", "직선의 기울기는 y의 변화량을 x의 변화량으로 나눈 값이다. 서로 다른 두 점을 같은 순서로 빼야 한다.",
                        "(1, 2)에서 (3, 6)으로 가면 기울기는 (6−2)/(3−1) = 2이다.",
                        "(0, 1)과 (2, 5)를 지나는 직선의 기울기는?", new[] { "4", "1/2", "2", "3" }, 2,
                        "세로 변화량 4를 가로 변화량 2로 나눠.")),
                Area(4, "잔향 균열지",
                    Lesson("23_Exponential", "같은 비율로 커지는 힘", "지수함수는 지수에 변수가 들어 있는 함수다. y = 2ˣ에서는 x가 1 늘 때마다 y가 2배가 된다.",
                        "x가 0, 1, 2, 3이면 2ˣ는 1, 2, 4, 8이다.",
                        "y = 3ˣ에서 x가 1 늘어나면 y는 몇 배가 될까?", new[] { "1배", "2배", "3배", "9배" }, 2,
                        "3을 한 번 더 곱하게 돼."),
                    Lesson("24_Log", "몇 번 곱했는가", "로그는 거듭제곱의 지수를 되묻는 표현이다. 양수 a≠1에 대해 aᵇ=c이면 logₐc=b이다.",
                        "2³ = 8이므로 log₂8 = 3이다. 밑과 결과를 보고 지수를 찾는다.",
                        "log₂16의 값은?", new[] { "8", "4", "2", "16" }, 1,
                        "2를 네 번 곱하면 16이야.")),
                Area(4, "뒤틀린 회랑",
                    Lesson("25_Angle", "회전으로 읽는 각", "일반각은 회전 방향과 회전량을 나타낸다. 반시계 방향은 양수, 시계 방향은 음수이며 한 바퀴는 360°다.",
                        "450°는 한 바퀴를 돈 뒤 90° 더 회전한 각이다. 끝나는 방향은 90°와 같다.",
                        "390°와 끝나는 방향이 같은 각은?", new[] { "30°", "90°", "180°", "−30°" }, 0,
                        "한 바퀴 360°를 빼봐."),
                    Lesson("26_UnitCircle", "원 위의 두 좌표", "반지름 1인 원에서 양의 x축으로부터 각 θ만큼 회전한 점의 좌표는 (cos θ, sin θ)다.",
                        "90°의 점은 (0, 1)이므로 cos 90°=0, sin 90°=1이다.",
                        "단위원에서 180°에 해당하는 점은 (−1, 0)이다. cos 180°는?", new[] { "0", "1", "−1", "180" }, 2,
                        "cos는 원 위 점의 x좌표야.")),
                Area(4, "재앙의 비탈",
                    Lesson("27_Sequence", "일정하게 더하는 흐름", "수열은 수를 순서대로 나열한 것이다. 이웃한 항의 차가 일정하면 등차수열이다.",
                        "2, 5, 8, 11, …은 매번 3을 더하는 등차수열이다.",
                        "4, 9, 14, …의 다음 항은?", new[] { "18", "19", "20", "23" }, 1,
                        "바로 전 항에 같은 차이 5를 더해."),
                    Lesson("28_Geometric", "일정하게 곱하는 흐름", "이웃한 항 사이에 곱하는 비율이 일정하면 등비수열이다. 덧셈의 규칙과 곱셈의 규칙을 구별하자.",
                        "3, 6, 12, 24, …는 매번 2를 곱하는 등비수열이다.",
                        "2, 6, 18, …의 다음 항은?", new[] { "30", "36", "21", "54" }, 3,
                        "앞의 수에 매번 3을 곱하고 있어."),
                    Lesson("29_Change", "구간에서 얼마나 변했나", "평균변화율은 함수값의 변화량을 입력값의 변화량으로 나눈 것이다. 그래프의 두 점을 잇는 직선의 기울기와 같다.",
                        "f(x)=x²에서 x가 1에서 3으로 바뀌면 평균변화율은 (9−1)/(3−1)=4이다.",
                        "f(x)=x²에서 x가 2에서 4로 바뀔 때 평균변화율은?", new[] { "2", "12", "6", "8" }, 2,
                        "함수값의 차 16−4를 입력의 차 4−2로 나눠."))
            };
        }

        static HuntingAreaDefinition Area(int chapter, string name, params HuntingLessonDefinition[] lessons)
        {
            Color[] landscapes = { new Color(.23f, .29f, .22f), new Color(.12f, .25f, .23f), new Color(.24f, .22f, .23f), new Color(.18f, .14f, .24f) };
            Color[] creatures = { new Color(.6f, .72f, .36f), new Color(.4f, .75f, .65f), new Color(.66f, .44f, .36f), new Color(.63f, .39f, .75f) };
            return new HuntingAreaDefinition
            {
                locationName = name, chapterIntroduction = Introductions[chapter - 1], lessons = lessons,
                huntTarget = Mathf.Max(3, lessons.Length), enemyHealth = 40 + chapter * 12, enemyDamage = 3 + chapter * 2,
                wrongAnswerDamage = chapter + 1, killHeal = 4,
                attackWindup = 1.5f - chapter * .1f, questionInterval = 6, respawnDelay = 2,
                landscape = landscapes[chapter - 1], creature = creatures[chapter - 1]
            };
        }

        static HuntingLessonDefinition Lesson(string id, string title, string explanation, string example,
            string prompt, string[] choices, int correct, string hint)
        {
            string path = QuestionsFolder + id + ".asset";
            var question = AssetDatabase.LoadAssetAtPath<QuestionDefinition>(path);
            if (question == null)
            {
                question = ScriptableObject.CreateInstance<QuestionDefinition>();
                AssetDatabase.CreateAsset(question, path);
            }
            question.conceptId = "hunting-" + id;
            question.title = title;
            question.steps = new[] { new QuestionStep { phase = "흐름 읽기", prompt = prompt, choices = choices, correctIndex = correct, hint = hint } };
            if (!question.IsValid(out var reason)) throw new InvalidOperationException(reason);
            EditorUtility.SetDirty(question);
            return new HuntingLessonDefinition { title = title, explanation = explanation, example = example, question = question };
        }
    }
}
