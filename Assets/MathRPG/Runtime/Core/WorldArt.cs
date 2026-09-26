using UnityEngine;

namespace MathRPG
{
    public static class WorldArt
    {
        public static SpriteRenderer Shape(string name, Vector3 position, Vector2 size, Color color,
            string shape = "Square", Transform parent = null, int order = 0)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = new Vector3(size.x, size.y, 1);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = Resources.Load<Sprite>("MathRPG/" + shape);
            renderer.color = color;
            renderer.sortingOrder = order;
            return renderer;
        }

        public static Transform Character(Transform root, bool instructor = false)
        {
            var visual = new GameObject("Visual").transform;
            visual.SetParent(root, false);
            Color cloth = instructor ? new Color(.58f, .52f, .39f) : new Color(.18f, .55f, .68f);
            Shape("Boot left", new Vector3(-.2f, .2f), new Vector2(.22f, .4f), new Color(.12f, .18f, .24f), parent: visual, order: 12);
            Shape("Boot right", new Vector3(.2f, .2f), new Vector2(.22f, .4f), new Color(.12f, .18f, .24f), parent: visual, order: 12);
            Shape("Cloak", new Vector3(-.15f, .78f), new Vector2(.95f, 1.2f), cloth * .75f, "Triangle", visual, 10);
            Shape("Tunic", new Vector3(0, .87f), new Vector2(.64f, .85f), cloth, parent: visual, order: 13);
            Shape("Belt", new Vector3(0, .55f), new Vector2(.67f, .1f), new Color(.92f, .71f, .38f), parent: visual, order: 14);
            Shape("Face", new Vector3(0, 1.52f), new Vector2(.49f, .53f), new Color(.95f, .79f, .61f), "Disc", visual, 13);
            Shape("Hair", new Vector3(-.06f, 1.72f), new Vector2(.59f, .3f), new Color(.15f, .23f, .29f), "Disc", visual, 14);
            Shape("Rune glove", new Vector3(.43f, .98f), new Vector2(.25f, .28f), new Color(.53f, .97f, 1), "Diamond", visual, 15);
            var sword = Shape("Sword", new Vector3(.65f, .75f), new Vector2(.11f, 1.25f), new Color(.81f, .88f, .84f), "Diamond", visual, 11);
            sword.transform.localRotation = Quaternion.Euler(0, 0, -24);
            return visual;
        }

        public static void BuildEnvironment(Transform root)
        {
            Shape("Sky", new Vector3(0, 1, 0), new Vector2(40, 22), new Color(.055f, .105f, .16f), parent: root, order: -30);
            Shape("Horizon", new Vector3(0, -1.4f), new Vector2(35, 4), new Color(.11f, .22f, .28f), parent: root, order: -29);
            Shape("Moon halo", new Vector3(6.5f, 3.6f), new Vector2(2.4f, 2.4f), new Color(.29f, .47f, .47f, .25f), "Disc", root, -28);
            Shape("Moon", new Vector3(6.5f, 3.6f), new Vector2(1.5f, 1.5f), new Color(.83f, .84f, .67f), "Disc", root, -27);
            for (int i = 0; i < 35; i++)
                Shape("Star", new Vector3(-14 + (i * 7.37f) % 28, 1.4f + (i * 1.71f) % 4.2f), new Vector2(.028f, .028f), new Color(.62f, .77f, .8f, .6f), "Diamond", root, -26);
            for (int i = 0; i < 9; i++)
            {
                float x = -15 + i * 4;
                Shape("Distant mountain", new Vector3(x, -.8f), new Vector2(6, 5 + (i % 3)), new Color(.085f, .17f, .23f), "Triangle", root, -25);
            }
            for (int i = 0; i < 5; i++)
            {
                float x = -10 + i * 5;
                Shape("Stone column", new Vector3(x, -.4f), new Vector2(.65f, 5.6f), new Color(.19f, .28f, .32f), parent: root, order: -15);
                Shape("Column cap", new Vector3(x, 2.35f), new Vector2(1.15f, .25f), new Color(.3f, .39f, .39f), parent: root, order: -14);
                Shape("Column foot", new Vector3(x, -2.95f), new Vector2(1.1f, .5f), new Color(.25f, .34f, .35f), parent: root, order: -14);
                Shape("Column highlight", new Vector3(x - .2f, -.4f), new Vector2(.09f, 5.2f), new Color(.31f, .41f, .42f, .5f), parent: root, order: -14);
                if (i == 1 || i == 3)
                {
                    Shape("Banner", new Vector3(x + .85f, 1.05f), new Vector2(.95f, 2.2f), new Color(.17f, .4f, .46f), parent: root, order: -12);
                    Shape("Banner rune", new Vector3(x + .85f, 1.2f), new Vector2(.32f, .55f), new Color(.78f, .74f, .51f), "Diamond", root, -11);
                }
            }
            Shape("Wall rail", new Vector3(0, -1.95f), new Vector2(30, .3f), new Color(.17f, .28f, .32f), parent: root, order: -16);
            Shape("Ground", new Vector3(0, -5.3f), new Vector2(40, 4.2f), new Color(.08f, .15f, .2f), parent: root, order: -5);
            Shape("Stone path", new Vector3(0, -3.35f), new Vector2(30, .3f), new Color(.38f, .45f, .44f), parent: root, order: -4);
            Shape("Path rim", new Vector3(0, -3.21f), new Vector2(30, .045f), new Color(.68f, .67f, .53f), parent: root, order: -3);
            for (int i = 0; i < 23; i++)
                Shape("Paving seam", new Vector3(-14 + i * 1.3f, -3.35f), new Vector2(.026f, .28f), new Color(.13f, .22f, .25f), parent: root, order: -2);
            for (int i = 0; i < 12; i++)
                Shape("Grass", new Vector3(-11 + i * 2.1f, -3.07f), new Vector2(.16f, .3f), new Color(.29f, .45f, .39f), "Triangle", root, -1);
            Shape("Checkpoint ring", new Vector3(-6, -3.13f), new Vector2(1.5f, .15f), new Color(.39f, .9f, .91f, .7f), "Disc", root, 1);
        }
    }
}
