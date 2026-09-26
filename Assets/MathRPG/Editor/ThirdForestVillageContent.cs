using System.IO;
using UnityEditor;
using UnityEngine;

namespace MathRPG.Editor
{
    public static class ThirdForestVillageContent
    {
        const string Folder = "Assets/MathRPG/Content/ThirdForest/";
        const string Path = "Assets/MathRPG/Resources/MathRPG/ThirdForestVillage.asset";

        public static void Ensure(TrainingDefinition training)
        {
            Directory.CreateDirectory(Folder);
            var data = AssetDatabase.LoadAssetAtPath<ForestVillageDefinition>(Path);
            if (data == null) { data = ScriptableObject.CreateInstance<ForestVillageDefinition>(); AssetDatabase.CreateAsset(data, Path); }
            data.villageName = "안개뜰";
            data.guideName = "안개 정령";
            data.bossName = "안개뜰 봉인 상자";
            data.relicName = "원형 봉인 조각";
            data.encounterKind = ForestEncounterKind.Chest;
            data.arrival = "안개 정령: 마지막 조각은 우리가 직접 숨겼어. 장갑을 낀 인간들이 올 때마다 빼앗기지 않으려고.\n\n동행 정령: 이 아이가 앞의 두 조각을 돌려놓았어. 상자를 열 기회는 줘.";
            data.request = "안개 정령: 상자의 봉인은 이차함수의 모양으로 겹쳐져 있어. 틀리면 다시 잠길 뿐이야. 몇 번이든 다시 풀 수 있어.\n\n주인공: 서두르지 않고 열어볼게.";
            data.reward = "상자가 조용히 열리고 원형 봉인 조각이 떠올랐다.\n\n주인공은 부모에게 편지를 썼다. ‘할 만은 한데, 힘들긴 합니다.’\n\n그때 숲 가장자리에서 불길이 솟았다. 폭주한 장갑의 마력이 만든 불이었다. 흥분·분노·공포·좌절·탐욕에 삼켜진 응시자 다섯이 차례로 타락 정령이 되어 안개 속으로 사라졌다.\n\n동행 정령: …장갑은 사람을 안 가려.\n안개 정령: 봤지. 장갑 낀 인간은 결국 다 저렇게 돼.\n주인공은 자기 장갑을 한 번 내려다봤다.";
            data.clearEvent = "세 봉인 조각이 한꺼번에 빛난다. 방금 타락한 정령들이 향한 깊은 숲에서 거대한 울음이 들렸다.";
            data.ambient = "안개 정령: 수호님이 깨어 계실 때는 인간이 이렇게 깊이 들어오지 못했어.";
            data.rewind = "동행 정령: 상자는 도망가지 않아. 틀려도 다시 열면 돼.";
            data.questions = new[]
            {
                Question("01_Vertex", "quadratic-vertex", "포물선의 꼭짓점",
                    Step("조준", "y=(x-2)²+1의 꼭짓점은?", new[] { "(-2,1)", "(2,1)", "(1,2)", "(2,-1)" }, 1, "y=(x-p)²+q의 꼭짓점은 (p,q)야."),
                    Step("발동", "이 그래프는 어느 방향으로 열릴까?", new[] { "위", "아래", "왼쪽", "오른쪽" }, 0, "제곱항의 계수가 양수야.")),
                Question("02_Axis", "quadratic-axis", "안개의 대칭축",
                    Step("조준", "y=(x+3)²-2의 대칭축은?", new[] { "x=-3", "x=3", "y=-2", "y=2" }, 0, "꼭짓점의 x좌표를 지나."),
                    Step("발동", "이 함수의 최솟값은?", new[] { "-3", "-2", "2", "3" }, 1, "위로 열린 포물선의 꼭짓점 y값이 최솟값이야.")),
                Question("03_Translate", "quadratic-translation", "옮겨진 곡선",
                    Step("조준", "y=x²을 오른쪽으로 4만큼 옮기면?", new[] { "y=(x+4)²", "y=(x-4)²", "y=x²+4", "y=x²-4" }, 1, "오른쪽 이동은 괄호 안에서 x-4로 써."),
                    Step("발동", "여기에 위로 2만큼 더 옮기면?", new[] { "y=(x-4)²+2", "y=(x+4)²+2", "y=(x-2)²+4", "y=x²-2" }, 0, "세로 이동은 식 바깥에 더해."))
            };
            EditorUtility.SetDirty(data);
            training.thirdForest = data;
            EditorUtility.SetDirty(training);
            AssetDatabase.SaveAssets();
            ForestClimaxContent.Ensure(training);
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
