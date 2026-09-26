using System;
using UnityEngine;

namespace MathRPG
{
    public enum QuestionStepType { MultipleChoice = 0, BlockInput = 1 }

    [Serializable]
    public class QuestionStep
    {
        public QuestionStepType type = QuestionStepType.MultipleChoice;
        public BlockCalculationDefinition calculation;
        public string phase;
        [TextArea] public string prompt;
        public string[] choices;
        public int correctIndex;
        [TextArea] public string hint;
        public TriangleDiagramData diagram;
        public GeometryDiagramData geometry;
    }

    [CreateAssetMenu(menuName = "Math RPG/Question")]
    public class QuestionDefinition : ScriptableObject
    {
        public string conceptId;
        public string title;
        public QuestionStep[] steps;

        public bool IsValid(out string reason)
        {
            if (string.IsNullOrWhiteSpace(conceptId) || steps == null || steps.Length == 0)
            { reason = "A concept and at least one step are required."; return false; }
            foreach (var step in steps)
            {
                if (step == null || string.IsNullOrWhiteSpace(step.prompt))
                { reason = "Invalid question step or answer index."; return false; }
                if (string.IsNullOrWhiteSpace(step.hint))
                { reason = "Every step needs a useful hint."; return false; }
                if (step.type == QuestionStepType.BlockInput)
                {
                    if (step.calculation == null) { reason = "Missing block calculation."; return false; }
                    if (!step.calculation.IsValid(out reason)) return false;
                    continue;
                }
                if (step.type != QuestionStepType.MultipleChoice || step.choices == null ||
                    step.choices.Length < 2 || step.choices.Length > 4 || step.correctIndex < 0 || step.correctIndex >= step.choices.Length)
                { reason = "Invalid question step or answer index."; return false; }
                foreach (var choice in step.choices)
                    if (string.IsNullOrWhiteSpace(choice)) { reason = "Empty answer."; return false; }
            }
            reason = null;
            return true;
        }
    }
}
