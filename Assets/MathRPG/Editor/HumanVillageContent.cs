using System.IO;
using UnityEditor;
using UnityEngine;

namespace MathRPG.Editor
{
    public static class HumanVillageContent
    {
        const string Folder = "Assets/MathRPG/Content/HumanVillage/";
        const string Path = "Assets/MathRPG/Resources/MathRPG/HumanVillage.asset";

        public static void Ensure(TrainingDefinition training)
        {
            Directory.CreateDirectory(Folder);
            Directory.CreateDirectory("Assets/MathRPG/Resources/MathRPG");
            AssetDatabase.Refresh();
            var data = training.humanVillage != null ? training.humanVillage : AssetDatabase.LoadAssetAtPath<HumanVillageDefinition>(Path);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<HumanVillageDefinition>();
                data.arrival = "숲의 짙은 흔적은 강을 따라 인간들의 나루로 이어졌다.\n\n나루지기: 길을 찾는 거라면 기다려. 어젯밤에도 배가 부서졌어.\n주인공: 무슨 일이 있었나요?\n\n나루지기: 저 탁류 속 놈 때문이지. 짐을 옮길 사람도 이제 얼마 안 남았어.";
                data.request = "나루지기: 오른쪽 물길을 막은 포식자를 쫓아줘. 배를 띄우지 못하면 남은 식량도 건너오지 못해.\n\n주인공: 쓰러뜨리면 마을도 괜찮아지겠죠?\n나루지기: …오늘은 버티겠지.\n\n정령: 우선 눈앞의 길부터 열어보자.";
                data.aftermath = "나루지기: 물길이 열렸군. 하지만 윗마을에도 무언가 나타났대. 또 짐을 옮겨야겠어.\n\n주인공: …겨우 하나 끝냈는데.\n\n정령: 네가 한 일이 사라진 건 아니야. 오늘 건너갈 사람들은 네 덕분에 갈 수 있어.";
                data.ambient = "정령: 다들 서두르고 있네. 너도 계속 앞만 보고 있어. 잠깐 숨을 골라도 괜찮아.";
                data.clearedAmbient = "정령: 아까보다 어깨가 내려갔어. 이겼는데도… 많이 지친 거야?";
                data.rewind = "정령: 아직 내가 붙잡고 있어.\n나루로 돌아가자. 서두르지 않아도 돼.";
                data.questions = new[]
                {
                    Question("01_Roots", "square-roots", "제곱의 안쪽",
                        Step("조준", "제곱해서 9가 되는 수는 3과 -3이다.\n√9는 그중 음이 아닌 값을 뜻한다. √9는?", new[] { "-3", "3", "9", "±3" }, 1, "9의 제곱근은 ±3이지만, 기호 √9는 음이 아닌 제곱근 3 하나를 나타내."),
                        Step("차징", "이제 x² = 9를 만족하는 x를 모두 고르면?", new[] { "3만", "-3만", "3과 -3", "9와 -9" }, 2, "3²도 9이고 (-3)²도 9야. √9의 값과 방정식 x²=9의 모든 해를 구분해봐.")),
                    Question("02_Factors", "quadratic-factorization", "곱으로 되돌리는 흐름",
                        Step("조준", "(x + 2)(x + 3)을 펼치면 어떤 식일까?", new[] { "x² + 5x + 6", "x² + 6x + 5", "x² + 5", "2x + 5" }, 0, "각 항을 곱해 더해봐. x² + 3x + 2x + 6 = x² + 5x + 6이야."),
                        Step("차징", "반대로 x² + 5x + 6을 인수분해하면?", new[] { "(x + 1)(x + 6)", "(x - 2)(x - 3)", "(x + 2)(x - 3)", "(x + 2)(x + 3)" }, 3, "곱해서 6, 더해서 5인 두 수 2와 3을 찾아. 앞에서 펼친 계산을 거꾸로 읽으면 돼.")),
                    Question("03_Quadratic", "quadratic-formula", "균형의 두 갈래",
                        Step("조준", "x² - 5x + 6 = 0을 ax² + bx + c = 0과 비교하면\na, b, c는 각각?", new[] { "1, 5, 6", "1, -5, 6", "0, -5, 6", "1, -5, -6" }, 1, "x² 앞의 1은 생략되어 있어. x의 계수에는 부호까지 포함하므로 b=-5야."),
                        Step("차징", "근의 공식 x = (-b ± √(b² - 4ac)) / (2a)에 넣으면\nx = (5 ± √1) / 2다. 두 해는?", new[] { "-2와 -3", "1과 5", "2와 3", "5와 6" }, 2, "√1=1이야. (5+1)/2=3, (5-1)/2=2로 두 경우를 읽어봐."))
                };
                AssetDatabase.CreateAsset(data, Path);
            }
            training.humanVillage = data;
            EnsureSecondVillage(data);
            HumanClimaxContent.Ensure();
            RivalSecondContent.Ensure();
            EditorUtility.SetDirty(training);
            AssetDatabase.SaveAssets();
        }

