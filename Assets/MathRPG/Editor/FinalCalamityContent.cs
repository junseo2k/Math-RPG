using System.IO;
using UnityEditor;
using UnityEngine;

namespace MathRPG.Editor
{
    public static class FinalCalamityContent
    {
        const string Path = "Assets/MathRPG/Resources/MathRPG/FinalCalamity.asset";
        public static void Ensure()
        {
            if (AssetDatabase.LoadAssetAtPath<FinalCalamityDefinition>(Path) != null) return;
            Directory.CreateDirectory("Assets/MathRPG/Resources/MathRPG");
            AssetDatabase.Refresh();
            var data = ScriptableObject.CreateInstance<FinalCalamityDefinition>();
            data.introduction = "길 끝에는 검은 갑옷을 두른 기사가 서 있었다.\n그 주위에서 먼지와 빗방울이 거꾸로 흘렀다.\n\n정령: 원래부터 정령이었던 존재야. 마력에 취해 저렇게 된 거지.\n\n주인공은 검을 쥐었다. 기사가 고개를 들었다.";
            data.farewell = "정령: …저걸 쓰러뜨리면 나도 사라지겠지.\n나를 여기로 보낸 힘도 저 재앙에 남아 있던 거니까.\n\n주인공: 알고 있었어?\n정령: 응. 그래도 너를 만난 건 후회하지 않아.\n네가 좋아했던 그 감각, 잊지 마.\n\n정령은 마지막으로 이 순간을 기억에 남겼다.";
            data.isolation = "기사가 검을 내리긋자 두 사람 사이의 공간이 갈라졌다.\n정령의 목소리가 끊겼다. 손을 뻗어도 닿지 않았다.\n\n주인공: …이제 내 손으로.\n\nQ 룬 대결 · 1~4 답 선택 · R 다른 풀이\n정답이어도 장판은 직접 피해야 한다.\n패배하면 정령이 남긴 격리 직전의 기록에서 다시 시작한다.";
            data.phaseTwo = "기사가 검을 돌렸다. 주인공의 손끝이 투명하게 흔들렸다.\n재앙: 너도 결국 같은 모습이 될 것이다.\n\n두 번째 페이즈 — 시간이 더 짧아지고 정령화가 시작된다.\n정령화 예고 후 잠시 공격·스킬·룬을 쓸 수 없다.\nA/D 이동, SPACE 점프, S 숙이기는 계속 사용할 수 있다.\n\n주인공은 한 발 물러서며 검의 궤적을 살폈다.";
            data.phaseThree = "룬이 맺히기도 전에 시간이 잘려 나갔다.\n식으로 장판을 줄이던 흐름은 더 이상 이어지지 않았다.\n\n주인공: 답을 적는 손만 내 힘인 건 아니야.\n\n마지막 페이즈 — Q 룬 대결을 사용할 수 없다.\n넓은 즉사 장판을 피한 뒤 J 검격과 K 잔광으로 반격하자.\n정령화 중에는 회피에 집중하자.";
            data.victory = "검이 검은 갑옷을 갈랐다. 멈춰 있던 빗방울이 땅에 닿았다.\n재앙의 형체가 흩어지며 뒤틀린 마력도 잦아들었다.\n\n주인공은 곁을 돌아보았다. 익숙한 빛은 보이지 않았다.\n\n주인공: …기억할게.\n\n손에 남은 감각만은 자신의 것이었다.";
            data.problems = new[]
            {
                Problem("두 제곱 사이", "101² − 99² = (101−99)(101+99). 각각 제곱하기 전에 두 수의 차와 합을 보자.",
                    Answer("101² − 99²의 값은? 두 제곱의 차로 생각해보자.", new[] { "200", "400", "800", "4" }, 1),
                    Answer("101² − 99²을 계산한다. 먼저 101²은?", new[] { "10201", "10001", "10200", "10101" }, 0),
                    Answer("101² = 10201. 이번에는 99²은?", new[] { "9901", "9801", "9604", "9999" }, 1),
                    Answer("10201 − 9801의 값은?", new[] { "200", "398", "400", "402" }, 2)),
                Problem("곱셈을 끝까지 하지 않고", "같은 밑의 거듭제곱을 나누면 지수를 뺀다. 2⁹ ÷ 2⁶ = 2³.",
                    Answer("2⁹ ÷ 2⁶의 값은?", new[] { "2", "4", "8", "64" }, 2),
                    Answer("2⁹ ÷ 2⁶을 계산한다. 먼저 2⁹은?", new[] { "256", "1024", "128", "512" }, 3),
                    Answer("분자는 512. 분모 2⁶은?", new[] { "32", "64", "128", "12" }, 1),
                    Answer("512 ÷ 64의 값은?", new[] { "8", "16", "4", "32" }, 0)),
                Problem("식을 하나로 접기", "x² − 6x + 9 = (x−3)². x=103을 넣기 전에 완전제곱식으로 묶어보자.",
                    Answer("x=103일 때 x² − 6x + 9의 값은?", new[] { "10609", "10000", "10009", "9991" }, 1),
                    Answer("x=103에서 x² − 6x + 9를 계산한다. 먼저 103²은?", new[] { "10609", "10600", "10309", "10909" }, 0),
                    Answer("103² = 10609. 빼야 할 6×103은?", new[] { "603", "636", "618", "609" }, 2),
                    Answer("10609 − 618 + 9의 값은?", new[] { "9991", "10009", "10100", "10000" }, 3))
            };
            AssetDatabase.CreateAsset(data, Path);
            AssetDatabase.SaveAssets();
        }
        static CalamityAnswer Answer(string prompt, string[] choices, int correct)
            => new CalamityAnswer { prompt = prompt, choices = choices, correctIndex = correct };
        static CalamityProblem Problem(string title, string insight, CalamityAnswer shortcut, params CalamityAnswer[] steps)
            => new CalamityProblem { title = title, insight = insight, shortcut = shortcut, directSteps = steps };
    }
}
