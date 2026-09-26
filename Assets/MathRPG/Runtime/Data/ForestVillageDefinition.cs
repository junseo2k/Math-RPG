using UnityEngine;

namespace MathRPG
{
    public enum ForestEncounterKind { Collector, DevouringSpirit, Chest }

    [CreateAssetMenu(menuName = "Math RPG/Forest Village")]
    public sealed class ForestVillageDefinition : ScriptableObject
    {
        public string villageName = "세잎의 정령 마을";
        public string guideName = "세잎 정령";
        public string bossName = "뒤틀린 뿌리 정령";
        public string relicName = "삼각의 유물";
        [TextArea] public string arrival;
        [TextArea] public string request;
        [TextArea] public string reward;
        [TextArea] public string ambient;
        [TextArea] public string rewind;
        [TextArea] public string clearEvent;
        public ForestEncounterKind encounterKind;
        public QuestionDefinition[] questions;
        [Min(1)] public int bossHealth = 360;
        [Min(1)] public int attackDamage = 12;
        [Min(1)] public int drainDamage = 8;
        [Min(1)] public int runeDamage = 36;
        [Min(0)] public int correctHeal = 6;
        [Min(.2f)] public float attackWindup = 1.2f;
        [Min(.5f)] public float firstQuestionDelay = 1.6f;
        [Min(1)] public float questionInterval = 4f;
        [Min(3)] public float gimmickTime = 15f;
        [Min(1)] public int gimmickCount = 3;

        public bool IsValid()
        {
            if (string.IsNullOrWhiteSpace(villageName) || questions == null || questions.Length < 2 || questions.Length > 3 ||
                bossHealth <= 0 || attackDamage <= 0 || drainDamage <= 0 || runeDamage <= 0 || correctHeal < 0 ||
                attackWindup < .2f || firstQuestionDelay < .5f || questionInterval < 1 || gimmickTime < 3 || gimmickCount < 1) return false;
            foreach (var question in questions)
                if (question == null || !question.IsValid(out _)) return false;
            return true;
        }
    }
}
