using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MathRPG.Editor
{
    public static class TrainingCourseContent
    {
        const string Folder = "Assets/MathRPG/Content/Course/";
        const string CoursePath = "Assets/MathRPG/Resources/MathRPG/TrainingCourse.asset";

        public static void Ensure(TrainingDefinition training)
        {
            if (training.course != null) { RivalDuelContent.Ensure(training); ForestVillageContent.Ensure(training); return; }
            Directory.CreateDirectory(Folder);
            Directory.CreateDirectory(Path.GetDirectoryName(CoursePath));
            AssetDatabase.Refresh();
            var course = AssetDatabase.LoadAssetAtPath<TrainingCourseDefinition>(CoursePath);
            if (course == null)
            {
                course = ScriptableObject.CreateInstance<TrainingCourseDefinition>();
                course.stages = MakeStages();
                AssetDatabase.CreateAsset(course, CoursePath);
            }
            training.course = course;
            EditorUtility.SetDirty(training);
            AssetDatabase.SaveAssets();
            RivalDuelContent.Ensure(training);
            ForestVillageContent.Ensure(training);
        }

        static QuestionStep Step(string phase, string prompt, string[] choices, int answer, string hint)
            => new QuestionStep { phase = phase, prompt = prompt, choices = choices, correctIndex = answer, hint = hint };

        static QuestionDefinition Question(string file, string concept, string title, params QuestionStep[] steps)
        {
            string path = Folder + file + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<QuestionDefinition>(path);
            if (existing != null) return existing;
            var question = ScriptableObject.CreateInstance<QuestionDefinition>();
            question.conceptId = concept;
            question.title = title;
            question.steps = steps;
            AssetDatabase.CreateAsset(question, path);
            return question;
        }

        static TrainingStageDefinition[] MakeStages()
        {
            var opposite = Question("01_Opposite", "integer-addition", "서로 다른 방향의 힘",
                Step("조준", "오른쪽으로 5, 왼쪽으로 3의 힘이 겹친다.\n남은 힘의 크기를 읽는 식은?", new[] { "5 + 3", "5 - 3", "3 - 5", "5 × 3" }, 1, "반대 방향의 힘은 상쇄돼. 큰 힘에서 작은 힘을 빼봐."),
                Step("차징", "상쇄되고 남은 힘은 어느 방향일까?", new[] { "왼쪽으로 2", "오른쪽으로 8", "오른쪽으로 2", "힘이 없다" }, 2, "오른쪽 힘 5 중 3이 상쇄되어 2가 남았어."));
            var prime = Question("02_Prime", "prime-factorization", "가장 작은 조각",
                Step("조준", "12의 힘을 소수 조각으로만 나누려 한다.\n어떤 묶음이 끝까지 나뉜 모습일까?", new[] { "3 × 4", "2 × 6", "2 × 2 × 3", "1 × 12" }, 2, "4와 6은 더 나눌 수 있어. 2와 3은 소수야."),
                Step("차징", "2 × 2 × 3에 쓰인 서로 다른 소수는?", new[] { "1과 12", "2와 3", "3과 4", "2와 6" }, 1, "같은 2가 두 번 있어도 소수의 종류는 하나로 세어."));
            var balance = Question("03_Balance", "equation-balance", "균형을 지키는 룬",
                Step("조준", "x + 3 = 7에서 x만 남기려면?", new[] { "양쪽에서 3을 뺀다", "왼쪽에만 3을 더한다", "양쪽에 3을 곱한다", "오른쪽에서만 3을 뺀다" }, 0, "균형을 유지하려면 양쪽에 같은 일을 해야 해."),
                Step("차징", "x = 7 - 3. x의 값은?", new[] { "10", "3", "-4", "4" }, 3, "7에서 3을 덜어내고, 그 값에 3을 더해 7이 되는지 확인해봐."));
            var coordinate = Question("04_Coordinate", "coordinates", "발자국의 위치",
                Step("조준", "점 (2, 3)의 첫 번째 수 2는 무엇을 뜻할까?", new[] { "세로 위치", "원점과의 거리", "가로 위치", "두 좌표의 합" }, 2, "좌표는 가로 x, 세로 y 순서로 적어."),
                Step("차징", "(2, 3)에서 오른쪽으로 1만큼 이동하면?", new[] { "(3, 3)", "(2, 4)", "(1, 3)", "(3, 2)" }, 0, "오른쪽 이동은 가로 좌표만 커져. 세로 위치는 그대로야."));
            var powers = Question("05_Powers", "exponent-product", "겹쳐지는 빛",
                Step("조준", "a² × a³처럼 같은 밑의 거듭제곱을 곱할 때는?", new[] { "지수를 더한다", "지수를 곱한다", "밑을 더한다", "지수를 뺀다" }, 0, "a가 두 번, 다시 세 번 곱해져 있어. 모두 몇 번인지 세어봐."),
                Step("차징", "a² × a³를 하나의 거듭제곱으로 묶으면?", new[] { "a⁶", "2a⁵", "a⁵", "a" }, 2, "같은 a가 2 + 3번 곱해진 셈이야."));
            var distribute = Question("06_Distribute", "distributive-law", "갈라지는 빛",
                Step("조준", "2(x + 3)의 괄호를 풀려 한다. 2는 어디에 곱할까?", new[] { "x에만", "3에만", "어디에도 곱하지 않는다", "x와 3 모두에" }, 3, "괄호 안의 두 항 모두 바깥 수의 영향을 받아."),
                Step("차징", "2(x + 3)을 펼친 식은?", new[] { "2x + 3", "2x + 6", "x + 6", "5x" }, 1, "2 × x와 2 × 3을 각각 계산해 연결해봐."));
            var chestA = Question("07_ChestLight", "exponent-product", "첫 번째 봉인",
                Step("봉인 해제", "a² × a³ = a⁵.\n이 룬의 연결은 올바를까?", new[] { "올바르다", "올바르지 않다" }, 0, "같은 밑의 곱은 지수를 더해. 2 + 3은 5야."));
            var chestB = Question("08_ChestBalance", "inequality-sign", "두 번째 봉인",
                Step("봉인 해제", "x < 3의 양쪽에 -2를 곱했다.\n균형의 방향을 올바르게 읽은 것은?", new[] { "-2x < -6", "-2x > -6", "-2x = -6", "2x > 6" }, 1, "부등식의 양쪽에 음수를 곱하면 부등호 방향이 반대로 바뀌어."));
            var slope = Question("09_Slope", "linear-slope", "흐름의 기울기",
                Step("조준", "y = 2x + 1에서 x가 1 커질 때, y는?", new[] { "1 커진다", "2 커진다", "2 작아진다", "변하지 않는다" }, 1, "x 앞의 수 2가 x의 변화에 따른 y의 변화를 알려줘."),
                Step("차징", "y = 2x + 1의 기울기는?", new[] { "1", "-1", "2", "0" }, 2, "x가 1 커질 때 y는 2 커졌어. 그 비율이 기울기야."));
            var intercept = Question("10_Intercept", "linear-intercept", "흐름의 출발점",
                Step("조준", "y = 2x + 1이 y축을 만나는 점을 찾으려면?", new[] { "y에 0을 넣는다", "x에 1을 넣는다", "y에 2를 넣는다", "x에 0을 넣는다" }, 3, "y축 위의 모든 점은 가로 좌표 x가 0이야."),
                Step("차징", "x = 0이면 y = 1. y축과 만나는 점은?", new[] { "(1, 0)", "(0, 1)", "(2, 1)", "(0, 2)" }, 1, "좌표는 (x, y) 순서야. x는 0, y는 1이지."));

            return new[]
            {
                new TrainingStageDefinition {
                    locationName = "여명의 연무장", instructorName = "여명의 교관", trialKind = InstructorTrialKind.Questions,
                    briefing = "교관: 힘의 방향과 작은 조각을 읽어라. 두 흐름을 보여주면 된다.",
                    rewardDialogue = "교관: 이제 첫 흐름을 읽을 줄 아는군. 이 책을 받아라.\n주인공: …방금은, 내 손으로 해냈다.",
                    bookName = "여명의 서", questions = new[] { opposite, prime }, questionCount = 2,
                    enemyHealth = 75, enemyDamage = 12, enemyWindup = 1.1f, accent = new Color(.46f, .9f, .91f) },
                new TrainingStageDefinition {
                    locationName = "바람길 초소", instructorName = "바람길의 교관", trialKind = InstructorTrialKind.Hunt,
                    briefing = "교관: 의뢰를 맡겼다. 오른쪽 마당의 훈련체 셋을 쓰러뜨려라.",
                    rewardDialogue = "교관: 흐름을 읽는 손이 제법 익숙해졌군. 다음 길로 가도 좋다.",
                    bookName = "바람길의 서", questions = new[] { balance, coordinate }, huntTarget = 3,
                    enemyHealth = 90, enemyDamage = 12, enemyWindup = 1.1f, accent = new Color(.65f, .86f, .5f) },
                new TrainingStageDefinition {
                    locationName = "별빛 보관소", instructorName = "별빛의 교관", trialKind = InstructorTrialKind.Treasure,
                    briefing = "교관: 마당의 봉인 상자 둘을 열어라. 잘못 골라도 다치지 않으니 다시 시도해라.",
                    rewardDialogue = "교관: 다른 모양으로 숨은 같은 흐름을 알아봤구나. 이 책도 가져가라.",
                    bookName = "별빛의 서", questions = new[] { powers, distribute }, chestQuestions = new[] { chestA, chestB },
                    accent = new Color(.8f, .65f, .98f) },
                new TrainingStageDefinition {
                    locationName = "푸른 첨탑", instructorName = "첨탑의 교관", trialKind = InstructorTrialKind.Sparring,
                    briefing = "교관: 마지막은 나와 겨룬다. 검을 들어라. 왼쪽으로 물러나면 언제든 멈출 수 있다.",
                    rewardDialogue = "교관: 네 곳의 수련을 모두 마쳤다. 이제 배운 것을 네 검으로 만들어라.",
                    bookName = "첨탑의 서", questions = new[] { slope, intercept },
                    enemyHealth = 180, enemyDamage = 14, enemyWindup = 1.05f, accent = new Color(.47f, .68f, 1f) }
            };
        }
    }
}
