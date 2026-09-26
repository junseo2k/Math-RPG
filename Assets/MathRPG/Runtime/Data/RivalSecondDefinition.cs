using UnityEngine;

namespace MathRPG
{
    [CreateAssetMenu(menuName = "Math RPG/Rival Second")]
    public sealed class RivalSecondDefinition : ScriptableObject
    {
        public string rivalName = "라이벌2";
        [TextArea] public string introduction, breakdown, defeat, departure;
        public QuestionDefinition[] questions;
        [Min(1)] public int health = 380;
        [Min(1)] public int attackDamage = 10;
        [Min(1)] public int runeDamage = 55;
        [Min(1)] public float firstTimeLimit = 16;
        [Min(0)] public float timeReduction = 5;
        [Min(1)] public float minimumTimeLimit = 6;
        [Min(.5f)] public float questionInterval = 5;
        [Min(.2f)] public float attackWindup = 1.2f;
        [Min(.2f)] public float attackInterval = 2;
        [Min(.1f)] public float moveSpeed = 2;
        [Min(.5f)] public float attackRange = 3.2f;
        [Min(0)] public float maximumShake = 4;

        public bool IsValid()
        {
            if (questions == null || questions.Length != 3 || health <= 1 || attackDamage <= 0 || runeDamage <= 0 ||
                minimumTimeLimit < 1 || firstTimeLimit < minimumTimeLimit || timeReduction < 0 || questionInterval < .5f ||
                attackWindup < .2f || attackInterval < .2f || moveSpeed < .1f || attackRange < .5f || maximumShake < 0) return false;
            foreach (var question in questions)
                if (question == null || !question.IsValid(out _)) return false;
            return true;
        }
    }
}
