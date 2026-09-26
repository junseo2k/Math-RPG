using UnityEngine;

namespace MathRPG
{
    [CreateAssetMenu(menuName = "Math RPG/Training")]
    public class TrainingDefinition : ScriptableObject
    {
        public string locationName = "여명의 연무장";
        public QuestionDefinition[] questions;
        public SkillDefinition runeSkill;
        public DialogueDefinition dialogue;
        public TrainingCourseDefinition course;
        public RivalDuelDefinition rival;
        public ForestVillageDefinition forest;
        public ForestVillageDefinition secondForest;
        public ForestVillageDefinition thirdForest;
        public ForestClimaxDefinition forestClimax;
        public HumanVillageDefinition humanVillage;
        [Min(10)] public int playerHealth = 100;
        [Min(1)] public int enemyHealth = 75;
        [Min(0)] public int wrongAnswerDamage = 3;
        [Min(0)] public int hintCharges = 3;
        [Min(1)] public int instructorQuestionCount = 2;
        [Min(1)] public int enemyDamage = 12;
        [Min(0.2f)] public float enemyWindup = 1.1f;
    }
}
