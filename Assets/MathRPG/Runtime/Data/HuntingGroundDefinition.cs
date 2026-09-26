using System;
using UnityEngine;

namespace MathRPG
{
    [Serializable]
    public sealed class HuntingLessonDefinition
    {
        public string title;
        [TextArea(2, 4)] public string explanation;
        [TextArea(2, 4)] public string example;
        public QuestionDefinition question;
    }

    [Serializable]
    public sealed class HuntingAreaDefinition
    {
        public string locationName;
        public string chapterIntroduction;
        public HuntingLessonDefinition[] lessons;
        [Min(1)] public int huntTarget = 3;
        [Min(1)] public int enemyHealth = 55;
        [Min(1)] public int enemyDamage = 6;
        [Min(0)] public int wrongAnswerDamage = 3;
        [Min(0)] public int killHeal = 4;
        [Min(.2f)] public float attackWindup = 1.3f;
        [Min(.5f)] public float respawnDelay = 2;
        [Min(1)] public float questionInterval = 6;
        public Color landscape = new Color(.17f, .25f, .22f);
        public Color creature = new Color(.6f, .7f, .4f);
    }

    [CreateAssetMenu(menuName = "Math RPG/Hunting Grounds")]
    public sealed class HuntingGroundDefinition : ScriptableObject
    {
        public const int AreaCount = 12;
        public HuntingAreaDefinition[] areas;

        public bool IsValid(out string reason)
        {
            reason = "사냥터 12개의 데이터가 필요합니다.";
            if (areas == null || areas.Length != AreaCount) return false;
            foreach (var area in areas)
            {
                reason = "사냥터 이름·소개·개념·전투 수치를 확인해주세요.";
                if (area == null || string.IsNullOrWhiteSpace(area.locationName) || string.IsNullOrWhiteSpace(area.chapterIntroduction) ||
                    area.lessons == null || area.lessons.Length == 0 || area.lessons.Length > 8 ||
                    area.huntTarget < area.lessons.Length || area.huntTarget > 1000 || area.enemyHealth < 1 || area.enemyDamage < 1 || area.wrongAnswerDamage < 0 || area.killHeal < 0 ||
                    !FinitePositive(area.attackWindup, .2f) || !FinitePositive(area.respawnDelay, .5f) || !FinitePositive(area.questionInterval, 1)) return false;
                foreach (var lesson in area.lessons)
                    if (lesson == null || string.IsNullOrWhiteSpace(lesson.title) || string.IsNullOrWhiteSpace(lesson.explanation) ||
                        string.IsNullOrWhiteSpace(lesson.example) || lesson.question == null || !lesson.question.IsValid(out reason)) return false;
            }
            reason = null;
            return true;
        }

        static bool FinitePositive(float value, float minimum) => !float.IsNaN(value) && !float.IsInfinity(value) && value >= minimum;
    }
}
