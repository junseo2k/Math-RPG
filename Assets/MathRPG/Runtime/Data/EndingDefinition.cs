using System;
using UnityEngine;

namespace MathRPG
{
    [Serializable]
    public sealed class EndingPage
    {
        public string title;
        [TextArea(3, 12)] public string body;
        public string action = "계속";
        public Color backdrop = new Color(.13f, .19f, .22f);
        [Range(.2f, 5)] public float minimumHold = .8f;
    }

    [CreateAssetMenu(menuName = "Math RPG/Ending")]
    public sealed class EndingDefinition : ScriptableObject
    {
        public EndingPage[] pages;
        public Color protagonist = new Color(.4f, .67f, .7f);
        public Color family = new Color(.78f, .61f, .43f);
        public Color gold = new Color(.88f, .75f, .44f);
        [Range(.2f, 3)] public float fadeDuration = .8f;
        [Range(.5f, 4)] public float motionDuration = 1.8f;

        public bool IsValid()
        {
            if (pages == null || pages.Length != (int)EndingBeat.Complete || !PositiveFinite(fadeDuration) || !PositiveFinite(motionDuration)) return false;
            foreach (var page in pages)
                if (page == null || string.IsNullOrWhiteSpace(page.title) || string.IsNullOrWhiteSpace(page.body) ||
                    string.IsNullOrWhiteSpace(page.action) || !PositiveFinite(page.minimumHold)) return false;
            return true;
        }
        static bool PositiveFinite(float value) => value > 0 && !float.IsNaN(value) && !float.IsInfinity(value);
        public EndingPage Page(EndingBeat beat) => pages[(int)beat - 1];
    }
}
