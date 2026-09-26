using System.IO;
using UnityEditor;
using UnityEngine;

namespace MathRPG.Editor
{
    public static class ForestClimaxContent
    {
        const string Path = "Assets/MathRPG/Resources/MathRPG/ForestClimax.asset";
        public static void Ensure(TrainingDefinition training)
        {
            var data = AssetDatabase.LoadAssetAtPath<ForestClimaxDefinition>(Path);
            if (data == null)
            {
                if (training.forest == null || training.secondForest == null || training.thirdForest == null ||
                    !training.forest.IsValid() || !training.secondForest.IsValid() || !training.thirdForest.IsValid())
                { Debug.LogWarning("Create valid forest village questions before the forest climax."); return; }
                Directory.CreateDirectory("Assets/MathRPG/Resources/MathRPG");
                AssetDatabase.Refresh();
                data = ScriptableObject.CreateInstance<ForestClimaxDefinition>();
                AssetDatabase.CreateAsset(data, Path);
            }
            data.awakening = "삼각형, 평행사변형, 원의 봉인 조각이 하나의 고리로 이어졌다.\n\n수호 정령: 숲의 목소리를 되찾아 주었구나. 방금 타락한 이들까지 깊은 곳의 무언가가 끌어모으고 있다.\n\n전투 중 단 한 번, 내 빛을 부를 수 있다.";
            data.introduction = "말랑한 정령 덩어리 곳곳에 녹다 만 도형 조각과 낡은 룬 장갑, 찢어진 완장과 현역 응시자의 완장이 뒤섞여 있다.\n\n동행 정령: 폭주로 생긴 정령들이 모인 거야. 하지만 저 안쪽에는 처음부터 다른 무언가가 있어.";
            data.victory = "숯빛 중심이 무너지자 붙잡혀 있던 빛이 숲으로 흩어졌다.\n\n안개 정령: 네가 찾는 짙은 색 슬라임은 숲 깊은 쪽에서 보았어.\n\n주인공: 이제 어디로 가야 할지 알겠어.\n동행 정령: 함께 가자. 숲은 우리를 기억할 거야.";
            data.questions = new[] { training.thirdForest.questions[0], training.thirdForest.questions[2] };
            EditorUtility.SetDirty(data);
            training.forestClimax = data;
            EditorUtility.SetDirty(training);
            AssetDatabase.SaveAssets();
            HumanVillageContent.Ensure(training);
        }
    }
}
