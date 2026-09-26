using UnityEngine;

namespace MathRPG
{
    public sealed class RivalSecondRematchView : MonoBehaviour
    {
        TrainingGame game;
        GameObject root, opponent;
        public Transform HitVisual => opponent != null ? opponent.transform : null;
        Sprite square;
        SpriteRenderer body, hazard, borderLeft, borderRight;

        public void Initialize(TrainingGame controller)
        {
            game = controller;
            square = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height), new Vector2(.5f, .5f), Texture2D.whiteTexture.width);
            root = new GameObject("Rematch road");
            root.transform.SetParent(transform, false);
            Box("Sky", root.transform, new Vector3(0, 0, 2), new Vector2(24, 14), new Color(.13f, .12f, .18f), -30);
            Box("Road", root.transform, new Vector3(0, PlayerMotor.GroundY - .3f, 1), new Vector2(22, .4f), new Color(.28f, .27f, .28f), -20);
            for (int i = 0; i < 6; i++) Box("Rubble", root.transform, new Vector3(-9 + i * 3.3f, PlayerMotor.GroundY - .1f, 1), new Vector2(.7f, .35f), new Color(.4f, .36f, .39f), -18);
            opponent = new GameObject("Commander");
            opponent.transform.SetParent(root.transform, false);
            body = Box("Coat", opponent.transform, new Vector3(0, .85f, 0), new Vector2(.65f, 1.25f), Color.white, 20);
            Box("Head", opponent.transform, new Vector3(0, 1.7f, 0), new Vector2(.43f, .43f), new Color(.78f, .66f, .56f), 21);
            Box("Sword", opponent.transform, new Vector3(-.5f, .95f, 0), new Vector2(.09f, 1.45f), new Color(.78f, .83f, .85f), 21);
            hazard = Box("Rune column warning", root.transform, Vector3.zero, Vector2.one, Color.red, 15);
            borderLeft = Box("Left hazard boundary", root.transform, Vector3.zero, Vector2.one, Color.red, 16);
            borderRight = Box("Right hazard boundary", root.transform, Vector3.zero, Vector2.one, Color.red, 16);
            Hide();
        }

        SpriteRenderer Box(string name, Transform parent, Vector3 position, Vector2 size, Color color, int order)
        {
            var item = new GameObject(name);
            item.transform.SetParent(parent, false);
            item.transform.localPosition = position;
            item.transform.localScale = new Vector3(size.x, size.y, 1);
            var renderer = item.AddComponent<SpriteRenderer>();
            renderer.sprite = square; renderer.color = color; renderer.sortingOrder = order;
            return renderer;
        }

        public void Show()
        {
            root.SetActive(true);
            opponent.SetActive(true);
            body.color = game.SecondRematch.Definition.rivalColor;
            Refresh();
        }
        public void Hide() { if (root != null) root.SetActive(false); }
        public void HideOpponent() { opponent.SetActive(false); }
        void LateUpdate() { if (root != null && root.activeSelf) Refresh(); }
        void Refresh()
        {
            var encounter = game.SecondRematch;
            opponent.transform.position = game.enemy.transform.position;
            body.color = Color.Lerp(encounter.Definition.rivalColor, new Color(.9f, .25f, .2f), (float)encounter.Rage / encounter.Definition.maximumRage);
            bool visible = encounter.HazardActive;
            hazard.gameObject.SetActive(visible);
            borderLeft.gameObject.SetActive(visible);
            borderRight.gameObject.SetActive(visible);
            if (!visible) return;
            var center = new Vector3(encounter.HazardX, PlayerMotor.GroundY + 2.5f, 0);
            hazard.transform.position = center;
            hazard.transform.localScale = new Vector3(encounter.HazardRadius * 2, 5, 1);
            var color = encounter.Definition.hazardColor;
            color.a = Mathf.Lerp(.18f, .5f, 1 - Mathf.Clamp01(encounter.HazardRemaining / encounter.Definition.hazardWarning));
            hazard.color = color;
            color.a = 1;
            borderLeft.color = borderRight.color = color;
            borderLeft.transform.position = center + Vector3.left * encounter.HazardRadius;
            borderRight.transform.position = center + Vector3.right * encounter.HazardRadius;
            borderLeft.transform.localScale = borderRight.transform.localScale = new Vector3(.06f, 5, 1);
        }
        void OnDestroy() { if (square != null) Destroy(square); }
    }
}
