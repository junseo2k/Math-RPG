using UnityEngine;

namespace MathRPG
{
    [CreateAssetMenu(menuName = "Math RPG/Forest Climax")]
    public sealed class ForestClimaxDefinition : ScriptableObject
    {
        public string bossName = "타락 정령 집합체";
        [TextArea] public string awakening, introduction, victory;
        public QuestionDefinition[] questions;
        [Min(1)] public int health = 720;
        [Min(1)] public int attackDamage = 14;
        [Min(1)] public int wrongDamage = 8;
        [Min(1)] public int runeDamage = 42;
        [Min(0)] public int correctHeal = 6;
        [Min(1)] public int poisonDamage = 2;
        [Min(1)] public int guardianDamage = 108;
        [Range(.01f, .5f)] public float guardianDamageFraction = .15f;
        [Min(3)] public float captureTime = 15;
        [Min(3)] public float rageTime = 18;
        [Min(1)] public int rageDamageTarget = 120;
        [Min(.2f)] public float minimumWindup = .65f;
        [Min(.2f)] public float openingWindup = 1.3f;
        [Min(.2f)] public float secondWindup = 1.1f;
        [Min(.1f)] public float recovery = 1.2f;
        [Min(.5f)] public float firstQuestionDelay = 2;
        [Min(1)] public float questionInterval = 4;
        public bool[] openingLowAttacks = { true, false };
        public bool[] secondLowAttacks = { true, true, false };

        public bool IsValid()
        {
            if (questions == null || questions.Length < 2 || questions.Length > 3 || health <= 0 || attackDamage <= 0 ||
                wrongDamage <= 0 || runeDamage <= 0 || correctHeal < 0 || poisonDamage <= 0 || guardianDamage <= 0 ||
                guardianDamageFraction <= 0 || captureTime < 3 || rageTime < 3 || rageDamageTarget <= 0 ||
                minimumWindup < .2f || openingWindup < minimumWindup || secondWindup < minimumWindup || recovery < .1f ||
                firstQuestionDelay < .5f || questionInterval < 1 || openingLowAttacks == null || openingLowAttacks.Length == 0 ||
                secondLowAttacks == null || secondLowAttacks.Length == 0) return false;
            foreach (var question in questions)
                if (question == null || !question.IsValid(out _)) return false;
            return true;
        }
    }
}
