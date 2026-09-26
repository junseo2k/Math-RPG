using UnityEngine;

namespace MathRPG
{
    [CreateAssetMenu(menuName = "Math RPG/Human Climax")]
    public sealed class HumanClimaxDefinition : ScriptableObject
    {
        public string bossName = "검은 범람";
        [TextArea] public string letter, disaster, victory;
        public QuestionDefinition[] questions;
        [Min(1)] public int bossHealth = 680;
        [Min(1)] public int attackDamage = 13;
        [Min(1)] public int wrongDamage = 8;
        [Min(1)] public int rageDamage = 2;
        [Min(1)] public int maxRage = 3;
        [Min(1)] public int runeDamage = 60;
        [Min(1)] public int sealBreakDamage = 100;
        [Min(0)] public int correctHeal = 6;
        [Min(.2f)] public float attackWindup = 1.2f;
        [Min(.2f)] public float attackInterval = 1.8f;
        [Min(.5f)] public float attackRange = 4.5f;
        public bool[] firstPattern = { true, false, true };
        public bool[] secondPattern = { false, true, false };
        [Min(.5f)] public float firstQuestionDelay = 2;
        [Min(1)] public float questionInterval = 4;

        public bool IsValid()
        {
            if (string.IsNullOrWhiteSpace(bossName) || questions == null || questions.Length != 3 ||
                bossHealth <= 0 || attackDamage <= 0 || wrongDamage <= 0 || rageDamage <= 0 || maxRage <= 0 ||
                runeDamage <= 0 || sealBreakDamage <= 0 || correctHeal < 0 || attackWindup < .2f ||
                firstQuestionDelay < .5f || questionInterval < 1 || attackInterval < .2f || attackRange < .5f ||
                firstPattern == null || firstPattern.Length == 0 || secondPattern == null || secondPattern.Length == 0) return false;
            foreach (var question in questions)
                if (question == null || !question.IsValid(out _)) return false;
            return true;
        }
    }
}
