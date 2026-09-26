using System;
using UnityEngine;

namespace MathRPG
{
    [Serializable]
    public sealed class CalamityAnswer
    {
        [TextArea] public string prompt;
        public string[] choices;
        public int correctIndex;
        public bool IsValid() => !string.IsNullOrWhiteSpace(prompt) && choices != null && choices.Length == 4 &&
            Array.TrueForAll(choices, value => !string.IsNullOrWhiteSpace(value)) && correctIndex >= 0 && correctIndex < 4;
    }

    [Serializable]
    public sealed class CalamityProblem
    {
        public string title;
        public CalamityAnswer[] directSteps;
        public CalamityAnswer shortcut;
        [TextArea] public string insight;
        public bool IsValid() => !string.IsNullOrWhiteSpace(title) && !string.IsNullOrWhiteSpace(insight) &&
            directSteps != null && directSteps.Length >= 2 && Array.TrueForAll(directSteps, step => step != null && step.IsValid()) &&
            shortcut != null && shortcut.IsValid();
    }

    [CreateAssetMenu(menuName = "Math RPG/Final Calamity")]
    public sealed class FinalCalamityDefinition : ScriptableObject
    {
        public string bossName = "재앙 — 어둠의 기사";
        [TextArea(3, 12)] public string introduction, farewell, isolation, phaseTwo, phaseThree, victory;
        public CalamityProblem[] problems;
        [Min(3)] public int health = 840;
        [Min(1)] public int swordDamage = 18, runeDamage = 90, wrongDamage = 8, pressureDamage = 2;
        [Min(0)] public int rageDamage = 2;
        [Range(1, 5)] public int maximumRage = 3;
        [Range(.55f, .8f)] public float secondPhaseHealth = .67f;
        [Range(.2f, .45f)] public float thirdPhaseHealth = .34f;
        [Range(.8f, 3)] public float attackInterval = 2.1f;
        [Range(.5f, 2)] public float attackWindup = 1.1f;
        [Range(.5f, 1)] public float finalAttackMultiplier = .75f;
        [Range(0, .1f)] public float rageSpeedStep = .06f;
        [Range(1, 4)] public float moveSpeed = 2.2f;
        [Range(2, 4)] public float attackRange = 3f;
        [Range(6, 10)] public float arenaHalfWidth = 9f;
        [Range(3, 12)] public float questionInterval = 7f;
        [Range(8, 30)] public float questionTime = 15f;
        [Range(1, 5)] public float timeCrushAfter = 3f;
        [Range(2, 8)] public float firstCrushedTime = 5f;
        [Range(1, 5)] public float secondCrushedTime = 3f;
        [Range(.5f, 1.5f)] public float narrowRadius = 1f;
        [Range(1.5f, 3)] public float wideRadius = 2.5f;
        [Range(1.5f, 4)] public float hazardWarning = 2.5f;
        [Range(3, 10)] public float finalHazardInterval = 5f;
        [Range(.5f, 3)] public float hazardRecovery = 1.4f;
        [Range(3, 15)] public float pressureInterval = 7f;
        [Range(8, 25)] public float spiritInterval = 16f;
        [Range(1, 3)] public float spiritWarning = 1.5f;
        [Range(1, 5)] public float spiritDuration = 3f;
        public Color armorColor = new Color(.23f, .19f, .32f);
        public Color runeColor = new Color(.75f, .32f, .84f);
        public Color spiritColor = new Color(.54f, .93f, 1f, .8f);

        public bool IsValid()
        {
            if (problems == null || problems.Length != 3 || !Array.TrueForAll(problems, p => p != null && p.IsValid()) ||
                health < 3 || swordDamage < 1 || runeDamage < 1 || wrongDamage < 1 || pressureDamage < 1 || rageDamage < 0 ||
                maximumRage < 1 || maximumRage > 5) return false;
            foreach (float value in new[] { secondPhaseHealth, thirdPhaseHealth, attackInterval, attackWindup, finalAttackMultiplier,
                rageSpeedStep, moveSpeed, attackRange, arenaHalfWidth, questionInterval, questionTime, timeCrushAfter, firstCrushedTime,
                secondCrushedTime, narrowRadius, wideRadius, hazardWarning, finalHazardInterval, hazardRecovery,
                pressureInterval, spiritInterval, spiritWarning, spiritDuration })
                if (float.IsNaN(value) || float.IsInfinity(value) || value < 0) return false;
            return secondPhaseHealth >= .55f && secondPhaseHealth <= .8f && thirdPhaseHealth >= .2f && thirdPhaseHealth <= .45f &&
                attackInterval >= .8f && attackWindup >= .5f && finalAttackMultiplier >= .5f && finalAttackMultiplier <= 1 &&
                rageSpeedStep <= .1f && moveSpeed >= 1 && attackRange >= 2 && attackRange <= 4 && arenaHalfWidth >= 6 && arenaHalfWidth <= 10 &&
                questionInterval >= 3 && questionTime >= 8 && timeCrushAfter >= 1 && timeCrushAfter < questionTime &&
                firstCrushedTime >= 2 && secondCrushedTime >= 1 && secondCrushedTime <= firstCrushedTime &&
                firstCrushedTime < questionTime - timeCrushAfter && narrowRadius >= .5f && wideRadius > narrowRadius && wideRadius <= 3 &&
                hazardWarning >= 1.5f && finalHazardInterval >= 3 && hazardRecovery >= .5f && pressureInterval >= 3 &&
                spiritInterval >= 8 && spiritWarning >= 1 && spiritDuration >= 1 && spiritDuration <= 5;
        }
    }
}
