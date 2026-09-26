using UnityEngine;

namespace MathRPG
{
    public sealed class ForestVillageView : MonoBehaviour
    {
        TrainingEnemy enemy;
        Transform environment, guide, boss, relic;
        Transform parallelRelic, circleRelic;
        SpriteRenderer sky, ground, gate, body;
        bool secondVillage;
        bool thirdVillage;
        bool visible;

        public void Initialize(TrainingEnemy opponent)
        {
            enemy = opponent;
            environment = new GameObject("Spirit forest").transform;
            environment.SetParent(transform, false);
            sky = WorldArt.Shape("Forest sky", Vector3.zero, new Vector2(30, 18), new Color(.06f, .17f, .15f), parent: environment, order: -30);
            ground = WorldArt.Shape("Moss ground", new Vector3(0, -4.5f), new Vector2(30, 2.6f), new Color(.16f, .28f, .2f), parent: environment, order: -10);
            for (int i = 0; i < 9; i++)
            {
                float x = -12 + i * 3;
                WorldArt.Shape("Tree trunk", new Vector3(x, .3f), new Vector2(.65f, 8), new Color(.12f, .24f, .2f), parent: environment, order: -24);
                WorldArt.Shape("Tree canopy", new Vector3(x, 3.6f), new Vector2(5, 4), new Color(.1f, .3f, .23f), "Disc", environment, -23);
            }
            guide = new GameObject("Three leaf spirit").transform;
            guide.SetParent(environment, false);
            guide.localPosition = new Vector3(-8.7f, -1.7f);
            for (int i = 0; i < 3; i++)
                WorldArt.Shape("Leaf", new Vector3((i - 1) * .35f, i == 1 ? .25f : 0), new Vector2(.5f, .9f), new Color(.57f, .94f, .63f), "Diamond", guide, 12);
            WorldArt.Shape("Return gate", new Vector3(-10.25f, -1.8f), new Vector2(.3f, 2.5f), Color.cyan, "Diamond", environment, 4);
            gate = WorldArt.Shape("Root gate", new Vector3(9.7f, -1.8f), new Vector2(.8f, 2.8f), new Color(.67f, .39f, .53f), "Triangle", environment, 4);
            relic = WorldArt.Shape("Triangle relic", new Vector3(6.5f, -1.5f), Vector2.one, new Color(.95f, .84f, .46f), "Triangle", environment, 12).transform;
            parallelRelic = new GameObject("Parallelogram relic").transform;
            parallelRelic.SetParent(environment, false);
            parallelRelic.localPosition = relic.localPosition;
            var corners = new[] { new Vector2(-.35f, .45f), new Vector2(.65f, .45f), new Vector2(.35f, -.45f), new Vector2(-.65f, -.45f) };
            for (int i = 0; i < corners.Length; i++)
            {
                var start = corners[i];
                var end = corners[(i + 1) % corners.Length];
                var edge = WorldArt.Shape("Relic edge", (start + end) * .5f, new Vector2((end - start).magnitude, .1f), new Color(.95f, .84f, .46f), parent: parallelRelic, order: 12);
                edge.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(end.y - start.y, end.x - start.x) * Mathf.Rad2Deg);
            }
            circleRelic = new GameObject("Circle relic").transform;
            circleRelic.SetParent(environment, false);
            circleRelic.localPosition = relic.localPosition;
            for (int i = 0; i < 32; i++)
            {
                float a = i * 2 * Mathf.PI / 32;
                float b = (i + 1) * 2 * Mathf.PI / 32;
                var start = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * .55f;
                var end = new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * .55f;
                var edge = WorldArt.Shape("Circle edge", (start + end) * .5f, new Vector2((end - start).magnitude, .08f), new Color(.95f, .84f, .46f), parent: circleRelic, order: 12);
                edge.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(end.y - start.y, end.x - start.x) * Mathf.Rad2Deg);
            }
            boss = new GameObject("Root spirit visual").transform;
            boss.SetParent(enemy.transform, false);
            body = WorldArt.Shape("Root body", new Vector3(0, .95f), new Vector2(1.8f, 2), new Color(.45f, .24f, .36f), "Triangle", boss, 12);
            WorldArt.Shape("Spirit core", new Vector3(0, .85f), new Vector2(.55f, .6f), new Color(.86f, .58f, .8f), "Diamond", boss, 13);
            SetVisible(false, false);
        }

        public void SetVisible(bool value, bool hasRelic)
        {
            visible = value;
            environment.gameObject.SetActive(value);
            guide.gameObject.SetActive(true);
            relic.gameObject.SetActive(hasRelic && !secondVillage && !thirdVillage);
            parallelRelic.gameObject.SetActive(hasRelic && secondVillage);
            circleRelic.gameObject.SetActive(hasRelic && thirdVillage);
            boss.gameObject.SetActive(false);
        }

        public void SetGuideVisible(bool value) => guide.gameObject.SetActive(value);

        public void SetBossMode(bool active)
        {
            boss.gameObject.SetActive(active);
            enemy.gameObject.SetActive(active);
            guide.gameObject.SetActive(visible && !active);
            if (!active) return;
            enemy.visual = boss;
            enemy.faceTarget = false;
        }

        public void SetVillage(int index)
        {
            secondVillage = index == 1;
            thirdVillage = index == 2;
            sky.color = thirdVillage ? new Color(.17f, .1f, .25f) : secondVillage ? new Color(.07f, .14f, .24f) : new Color(.06f, .17f, .15f);
            ground.color = thirdVillage ? new Color(.3f, .23f, .38f) : secondVillage ? new Color(.13f, .3f, .38f) : new Color(.16f, .28f, .2f);
            gate.sprite = Resources.Load<Sprite>("MathRPG/" + (thirdVillage ? "Disc" : secondVillage ? "Diamond" : "Triangle"));
            body.sprite = Resources.Load<Sprite>("MathRPG/" + (thirdVillage ? "Disc" : secondVillage ? "Square" : "Triangle"));
            body.color = thirdVillage ? new Color(.55f, .32f, .65f) : secondVillage ? new Color(.25f, .37f, .61f) : new Color(.45f, .24f, .36f);
            foreach (var leaf in guide.GetComponentsInChildren<SpriteRenderer>())
                leaf.color = thirdVillage ? new Color(.89f, .71f, 1f) : secondVillage ? new Color(.56f, .79f, 1f) : new Color(.57f, .94f, .63f);
        }
    }
}
