using UnityEngine;

namespace MathRPG
{
    public sealed class HuntingGroundView : MonoBehaviour
    {
        GameObject landscape;
        Transform creature, originalVisual;
        TrainingEnemy enemy;
        SpriteRenderer sky, ridge, gate, body;

        public void Initialize(TrainingEnemy opponent)
        {
            enemy = opponent;
            originalVisual = enemy.visual;
            landscape = new GameObject("Hunting landscape");
            landscape.transform.SetParent(transform, false);
            sky = WorldArt.Shape("Hunting sky", new Vector3(0, 1), new Vector2(40, 22), Color.white, parent: landscape.transform, order: -30);
            ridge = WorldArt.Shape("Hunting ridge", new Vector3(0, -.4f), new Vector2(30, 7), Color.white, "Triangle", landscape.transform, -20);
            WorldArt.Shape("Hunting ground", new Vector3(0, -5.3f), new Vector2(40, 4.2f), new Color(.15f, .13f, .1f), parent: landscape.transform, order: -5);
            for (int i = 0; i < 7; i++)
                WorldArt.Shape("Waystone", new Vector3(-9 + i * 3, -2.85f), new Vector2(.4f, .7f), new Color(.42f, .37f, .29f), "Diamond", landscape.transform, -3);
            WorldArt.Shape("Return passage", new Vector3(-10.2f, -1.8f), new Vector2(.3f, 2.5f), new Color(.65f, .55f, .35f), "Diamond", landscape.transform, 4);
            gate = WorldArt.Shape("Forward passage", new Vector3(9.7f, -1.8f), new Vector2(.45f, 2.5f), Color.gray, "Diamond", landscape.transform, 4);
            creature = new GameObject("Hunting creature").transform;
            creature.SetParent(enemy.transform, false);
            body = WorldArt.Shape("Body", new Vector3(0, .65f), new Vector2(1.3f, 1.3f), Color.white, "Disc", creature, 12);
            WorldArt.Shape("Core", new Vector3(0, .8f), new Vector2(.3f, .5f), new Color(1, .9f, .65f), "Diamond", creature, 13);
            Hide();
        }

        public void Show(HuntingAreaDefinition area, bool completed)
        {
            landscape.SetActive(true);
            creature.gameObject.SetActive(true);
            enemy.visual = creature;
            enemy.faceTarget = true;
            sky.color = area.landscape;
            ridge.color = area.landscape * .65f;
            body.color = area.creature;
            SetGate(completed);
        }

        public void SetGate(bool completed) => gate.color = completed ? new Color(.95f, .8f, .4f) : new Color(.35f, .31f, .27f);

        public void Hide()
        {
            if (landscape != null) landscape.SetActive(false);
            if (creature != null) creature.gameObject.SetActive(false);
            if (enemy != null && enemy.visual == creature) enemy.visual = originalVisual;
        }
    }
}
