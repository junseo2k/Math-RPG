using System.IO;
using UnityEditor;
using UnityEngine;

namespace MathRPG.Editor
{
    public static class ForestVillageContent
    {
        const string Folder = "Assets/MathRPG/Content/Forest/";
        const string Path = "Assets/MathRPG/Resources/MathRPG/ForestVillage.asset";

        public static void Ensure(TrainingDefinition training)
        {
            Directory.CreateDirectory(Folder);
            Directory.CreateDirectory("Assets/MathRPG/Resources/MathRPG");
            var data = AssetDatabase.LoadAssetAtPath<ForestVillageDefinition>(Path);
            if (data == null) { data = ScriptableObject.CreateInstance<ForestVillageDefinition>(); AssetDatabase.CreateAsset(data, Path); }
            data.villageName = "반디샘";
            data.guideName = "샘빛 정령";
            data.bossName = "정령 수집가";
            data.relicName = "삼각 봉인 조각";
            data.encounterKind = ForestEncounterKind.Collector;
            data.arrival = "샘빛 정령: 또 룬 장갑을 낀 인간이 왔어. 붙잡힌 정령들을 보고도 같은 말을 할 수 있을까?\n\n동행 정령: 이 아이는 빼앗으러 온 게 아니야. 확인할 기회는 줘.";
            data.request = "샘빛 정령: 수집가가 레이저를 쓰는 정령들과 봉인 조각을 병에 가뒀어.\n\n레이저는 숙이거나 높이를 바꿔 피하고, 그물은 좌우로 몸을 움직여 끊어. 장갑이 병의 약점을 펼치면 제곱근의 계산을 끝내야 해.";
            data.reward = "병들이 깨지며 정령들이 한꺼번에 날아올랐다. 수집가는 삼각 봉인 조각을 떨어뜨린 채 소란 속으로 달아났다.\n\n샘빛 정령: 도망쳤지만 붙잡힌 아이들은 돌아왔어. 이 조각은 수호님을 깨우는 봉인의 일부야.";
            data.clearEvent = "수집가는 달아났다. 반디샘의 정령들은 주인공의 장갑을 전보다 덜 경계한다.";
            data.ambient = "동행 정령: 같은 장갑인데 쓰는 사람에 따라 빛이 참 다르네.";
            data.rewind = "동행 정령: 병은 세 개야. 전조가 보이면 식부터 천천히 읽자.";
            data.questions = new[]
            {
                Question("01_RootMeaning", "square-root", "제곱을 되돌리는 수",
                    Step("조준", "x²=49이고 x는 양수다. x는?", new[] { "5", "6", "7", "8" }, 2, "7×7=49야."),
                    Step("발동", "√81의 값은?", new[] { "8", "9", "18", "81" }, 1, "9²=81이야.")),
                Question("02_Radical", "radical-simplify", "뿌리 밖으로",
                    Step("조준", "√12를 √(4×3)으로 보았다. 가장 간단한 꼴은?", new[] { "2√3", "3√2", "4√3", "6" }, 0, "√4=2를 밖으로 꺼내."),
                    Step("발동", "2√3+5√3은?", new[] { "7√6", "10√3", "7√3", "3√3" }, 2, "같은 √3끼리 계수만 더해.")),
                Question("03_RealNumber", "real-number-order", "수직선의 자리",
                    Step("조준", "√5는 어느 두 정수 사이에 있을까?", new[] { "0과 1", "1과 2", "2와 3", "3과 4" }, 2, "2²<5<3²이야."),
                    Step("발동", "다음 중 가장 큰 수는?", new[] { "2", "√5", "-√5", "0" }, 1, "√5는 2보다 조금 커."))
            };
            EditorUtility.SetDirty(data);
            training.forest = data;
            EditorUtility.SetDirty(training);
            AssetDatabase.SaveAssets();
            SecondForestVillageContent.Ensure(training);
        }

        static QuestionStep Step(string phase, string prompt, string[] choices, int answer, string hint)
            => new QuestionStep { phase = phase, prompt = prompt, choices = choices, correctIndex = answer, hint = hint };

        static QuestionDefinition Question(string file, string concept, string title, params QuestionStep[] steps)
        {
            string path = Folder + file + ".asset";
            var question = AssetDatabase.LoadAssetAtPath<QuestionDefinition>(path);
            if (question == null) { question = ScriptableObject.CreateInstance<QuestionDefinition>(); AssetDatabase.CreateAsset(question, path); }
            question.conceptId = concept; question.title = title; question.steps = steps; EditorUtility.SetDirty(question); return question;
        }
    }
}
