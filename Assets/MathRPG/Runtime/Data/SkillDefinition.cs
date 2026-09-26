using UnityEngine;

namespace MathRPG
{
    [CreateAssetMenu(menuName = "Math RPG/Skill")]
    public class SkillDefinition : ScriptableObject
    {
        public string displayName = "여명 가르기";
        [Min(1)] public int damage = 46;
        [Min(0.1f)] public float cooldown = 3f;
        [Min(1)] public float range = 6f;
        [Min(1)] public int booksForInstantCast = 2;
    }
}
