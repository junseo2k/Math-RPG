using UnityEngine;

namespace MathRPG
{
    [CreateAssetMenu(menuName = "Math RPG/Rival Duel")]
    public sealed class RivalDuelDefinition : ScriptableObject
    {
        public string displayName = "먼저 길을 떠난 가족";
        [TextArea] public string introduction;
        [TextArea] public string victory;
        [TextArea] public string patternChangeLine;
        public QuestionDefinition[] questions;
        [Min(1)] public int health = 420;
        [Min(1)] public int swordDamage = 12;
        [Min(1)] public int exchangePower = 32;
        [Range(.1f, .95f)] public float rivalPowerMultiplier = .55f;
        [Min(1)] public float firstChallengeDelay = 2f;
        [Min(1)] public float challengeInterval = 8f;
        [Min(1)] public float answerTime = 14f;
        [Min(.1f)] public float rivalSolveTime = 3.5f;
        [Min(.2f)] public float openingWindup = 1.1f;
        [Min(.2f)] public float secondWindup = .85f;
        [Min(.1f)] public float openingRecovery = 1.25f;
        [Min(.1f)] public float secondRecovery = .8f;
        public bool[] openingLowAttacks = { true, false };
        public bool[] secondLowAttacks = { true, true, false };

        public bool IsValid()
        {
            if (questions == null || questions.Length < 2 || questions.Length > 3 || health <= 0 || swordDamage <= 0 ||
                exchangePower <= 0 || rivalPowerMultiplier <= 0 || rivalPowerMultiplier >= 1 || firstChallengeDelay < 1 ||
                challengeInterval < 1 || answerTime < 1 || rivalSolveTime <= 0 || rivalSolveTime > answerTime ||
                openingWindup < .2f || secondWindup < .2f || openingRecovery <= 0 || secondRecovery <= 0 ||
                openingLowAttacks == null || openingLowAttacks.Length == 0 || secondLowAttacks == null || secondLowAttacks.Length == 0)
                return false;
            foreach (var question in questions)
                if (question == null || !question.IsValid(out _)) return false;
            return true;
        }
    }
}
