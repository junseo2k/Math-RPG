using UnityEngine;

namespace MathRPG
{
    [CreateAssetMenu(menuName = "Math RPG/Dialogue")]
    public class DialogueDefinition : ScriptableObject
    {
        [TextArea] public string[] introduction;
        [TextArea] public string[] ambient;
        [TextArea] public string[] rewind;
        [TextArea] public string instructorGreeting;
        [TextArea] public string instructorReward;
    }
}
