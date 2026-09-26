using System.IO;
using UnityEditor;
using UnityEngine;

namespace MathRPG.Editor
{
    public static class EndingContent
    {
        const string Path = "Assets/MathRPG/Resources/MathRPG/Ending.asset";
        public static void Ensure()
        {
            if (AssetDatabase.LoadAssetAtPath<EndingDefinition>(Path) != null) return;
            Directory.CreateDirectory("Assets/MathRPG/Resources/MathRPG");
            AssetDatabase.Refresh();
            var data = ScriptableObject.CreateInstance<EndingDefinition>();
            data.pages = new[]
            {
                Page("마침내 얻은 자리",
                    "재앙이 사라진 뒤, 주인공은 왕궁으로 불려 갔다.\n왕은 공적을 인정하고 주인공에게 총사령관의 벼슬을 내렸다.\n\n오랫동안 바라던 말이었다. 주인공은 임명장을 두 손으로 받았다.\n박수 소리가 울렸지만, 마음 한쪽에는 조용한 빈자리가 남았다.", "가족에게 돌아간다", new Color(.2f, .18f, .25f)),
                Page("돌아온 사람",
                    "집 앞에는 가족들이 서 있었다. 어머니가 먼저 달려와 주인공을 안았다.\n어머니: …돌아왔구나.\n아버지는 임명장을 보다가, 다시 주인공의 얼굴을 바라보았다.\n아버지: 고생했다. 안으로 들어오자.\n\n주인공: 저, 벼슬을 받았어요. 그래도 앞으로는 제가 좋아하는 것도 하면서 살고 싶어요.\n잠시 침묵이 흘렀다. 가족들은 함께 집 안으로 들어갔다.", "그 뒤의 이야기", new Color(.25f, .2f, .16f)),
                Page("왕궁에 남은 불안",
                    "왕: 다음 재난이 오면, 그때도 막아낼 수 있겠는가.\n신하: 사람들은 함께 싸우는 법을 배웠습니다. 왕국은 전보다 강해졌습니다.\n\n얼마 뒤, 왕명으로 작성된 문서들이 세상에 공개되었다.\n인장 분실 공표부터 인간 마을의 재난까지, 왕이 꾸민 일이었다.\n소식은 마을을 건너 퍼졌다. 사람들은 그동안의 일을 다시 이야기하기 시작했다.", "익숙한 일상으로", new Color(.13f, .16f, .22f)),
                Page("라이벌1의 아침",
                    "라이벌1은 일상으로 돌아갔다.\n아침에는 검을 손질하고, 밀린 일을 끝내고, 익숙한 길을 걸었다.\n\n문득 주인공이 제 발로 일어서던 모습이 떠올랐다.\n라이벌1: …이제는 알아서 가겠지.\n그는 작게 웃고 다시 하던 일을 이었다.", "훈련장으로", new Color(.19f, .26f, .22f)),
                Page("라이벌2의 훈련장",
                    "라이벌2는 병사들과 함께 훈련장에 서 있었다.\n라이벌2: 급하게 하지 마. 식을 아는 것과 손으로 해내는 건 다르니까.\n\n한 병사의 룬이 끊기자, 그는 옆으로 다가가 같은 자세를 잡았다.\n라이벌2: 다시. 나도 같이 한다.\n이번에는 두 사람이 나란히 처음부터 시작했다.", "남아 있는 빈자리", new Color(.22f, .23f, .25f)),
                Page("무심코 돌아본 곳",
                    "집으로 돌아오는 길에, 주인공은 무심코 옆을 돌아보았다.\n언제나 한마디 보태던 빛은 없었다.\n\n주인공은 한동안 그 자리를 보다가 천천히 고개를 돌렸다.\n주인공: …잊지 않았어.\n저녁 바람이 지나갔다.", "주인공의 오늘", new Color(.16f, .2f, .28f)),
                Page("누가 보지 않아도",
                    "어느 날, 일을 마친 주인공은 조용한 책상 앞에 앉았다.\n벽에는 임명장이 걸려 있었고, 문밖에서는 가족들의 목소리가 들렸다.\n\n종이에 적힌 식을 지웠다가, 다른 방법으로 다시 이어 보았다.\n마침내 한 줄이 깔끔하게 맞아떨어졌다.\n주인공은 혼자 작게 웃었다. 오늘도, 그게 좋았다.", "이야기를 마친다", new Color(.2f, .23f, .22f)),
                Page("끝",
                    "가장 찬란했던 순간은, 아무도 보지 않는 곳에도 있었다.\n\n주인공의 이야기는 여기서 막을 내립니다.", "엔딩 다시보기", new Color(.075f, .12f, .15f))
            };
            AssetDatabase.CreateAsset(data, Path);
            AssetDatabase.SaveAssets();
        }
        static EndingPage Page(string title, string body, string action, Color backdrop)
            => new EndingPage { title = title, body = body, action = action, backdrop = backdrop };
    }
}
