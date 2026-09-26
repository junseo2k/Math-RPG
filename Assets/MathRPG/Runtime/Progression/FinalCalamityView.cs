using UnityEngine;

namespace MathRPG
{
    public sealed class FinalCalamityView : MonoBehaviour
    {
        TrainingGame game;
        GameObject root, boss;
        public Transform HitVisual => boss != null ? boss.transform : null;
        Sprite square;
        SpriteRenderer armor, eye, hazard, leftEdge, rightEdge, transformed, spiritWarning;
        SpriteRenderer[] playerSprites;
        bool[] spriteEnabled;
        bool hidingPlayer;

        public void Initialize(TrainingGame controller)
        {
            game = controller;
            square = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height), new Vector2(.5f, .5f), Texture2D.whiteTexture.width);
            root = new GameObject("Calamity arena");
            root.transform.SetParent(transform, false);
            Box("Sky", root.transform, new Vector3(0, 0, 2), new Vector2(24, 14), new Color(.065f, .045f, .10f), -30);
            Box("Ground", root.transform, new Vector3(0, PlayerMotor.GroundY - .3f, 1), new Vector2(22, .4f), new Color(.23f, .18f, .26f), -20);
            for (int i = 0; i < 7; i++)
            {
                var shard = Box("Time fracture", root.transform, new Vector3(-9 + 3 * i, 1 + i % 3, 1), new Vector2(.1f, 2), new Color(.34f, .22f, .44f), -25);
                shard.transform.localRotation = Quaternion.Euler(0, 0, i % 2 == 0 ? 24 : -24);
            }
            boss = new GameObject("Dark knight");
            boss.transform.SetParent(root.transform, false);
            armor = Box("Armor", boss.transform, new Vector3(0, 1, 0), new Vector2(.9f, 1.65f), Color.white, 20);
            Box("Helm", boss.transform, new Vector3(0, 2, 0), new Vector2(.6f, .5f), new Color(.12f, .10f, .16f), 21);
            eye = Box("Visor", boss.transform, new Vector3(0, 2.02f, 0), new Vector2(.43f, .08f), Color.white, 22);
            Box("Blade", boss.transform, new Vector3(-.7f, 1.1f, 0), new Vector2(.13f, 2.05f), new Color(.7f, .64f, .77f), 21);
            hazard = Box("Hazard warning", root.transform, Vector3.zero, Vector2.one, Color.red, 15);
            leftEdge = Box("Hazard left edge", root.transform, Vector3.zero, Vector2.one, Color.red, 16);
            rightEdge = Box("Hazard right edge", root.transform, Vector3.zero, Vector2.one, Color.red, 16);
            transformed = Box("Spirit form", root.transform, Vector3.zero, new Vector2(.55f, .7f), Color.white, 30);
            spiritWarning = Box("Transformation warning", root.transform, Vector3.zero, new Vector2(1.1f, .12f), Color.white, 31);
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
            boss.SetActive(true);
            playerSprites = game.player.GetComponentsInChildren<SpriteRenderer>(true);
            spriteEnabled = new bool[playerSprites.Length];
            Refresh();
        }
        public void Hide()
        {
            RestorePlayer();
            if (root != null) root.SetActive(false);
        }
        public void HideBoss() { boss.SetActive(false); RestorePlayer(); }
        void LateUpdate() { if (root != null && root.activeSelf) Refresh(); }

        void Refresh()
        {
            var fight = game.Calamity;
            var data = fight.Definition;
            boss.transform.position = game.enemy.transform.position;
            armor.color = Color.Lerp(data.armorColor, new Color(.7f, .18f, .26f), (float)fight.Rage / data.maximumRage);
            eye.color = fight.Phase == 3 ? new Color(1, .3f, .2f) : data.runeColor;
            transformed.gameObject.SetActive(fight.Transformed);
            spiritWarning.gameObject.SetActive(fight.SpiritWarningRemaining > 0);
            spiritWarning.transform.position = game.player.transform.position + Vector3.up * 2.2f;
            spiritWarning.color = data.spiritColor;
            if (fight.Transformed)
            {
                if (!hidingPlayer)
                {
                    for (int i = 0; i < playerSprites.Length; i++)
                        if (playerSprites[i] != null) { spriteEnabled[i] = playerSprites[i].enabled; playerSprites[i].enabled = false; }
                    hidingPlayer = true;
                }
                transformed.transform.position = game.player.transform.position + Vector3.up;
                transformed.color = data.spiritColor;
            }
            else RestorePlayer();
            hazard.gameObject.SetActive(fight.HazardActive);
            leftEdge.gameObject.SetActive(fight.HazardActive);
            rightEdge.gameObject.SetActive(fight.HazardActive);
            if (!fight.HazardActive) return;
            var center = new Vector3(fight.HazardX, PlayerMotor.GroundY + 2.5f, 0);
            hazard.transform.position = center;
            hazard.transform.localScale = new Vector3(fight.HazardRadius * 2, 5, 1);
            hazard.color = new Color(1, .2f, .22f, Mathf.Lerp(.18f, .5f, 1 - Mathf.Clamp01(fight.HazardRemaining / data.hazardWarning)));
            leftEdge.transform.position = center + Vector3.left * fight.HazardRadius;
            rightEdge.transform.position = center + Vector3.right * fight.HazardRadius;
            leftEdge.transform.localScale = rightEdge.transform.localScale = new Vector3(.06f, 5, 1);
        }

        void RestorePlayer()
        {
            if (!hidingPlayer) return;
            for (int i = 0; i < playerSprites.Length; i++) if (playerSprites[i] != null) playerSprites[i].enabled = spriteEnabled[i];
            hidingPlayer = false;
        }
        void OnDestroy() { RestorePlayer(); if (square != null) Destroy(square); }
    }
}
