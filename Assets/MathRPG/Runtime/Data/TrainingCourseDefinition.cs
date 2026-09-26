using System;
using UnityEngine;

namespace MathRPG
{
    public enum InstructorTrialKind { Questions, Hunt, Treasure, Sparring }

    [Serializable]
    public sealed class TrainingStageDefinition
    {
        public string locationName;
        public string instructorName;
        public InstructorTrialKind trialKind;
        [TextArea] public string briefing;
        [TextArea] public string rewardDialogue;
        public string bookName;
        public QuestionDefinition[] questions;
        public QuestionDefinition[] chestQuestions;
        [Min(1)] public int questionCount = 2;
        [Min(1)] public int huntTarget = 3;
        [Min(1)] public int enemyHealth = 75;
        [Min(1)] public int enemyDamage = 12;
        [Min(.2f)] public float enemyWindup = 1.1f;
        public Color accent = new Color(.46f, .9f, .91f);
    }

    [CreateAssetMenu(menuName = "Math RPG/Training Course")]
    public sealed class TrainingCourseDefinition : ScriptableObject
    {
        public const int StageCount = 4;
        public TrainingStageDefinition[] stages;

        public bool IsValid()
        {
            if (stages == null || stages.Length != StageCount) return false;
            for (int i = 0; i < stages.Length; i++)
            {
                var stage = stages[i];
                if (stage == null || stage.trialKind != (InstructorTrialKind)i || string.IsNullOrWhiteSpace(stage.locationName) ||
                    stage.questionCount < 1 || stage.huntTarget < 1 || stage.enemyHealth < 1 || stage.enemyDamage < 1 || stage.enemyWindup < .2f ||
                    !ValidQuestions(stage.questions)) return false;
                if (stage.trialKind == InstructorTrialKind.Treasure &&
                    (!ValidQuestions(stage.chestQuestions) || stage.chestQuestions.Length != 2)) return false;
            }
            return true;
        }

        static bool ValidQuestions(QuestionDefinition[] questions)
        {
            if (questions == null || questions.Length == 0) return false;
            foreach (var question in questions)
                if (question == null || !question.IsValid(out _)) return false;
            return true;
        }
    }
}
