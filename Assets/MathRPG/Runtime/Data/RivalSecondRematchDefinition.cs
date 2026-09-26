using UnityEngine;

namespace MathRPG
{
    [CreateAssetMenu(menuName = "Math RPG/Rival Second Rematch")]
    public sealed class RivalSecondRematchDefinition : ScriptableObject
    {
        public string rivalName = "라이벌2";
        [TextArea(3, 10)] public string introduction, lesson, phaseDialogue, victory, departure;
        public QuestionDefinition[] questions;
        [Min(2)] public int health = 560;
        [Min(1)] public int swordDamage = 12, runeDamage = 65, wrongDamage = 8, narrowHazardDamage = 16;
        [Min(0)] public int rageDamage = 3;
        [Range(1, 5)] public int maximumRage = 3;
        [Range(0, 1)] public float runeFailureChance = .35f;
        [Range(0, 1)] public float phaseTwoFailureChance = .5f;
        [Range(.2f, .8f)] public float phaseThreshold = .5f;
        [Min(.3f)] public float attackInterval = 2f, attackWindup = 1f;
        [Range(.3f, 1)] public float phaseTwoIntervalMultiplier = .75f;
        [Range(0, .15f)] public float rageSpeedStep = .08f;
        [Min(1)] public float questionInterval = 6f;
        [Min(.1f)] public float moveSpeed = 2.3f;
        [Min(.5f)] public float attackRange = 2.8f;
        [Range(6, 10)] public float arenaHalfWidth = 9f;
        [Range(.5f, 1.5f)] public float narrowHazardRadius = .9f;
        [Range(1.5f, 3)] public float wideHazardRadius = 2.3f;
        [Range(1.5f, 4)] public float hazardWarning = 2.4f;
        public Color rivalColor = new Color(.68f, .44f, .32f);
        public Color hazardColor = new Color(1, .32f, .25f, .38f);

        public bool IsValid()
        {
            if (questions == null || questions.Length != 3 || health < 2 || swordDamage < 1 || runeDamage < 1 ||
                wrongDamage < 1 || narrowHazardDamage < 1 || rageDamage < 0 || maximumRage < 1 || maximumRage > 5) return false;
            foreach (float value in new[] { runeFailureChance, phaseTwoFailureChance, phaseThreshold, attackInterval, attackWindup,
                phaseTwoIntervalMultiplier, rageSpeedStep, questionInterval, moveSpeed, attackRange, arenaHalfWidth,
                narrowHazardRadius, wideHazardRadius, hazardWarning })
                if (float.IsNaN(value) || float.IsInfinity(value)) return false;
            if (runeFailureChance < 0 || runeFailureChance > 1 || phaseTwoFailureChance < 0 || phaseTwoFailureChance > 1 ||
                phaseThreshold < .2f || phaseThreshold > .8f || attackInterval < .3f || attackWindup < .3f ||
                phaseTwoIntervalMultiplier < .3f || phaseTwoIntervalMultiplier > 1 || rageSpeedStep < 0 || rageSpeedStep > .15f ||
                questionInterval < 1 || moveSpeed <= 0 || attackRange < .5f || attackRange > 5 || arenaHalfWidth < 6 || arenaHalfWidth > 10 ||
                narrowHazardRadius < .5f || wideHazardRadius < narrowHazardRadius || wideHazardRadius > 3 || hazardWarning < 1.5f) return false;
            foreach (var question in questions)
                if (question == null || !question.IsValid(out _)) return false;
            return true;
        }
    }
}
