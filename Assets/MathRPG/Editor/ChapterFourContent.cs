using System.IO;
using UnityEditor;
using UnityEngine;

namespace MathRPG.Editor
{
    public static class ChapterFourContent
    {
        const string Path = "Assets/MathRPG/Resources/MathRPG/ChapterFour.asset";

        public static void Ensure()
        {
            EndingContent.Ensure();
            FinalCalamityContent.Ensure();
            RivalSecondRematchContent.Ensure();
            var data = AssetDatabase.LoadAssetAtPath<ChapterFourDefinition>(Path);
            bool isNew = data == null;
            if (isNew)
            {
                Directory.CreateDirectory("Assets/MathRPG/Resources/MathRPG");
                AssetDatabase.Refresh();
                data = ScriptableObject.CreateInstance<ChapterFourDefinition>();
            }
            var defaults = new[]
            {
                Page("변질된 마력",
                    "하늘에 낯선 균열이 번졌다. 마력이 흐르는 방향이 뒤틀렸다.\n익숙한 식으로는 흐름조차 읽을 수 없는 괴물들이 길을 덮었다.\n\n사람들이 피신한 곳에도 재앙은 따라왔다. 더는 안전한 마을이 없었다.", "토벌대의 소식을 듣는다"),
                Page("새 총사령관",
                    "왕의 인장을 받은 친구는 총사령관이 되었다.\n라이벌2: 내가 앞에 선다. 대열을 유지해.\n\n그는 병사들을 이끌고 재앙을 향해 나아갔다.", "토벌의 행방"),
                Page("처음으로 흔들린 손",
                    "병사들의 시선이 한 사람의 손끝에 모였다.\n한 번의 실수가 모두를 죽일 수 있다는 생각에, 익숙한 식이 끊겼다.\n라이벌2: …물러서! 부상자부터 데리고 나가!\n\n토벌대는 돌아왔다. 재앙은 멈추지 않았다.", "한편, 길에서 멀어진 곳"),
                Page("아무도 없는 곳",
                    "주인공은 무너진 담벼락 아래에 홀로 앉아 있었다.\n검을 잡을 이유도, 돌아갈 곳도 떠오르지 않았다.\n\n품속에서 구겨진 봉투가 손끝에 닿았다. 아직 뜯지 못한 답장이었다.", "답장을 뜯는다"),
                Page("부모님의 답장",
                    "“이 벼슬을 얻으면 너도 우리도 다 편해질 거다.\n우릴 위해서라도 버텨다오.”\n\n주인공은 같은 문장을 오래 바라보았다.\n종이를 쥔 두 손이 천천히 벌어졌다.", "답장을 찢는다"),
                Page("찢어진 답장", "", "다가오는 발소리"),
                Page("찾아온 가족",
                    "찢어진 종이가 발치에 흩어졌다. 누군가 걸음을 멈췄다.\n라이벌1: …여기 있었구나. 얼마나 찾아다녔는지 알아?\n\n주인공은 고개를 들지 않았다.\n라이벌1은 주인공 앞에 멈춰 섰다.", "라이벌을 바라본다"),
                Page("들리지 않는 도발",
                    "라이벌1: 일어나. 날 쓰러뜨려봐.\n그러지 않으면 포기하지도 못해.\n\n주인공의 시선은 바닥에 머물렀다.\n검을 들어야 한다는 생각조차 끝까지 이어지지 않았다.", "검에 손을 뻗는다"),
                Page("일어나지 못한 사람",
                    "손이 올라가지 않았다. 다가오는 검에도 몸이 움직이지 않았다.\n라이벌1은 급히 검을 거뒀지만, 주인공은 그대로 무너졌다.\n\n싸움은 시작조차 되지 않았다.\n발치에 흩어진 답장 조각이 바람에 밀렸다.", "고개를 숙인 채"),
                Page("다시, 그 말",
                    "라이벌1: …이렇게 있을 거야?\n\n아무 대답도 돌아오지 않았다. 라이벌1이 가까이 몸을 낮췄다.\n\n라이벌1: …부모님이 걱정하실 거야.", "고개를 든다"),
                Page("처음으로 쏟아낸 말",
                    "주인공: 그딴 거 알 바 아니야!\n아무도 날 봐준 적 없잖아.\n다들 내가 뭘 해내는지만 신경 썼지, 나를 본 적은 없었어!\n\n라이벌1은 말을 잃었다. 주인공의 목소리만 또렷하게 남았다.", "쏟아지는 마력"),
                Page("무너지는 경계",
                    "룬 장갑의 틈으로 마력이 새어 나왔다. 손끝의 윤곽이 흐려졌다.\n분노가 커질수록 몸은 낯선 무언가로 바뀌려 했다.\n라이벌1: 손이…! 잠깐, 내 말 좀 들어!\n\n목소리가 멀어지고, 발밑의 감각이 사라졌다.", "안쪽의 목소리"),
                Page("또 다른 나",
                    "정령: 나는 다른 가능세계에서 온, 또 다른 너야.\n네 미래가 아니라, 다른 세계를 살았던 나.\n\n눈을 뜨자 정령이 마주 서 있었다.\n그 너머로 자기 자신과 닮은 윤곽이 겹쳐 보였다.", "정령의 이야기를 듣는다"),
                Page("돌아갈 수 없던 시간",
                    "정령: 나도 그 자리에 오르지 못했어. 뒤늦게 깨달았을 땐\n스물셋을 넘겨 다시 도전할 수도 없었지.\n재난의 잔재가 날 정령으로, 남은 시간의 힘이 이 세계로 보냈어.\n\n처음부터 알고 있었어. 네가 무너지는 모습도, 낯설지 않았어.", "말없이 듣는다"),
                Page("너를 위해서",
                    "정령: …너, 계속 부모님 뜻대로만 살아왔잖아.\n근데 넌 너잖아. 부모님을 위해서가 아니라, 너를 위해서 사는 거야.\n총사령관이 못 되면 뭐 어때. 네가 잘하는 걸 하면서 살면 되잖아.\n\n내가 놓친 건 그 자리가 아니었어. 내가 뭘 좋아하는지도 몰랐던 거야.", "정령이 조용히 묻는다"),
                Page("가장 찬란했던 순간",
                    "정령: …네 인생에서 가장 찬란했던 때는 언제야?\n\n\n아무 말도 떠오르지 않았다.\n침묵 속에서, 오래 잊고 있던 감각 하나가 남았다.", "그 감각을 따라간다"),
                Page("아무도 보지 않던 날",
                    "훈련소 한쪽에서, 같은 식을 몇 번이나 지우고 다시 썼다.\n한 줄이 다음 줄로 이어졌고, 마침내 답이 맞아떨어졌다.\n주인공: …됐다.\n\n칭찬할 사람도 없었다. 그런데 웃음이 났다. 그저 내가 풀어냈으니까.", "그 순간을 붙잡는다"),
                Page("내 손으로",
                    "주인공은 그때의 감각을 놓지 않고 천천히 숨을 골랐다.\n흩어지던 손끝이 돌아왔다. 마력이 장갑 안으로 잦아들었다.\n\n눈앞에 무너진 길과 라이벌1이 다시 보였다.\n주인공은 온전한 자기 손을 펴 보았다.", "스스로 일어선다"),
                Page("내가 좋아하는 것",
                    "주인공: 난… 풀렸을 때가 좋았어. 내 손으로 해냈을 때.\n싸우고, 생각하고, 다시 해보는 게.\n\n주인공은 자기 의지로 검을 집어 들었다.\n주인공: 다시 가볼게. 이번엔 내가 하고 싶어서야.", "재앙을 향해 출발한다")
            };
            bool changed = isNew;
            if (data.pages == null || data.pages.Length < defaults.Length)
            {
                var expanded = new ChapterFourPage[defaults.Length];
                if (data.pages != null) System.Array.Copy(data.pages, expanded, data.pages.Length);
                data.pages = expanded;
                changed = true;
            }
            for (int i = 0; i < defaults.Length; i++)
            {
                if (data.pages[i] != null) continue;
                data.pages[i] = defaults[i];
                changed = true;
            }
            var reunion = data.pages[(int)ChapterFourBeat.Reunion - 1];
            if (reunion.action == "이 순간을 기억한다")
            {
                reunion.action = defaults[(int)ChapterFourBeat.Reunion - 1].action;
                changed = true;
            }
            var awakening = data.pages[(int)ChapterFourBeat.Awakening - 1];
            const string oldLastLine = "주인공: 한 번 더. 이번엔 내가 하고 싶어서야.";
            if (awakening.body != null && awakening.body.Contains(oldLastLine))
            {
                awakening.body = awakening.body.Replace(oldLastLine, "주인공: 다시 가볼게. 이번엔 내가 하고 싶어서야.");
                changed = true;
            }
            if (awakening.action == "이 순간을 기억한다")
            {
                awakening.action = "재앙을 향해 출발한다";
                changed = true;
            }
            if (isNew) AssetDatabase.CreateAsset(data, Path);
            else if (changed) EditorUtility.SetDirty(data);
            if (changed) AssetDatabase.SaveAssets();
        }

        static ChapterFourPage Page(string title, string body, string action)
            => new ChapterFourPage { title = title, body = body, action = action };
    }
}