        static void EnsureSecondVillage(HumanVillageDefinition first)
        {
            if (first.nextVillage != null) { EnsureThirdVillage(first.nextVillage); return; }
            const string secondPath = "Assets/MathRPG/Resources/MathRPG/HumanVillageSecond.asset";
            var data = AssetDatabase.LoadAssetAtPath<HumanVillageDefinition>(secondPath);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<HumanVillageDefinition>();
                data.villageName = "바람고개";
                data.residentName = "고개지기";
                data.bossName = "능선의 파수꾼";
                data.bossHealth = 500;
                data.attackDamage = 13;
                data.wrongDamage = 8;
                data.runeDamage = 56;
                data.correctHeal = 6;
                data.arrival = "강을 벗어나자 지붕을 묶은 밧줄들이 바람에 울었다.\n\n고개지기: 나루에서 왔나? 물길이 열렸다더니, 이번엔 고갯길이 막혔어.\n주인공: 여기도요?\n\n정령: 사람들은 하늘만 보고 있네. 바람이 불 때마다 짐을 끌어안고 있어.";
                data.request = "고개지기: 능선의 파수꾼이 보급 수레를 돌려보내고 있어. 오른쪽 고갯길을 열어줘.\n\n주인공: 길만 열면 되는 거죠.\n고개지기: 그래. …안색이 안 좋은데, 잠깐 쉬고 가도 돼.\n\n정령: 올라갔다 내려오는 흐름에도 꼭짓점이 있어. 하나씩 살펴보자.";
                data.aftermath = "고개지기: 수레가 넘어왔어. 고맙… 그런데 다음 마을에서도 구호품을 보내달라는군.\n\n주인공: 또 다음인가요.\n고개지기: 예언이 맞는 거라면, 길 하나 열린들 괜찮아질까.\n\n정령: 지금은 네가 연 길로 사람들이 돌아오고 있어. 그건 분명해.";
                data.ambient = "정령: 여기선 바람 소리에도 사람들이 움찔해. 너까지 서두를 필요는 없어.";
                data.clearedAmbient = "정령: 길은 조용해졌는데 사람들 얼굴은 그대로네. 네가 못해서 그런 건 아니야.";
                data.rewind = "정령: 아직 늦지 않았어.\n바람고개로 돌아가서 흐름을 다시 보자.";
                data.questions = new[]
                {
                    Question("04_Parabola", "quadratic-opening", "갈라지는 바람",
                        Step("조준", "y = x²에서 x가 -2일 때 y는?", new[] { "-4", "-2", "2", "4" }, 3, "(-2)²는 (-2)×(-2)=4야. 음수를 제곱하면 양수가 돼."),
                        Step("차징", "y = ax²에서 a가 음수이면 포물선은 어느 방향으로 열릴까?", new[] { "위쪽 ∪", "아래쪽 ∩", "오른쪽", "직선이 된다" }, 1, "y=-x²에 x=-1, 0, 1을 넣으면 -1, 0, -1이야. 가운데가 높고 양쪽으로 내려가.")),
                    Question("05_Vertex", "quadratic-vertex", "고개의 중심",
                        Step("조준", "y = (x - 2)² + 1에서 제곱 부분이 0이 되는 x는?", new[] { "-2", "-1", "2", "1" }, 2, "x-2=0을 풀면 x=2야. 이때 제곱 부분이 가장 작은 0이 돼."),
                        Step("차징", "y = (x - 2)² + 1의 꼭짓점과 대칭축은?", new[] { "(2, 1), x=2", "(-2, 1), x=-2", "(2, 1), y=1", "(1, 2), x=1" }, 0, "x=2일 때 y=1이므로 꼭짓점은 (2, 1)이야. 대칭축은 꼭짓점을 지나는 세로선 x=2야.")),
                    Question("06_Maximum", "quadratic-maximum", "능선 너머의 흐름",
                        Step("조준", "y = -(x - 1)² + 4에서 x가 모든 실수일 때 y의 최댓값은?", new[] { "1", "-4", "최댓값이 없다", "4" }, 3, "제곱은 0 이상이므로 그 앞에 -를 붙이면 0 이하야. 4를 더한 값은 4를 넘지 못해."),
                        Step("차징", "같은 함수에서 x=0일 때 y=3이다.\n대칭축 x=1의 반대편에서 y=3이 되는 x는?", new[] { "-1", "1", "2", "3" }, 2, "0은 대칭축 1에서 왼쪽으로 1만큼 떨어져 있어. 오른쪽으로 1만큼 떨어진 x=2에서도 y=3이야."))
                };
                AssetDatabase.CreateAsset(data, secondPath);
            }
            first.nextVillage = data;
            EditorUtility.SetDirty(first);
            EnsureThirdVillage(data);
        }

        static void EnsureThirdVillage(HumanVillageDefinition second)
        {
            if (second.nextVillage != null) return;
            const string thirdPath = "Assets/MathRPG/Resources/MathRPG/HumanVillageThird.asset";
            var data = AssetDatabase.LoadAssetAtPath<HumanVillageDefinition>(thirdPath);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<HumanVillageDefinition>();
                data.villageName = "돌빛 장터";
                data.residentName = "창고지기";
                data.bossName = "잔해의 문지기";
                data.bossHealth = 500;
                data.attackDamage = 13;
                data.wrongDamage = 8;
                data.runeDamage = 64;
                data.correctHeal = 6;
                data.arrival = "장터의 좌판은 비어 있고, 사람들은 창고 앞에 줄을 서 있었다.\n\n창고지기: 고개를 열어준 사람이군. 수레는 왔는데 창고 길이 무너졌어.\n주인공: 어디로 가면 되나요?\n\n정령: 아직 짐도 내려놓지 않았잖아. 먼저 이야기를 들어보자.";
                data.request = "창고지기: 오른쪽 창고 길의 잔해의 문지기를 쫓아줘. 식량을 꺼내야 해.\n\n주인공: 알겠습니다.\n창고지기: 예언의 재난이 온다는데, 오늘 먹을 것부터 걱정해야 하니…\n\n정령: 식의 조건부터 보자. 한 번에 전부 풀 필요는 없어.";
                data.aftermath = "창고지기: 문이 열렸어. 이제 식량을 나누고, 지붕도 고쳐야겠군.\n\n주인공: 다음은 지붕인가요.\n창고지기: 잠깐, 안색이 안 좋아. 물부터 마셔.\n\n정령: 저 사람들이 식량을 받고 있어. 지금 한 일부터 보고 가자.";
                data.ambient = "정령: 좌판마다 불이 꺼져 있네. 사람들 목소리도 작아졌어.";
                data.clearedAmbient = "정령: 창고 앞의 줄이 움직이기 시작했어. 아직 걱정은 많아도, 오늘 먹을 것은 생겼네.";
                data.rewind = "정령: 흐름이 끊기기 전으로 돌아가자.\n돌빛 장터에서 다시 준비하면 돼.";
                data.questions = new[]
                {
                    Question("07_PolynomialBalance", "polynomial-equations-inequalities", "잔해 사이의 균형",
                        Step("조준", "(x² + 2x + 1) - (x² - x + 3)을 정리하면?", new[] { "x - 2", "3x + 4", "3x - 2", "2x² + x - 2" }, 2, "뒤 괄호의 모든 항에 -를 붙여. x²+2x+1-x²+x-3에서 같은 차수끼리 더하면 3x-2야."),
                        Step("차징", "정리한 식으로 3x - 2 = 7을 풀면?", new[] { "x=3", "x=5", "x=7/3", "x=-3" }, 0, "양변에 2를 더하면 3x=9야. 양변을 3으로 나누면 x=3이야."),
                        Step("발사", "부등식 -2x + 4 > 0의 해는?", new[] { "x>2", "x<2", "x<−2", "x≥2" }, 1, "-2x>-4에서 음수 -2로 나누면 부등호 방향이 바뀌어. 따라서 x<2야.")),
                    Question("08_RationalGraph", "rational-function-graph", "끊어진 길의 조건",
                        Step("조준", "유리함수 y = 2/(x - 1)에서 사용할 수 없는 x는?", new[] { "0", "2", "-1", "1" }, 3, "분모가 0이면 나눗셈을 할 수 없어. x-1=0이 되는 x=1을 제외해야 해."),
                        Step("차징", "y = 2/(x - 1)의 그래프 위에 있는 점은?", new[] { "(1, 2)", "(2, 2)", "(2, 1)", "(0, 2)" }, 1, "x=2를 넣으면 y=2/(2-1)=2야. 따라서 (2, 2)가 그래프 위에 있어."),
                        Step("발사", "y = 2/(x - 1)의 점근선 두 개는?", new[] { "x=0, y=1", "x=1, y=2", "x=1, y=0", "x=−1, y=0" }, 2, "분모가 0이 되는 세로선 x=1과, x의 절댓값이 커질수록 y가 가까워지는 가로선 y=0이야.")),
                    Question("09_RootAndLine", "radical-function-straight-line", "다시 잇는 길",
                        Step("조준", "실수 범위에서 y = √(x - 2)의 정의역은?", new[] { "x≥2", "x>2", "x≤2", "모든 실수" }, 0, "제곱근 안은 0 이상이어야 해. x-2≥0이므로 x≥2야. √0도 정의돼."),
                        Step("차징", "y = √(x - 2)의 그래프에서 x=6일 때의 점은?", new[] { "(6, 4)", "(6, −2)", "(6, 8)", "(6, 2)" }, 3, "√(6-2)=√4=2야. √는 음이 아닌 값을 나타내므로 (6, 2)야."),
                        Step("발사", "두 점 (2, 0), (6, 2)를 지나는 직선의 방정식은?", new[] { "y=2x−4", "y=x/2−1", "y=x/2+1", "y=−x/2+1" }, 1, "기울기는 (2-0)/(6-2)=1/2야. y=x/2+b에 (2,0)을 넣으면 b=-1이야."))
                };
                AssetDatabase.CreateAsset(data, thirdPath);
            }
            second.nextVillage = data;
            EditorUtility.SetDirty(second);
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
