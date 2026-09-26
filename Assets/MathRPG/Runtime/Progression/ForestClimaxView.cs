using UnityEngine;

namespace MathRPG
{
    public sealed class ForestClimaxView : MonoBehaviour
    {
        TrainingEnemy enemy;
        Transform guardian, body, splitGroup;
        SpriteRenderer[] fragments;

        public void Initialize(TrainingEnemy opponent)
        {
            enemy = opponent;
            guardian = new GameObject("Awakened guardian").transform;
            guardian.SetParent(transform, false);
            guardian.localPosition = new Vector3(7.5f, .6f);
            WorldArt.Shape("Guardian halo", Vector3.zero, new Vector2(1.9f, 1.9f), new Color(.76f, .9f, .7f, .35f), "Disc", guardian, 9);
            WorldArt.Shape("Guardian core", Vector3.zero, new Vector2(.7f, 1.3f), new Color(.94f, .93f, .68f), "Diamond", guardian, 12);
            body = new GameObject("Corrupted spirit aggregate").transform;
            body.SetParent(enemy.transform, false);
            fragments = new SpriteRenderer[3];
            string[] shapes = { "Triangle", "Square", "Disc" };
            for (int i = 0; i < fragments.Length; i++)
                fragments[i] = WorldArt.Shape("Bound fragment", new Vector3((i - 1) * .6f, i == 1 ? 1.3f : .7f), new Vector2(1.1f, 1.3f), new Color(.4f, .26f, .53f), shapes[i], body, 12 + i);
            splitGroup = new GameObject("Split spirits").transform;
            splitGroup.SetParent(transform, false);
            for (int i = 0; i < 4; i++)
                WorldArt.Shape("Laser spirit", new Vector3(-6 + i * 4, i % 2 == 0 ? .5f : 2.2f), new Vector2(.65f, .8f), new Color(.78f, .26f, .18f), "Disc", splitGroup, 12);
            SetVillage(false);
        }

        public void SetVillage(bool visible)
        { guardian.gameObject.SetActive(visible); body.gameObject.SetActive(false); splitGroup.gameObject.SetActive(false); }

        public void SetBoss(bool active)
        {
            body.gameObject.SetActive(active);
            if (!active) return;
            guardian.gameObject.SetActive(true);
            enemy.visual = body;
            enemy.faceTarget = false;
            enemy.gameObject.SetActive(true);
        }

        public void SetState(AggregateStage stage, int splitSpirits, int clones)
        {
            splitGroup.gameObject.SetActive(splitSpirits > 0);
            var tint = stage == AggregateStage.Core ? new Color(.18f, .16f, .14f) : new Color(.4f, .26f, .53f);
            for (int i = 0; i < fragments.Length; i++)
            {
                fragments[i].color = tint;
                fragments[i].transform.localRotation = Quaternion.Euler(0, 0, clones > 1 ? (i - 1) * 25 : 0);
                fragments[i].transform.localPosition = new Vector3((i - 1) * (clones > 1 ? 1.1f : .6f), i == 1 ? 1.3f : .7f);
            }
        }
    }
}
