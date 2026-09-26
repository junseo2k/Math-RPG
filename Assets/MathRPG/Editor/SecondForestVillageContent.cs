using System.IO;
using UnityEditor;
using UnityEngine;

namespace MathRPG.Editor
{
    public static class SecondForestVillageContent
    {
        const string Folder = "Assets/MathRPG/Content/SecondForest/";
        const string Path = "Assets/MathRPG/Resources/MathRPG/SecondForestVillage.asset";

        public static void Ensure(TrainingDefinition training)
        {
            Directory.CreateDirectory(Folder);
            var data = AssetDatabase.LoadAssetAtPath<ForestVillageDefinition>(Path);
            if (data == null) { data = ScriptableObject.CreateInstance<ForestVillageDefinition>(); AssetDatabase.CreateAsset(data, Path); }
            data.villageName = "솔그늘";
            data.guideName = "솔잎 정령";
            data.bossName = "조각을 삼킨 솔그늘 정령";
            data.relicName = "평행사변형 봉인 조각";
            data.encounterKind = ForestEncounterKind.DevouringSpirit;
            data.arrival = "솔잎 정령: 반디샘의 빛이 돌아온 걸 봤어. 하지만 우리 아이 하나가 봉인 조각의 마력에 이끌려 그걸 삼켜버렸어.\n\n주인공: 인간이 건드린 게 아니군요.\n솔잎 정령: 그래. 그래서 더 늦기 전에 되돌려야 해.";
            data.request = "솔잎 정령: 부풀어 오른 아이가 원기둥과 입체도형을 토해내고 있어. 떨어질 자리를 보고 움직여.\n\n장갑 마법이 열리면 차징이 시작돼. 틀리거나 끝까지 놓치면 맵 전체를 덮을 거야. 정답은 늦을수록 오히려 오래 기절시켜.";
            data.reward = "거대한 정령이 봉인 조각을 뱉고 손바닥만 한 원래 모습으로 돌아왔다.\n\n솔잎 정령: 인간 탓으로 돌리기 쉬웠는데 넌 먼저 사실을 봤구나. 평행사변형 조각을 가져가.";
            data.clearEvent = "작아진 솔그늘 정령이 수호 정령의 잠든 방향을 바라본다.";
            data.ambient = "동행 정령: 잘못 삼킨 힘과, 사람을 삼킨 감정은 같은 일이 아니야.";
            data.rewind = "동행 정령: 차징이 끝나기 전까지만 풀면 돼. 식을 하나씩 묶어보자.";
            data.bossHealth = 440;
            data.questions = new[]
            {
                Question("01_Factor", "quadratic-factor", "곱으로 되돌리기",
                    Step("조준", "x²+5x+6을 인수분해하면?", new[] { "(x+1)(x+6)", "(x+2)(x+3)", "(x-2)(x-3)", "(x+5)(x+1)" }, 1, "곱이 6이고 합이 5인 두 수는 2와 3이야."),
                    Step("발동", "(x-4)(x+1)=0의 해는?", new[] { "4, -1", "-4, 1", "4, 1", "-4, -1" }, 0, "각 인수를 0으로 놓아.")),
                Question("02_Formula", "quadratic-formula", "근을 가르는 식",
                    Step("조준", "x²-6x+5=0을 인수분해하면?", new[] { "(x-1)(x-5)", "(x+1)(x+5)", "(x-2)(x-3)", "(x+1)(x-5)" }, 0, "곱이 5이고 합이 -6인 수를 찾아."),
                    Step("발동", "방정식의 두 근은?", new[] { "-1, -5", "1, 5", "2, 3", "-1, 5" }, 1, "x-1=0 또는 x-5=0이야.")),
                Question("03_Discriminant", "quadratic-roots", "만나는 횟수",
                    Step("조준", "x²+2x+1=0은 (x+1)²=0이다. 서로 다른 실근은 몇 개?", new[] { "0개", "1개", "2개", "3개" }, 1, "같은 근 -1이 겹쳐 있어."),
                    Step("발동", "그 실근은?", new[] { "-2", "-1", "0", "1" }, 1, "x+1=0으로 두면 돼."))
            };
            EditorUtility.SetDirty(data);
            training.secondForest = data;
            EditorUtility.SetDirty(training);
            AssetDatabase.SaveAssets();
            ThirdForestVillageContent.Ensure(training);
        }

        static QuestionStep Step(string phase, string prompt, string[] choices, int answer, string hint)
            => new QuestionStep { phase = phase, prompt = prompt, choices = choices, correctIndex = answer, hint = hint };
        static QuestionDefinition Question(string file, string concept, string title, params QuestionStep[] steps)
        {
            string path = Folder + file + ".asset";
            var q = AssetDatabase.LoadAssetAtPath<QuestionDefinition>(path);
            if (q == null) { q = ScriptableObject.CreateInstance<QuestionDefinition>(); AssetDatabase.CreateAsset(q, path); }
            q.conceptId = concept; q.title = title; q.steps = steps; EditorUtility.SetDirty(q); return q;
        }
    }
}
