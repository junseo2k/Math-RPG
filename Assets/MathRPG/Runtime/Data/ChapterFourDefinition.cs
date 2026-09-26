using System;
using UnityEngine;

namespace MathRPG
{
    [Serializable]
    public sealed class ChapterFourPage
    {
        public string title;
        [TextArea(3, 8)] public string body;
        public string action;
    }

    [CreateAssetMenu(menuName = "Math RPG/Chapter Four")]
    public sealed class ChapterFourDefinition : ScriptableObject
    {
        public ChapterFourPage[] pages;
        public Color sky = new Color(.10f, .09f, .16f);
        public Color ground = new Color(.16f, .18f, .21f);
        public Color corruptedMagic = new Color(.66f, .37f, .77f);
        [Range(0, 1)] public float paperVolume = .35f;
        [Min(.1f)] public float paperDuration = .65f;
        public AudioClip paperTear;
        public Color innerWorldSky = new Color(.07f, .17f, .23f);
        public Color awakenedMagic = new Color(.55f, .88f, .87f);
        public Color memorySky = new Color(.24f, .20f, .13f);
        [Range(0, 12)] public float overloadShake = 3f;
        [Range(.1f, 3)] public float overloadPulseSpeed = .7f;
        [Range(.2f, 5)] public float overloadDuration = 1.5f;
        [Range(.2f, 5)] public float reflectionDuration = 1.5f;
        [Range(.2f, 5)] public float calmDuration = 1.2f;

        public bool IsValid()
        {
            if (pages == null || pages.Length != (int)ChapterFourBeat.Awakening) return false;
            foreach (var page in pages)
                if (page == null || string.IsNullOrWhiteSpace(page.title) || string.IsNullOrWhiteSpace(page.action)) return false;
            return PositiveFinite(paperDuration) && PositiveFinite(overloadDuration) &&
                PositiveFinite(reflectionDuration) && PositiveFinite(calmDuration) && PositiveFinite(overloadPulseSpeed) &&
                overloadShake >= 0 && !float.IsNaN(overloadShake) && !float.IsInfinity(overloadShake);
        }

        static bool PositiveFinite(float value) => value > 0 && !float.IsNaN(value) && !float.IsInfinity(value);

        public float HoldDuration(ChapterFourBeat beat)
        {
            if (beat == ChapterFourBeat.Torn) return Mathf.Clamp(paperDuration, .1f, 5f);
            if (beat == ChapterFourBeat.Overload) return Mathf.Clamp(overloadDuration, .2f, 5f);
            if (beat == ChapterFourBeat.Question || beat == ChapterFourBeat.Memory) return Mathf.Clamp(reflectionDuration, .2f, 5f);
            if (beat == ChapterFourBeat.Calm) return Mathf.Clamp(calmDuration, .2f, 5f);
            return .2f;
        }

        public ChapterFourPage Page(ChapterFourBeat beat) => pages[(int)beat - 1];

        public ChapterFourPage Page(ChapterFourBeat beat, int storyOrderVersion)
        {
            var original = Page(beat);
            if (storyOrderVersion == 0) return original;
            var page = new ChapterFourPage { title = original.title, body = original.body, action = original.action };
            // Existing assets keep their serialized page indices and authored text.
            switch (beat)
            {
                case ChapterFourBeat.Alone:
                    if (page.action == "답장을 뜯는다") page.action = "다가오는 발소리";
                    break;
                case ChapterFourBeat.Reunion:
                    const string reunion = "찢어진 종이가 발치에 흩어졌다. 누군가 걸음을 멈췄다.\n라이벌1: …여기 있었구나. 얼마나 찾아다녔는지 알아?\n\n주인공은 고개를 들지 않았다.\n라이벌1은 주인공 앞에 멈춰 섰다.";
                    if (page.body == reunion) page.body = reunion.Replace("찢어진 종이가 발치에 흩어졌다. ", "");
                    break;
                case ChapterFourBeat.Collapse:
                    const string collapse = "손이 올라가지 않았다. 다가오는 검에도 몸이 움직이지 않았다.\n라이벌1은 급히 검을 거뒀지만, 주인공은 그대로 무너졌다.\n\n싸움은 시작조차 되지 않았다.\n발치에 흩어진 답장 조각이 바람에 밀렸다.";
                    if (page.body == collapse) page.body = collapse.Replace("\n발치에 흩어진 답장 조각이 바람에 밀렸다.", "");
                    if (page.action == "고개를 숙인 채") page.action = "답장을 뜯는다";
                    break;
                case ChapterFourBeat.Torn:
                    if (page.action == "다가오는 발소리") page.action = "고개를 숙인 채";
                    break;
            }
            return page;
        }
    }
}
