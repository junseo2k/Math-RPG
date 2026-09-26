using UnityEngine;

namespace MathRPG
{
    [CreateAssetMenu(menuName = "Math RPG/Human Village")]
    public sealed class HumanVillageDefinition : ScriptableObject
    {
        public HumanVillageDefinition nextVillage;
        public string villageName = "잿빛 나루";
        public string residentName = "나루지기";
        public string bossName = "탁류의 포식자";
        [TextArea] public string arrival, request, aftermath, ambient, clearedAmbient, rewind;
        public QuestionDefinition[] questions;
        [Min(1)] public int bossHealth = 460;
        [Min(1)] public int attackDamage = 12;
        [Min(1)] public int wrongDamage = 8;
        [Min(1)] public int runeDamage = 48;
        [Min(0)] public int correctHeal = 6;
        [Min(.2f)] public float attackWindup = 1.2f;
        [Min(.5f)] public float firstQuestionDelay = 2;
        [Min(1)] public float questionInterval = 4;

        public bool IsValid()
        {
            if (string.IsNullOrWhiteSpace(villageName) || questions == null || questions.Length < 2 || questions.Length > 3 ||
                bossHealth <= 0 || attackDamage <= 0 || wrongDamage <= 0 || runeDamage <= 0 || correctHeal < 0 ||
                attackWindup < .2f || firstQuestionDelay < .5f || questionInterval < 1) return false;
            foreach (var question in questions)
                if (question == null || !question.IsValid(out _)) return false;
            return true;
        }
    }
}
