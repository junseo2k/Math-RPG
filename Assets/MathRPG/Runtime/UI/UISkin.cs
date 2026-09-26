using UnityEngine;
using UnityEngine.UI;

namespace MathRPG
{
    internal enum UIControlStyle { Long, Block }

    internal sealed class UISkin
    {
        internal static readonly Color BodyText = new Color(1f, .94f, .81f);
        internal static readonly Color MutedText = new Color(.88f, .79f, .64f);
        internal static readonly Color AccentText = new Color(1f, .86f, .57f);
        internal static readonly Color ButtonText = new Color(.23f, .15f, .09f);
        internal static readonly Color PanelFallback = new Color(.39f, .27f, .17f);
        internal static readonly Color ButtonFallback = new Color(.79f, .71f, .49f);

        readonly Sprite panel, inset, longButton, longPressed, eliminatedButton, square, squarePressed;
        internal readonly Sprite Check, Cross;

        internal UISkin()
        {
            panel = Load("panel_brown", true);
            inset = Load("panelInset_brown", true);
            longButton = Load("buttonLong_beige", true);
            longPressed = Load("buttonLong_beige_pressed", true);
            eliminatedButton = Load("buttonLong_grey", true);
            square = Load("buttonSquare_beige", true);
            squarePressed = Load("buttonSquare_beige_pressed", true);
            Check = Load("iconCheck_beige", false);
            Cross = Load("iconCross_beige", false);
        }

        static Sprite Load(string name, bool sliced)
        {
            var sprite = Resources.Load<Sprite>("MathRPG/UI/" + name);
            if (sprite == null)
                Debug.LogWarning("UI sprite missing: MathRPG/UI/" + name + ". Import the PNG as Sprite (2D and UI), Single.");
            else if (sliced && sprite.border == Vector4.zero)
                Debug.LogWarning("UI sprite needs a 9-slice Border in Sprite Editor: " + name);
            return sprite;
        }

        internal void ApplyPanel(Image image, bool recessed)
        {
            ApplyImage(image, recessed ? inset : panel, PanelFallback);
        }

        internal void ApplyButton(Button button, UIControlStyle style, bool primary, bool latched, bool eliminated)
        {
            bool block = style == UIControlStyle.Block;
            var normal = block ? square : longButton;
            var pressed = block ? squarePressed : longPressed;
            var idle = eliminated ? eliminatedButton : latched ? pressed : normal;
            var image = (Image)button.targetGraphic;
            ApplyImage(image, idle, ButtonFallback);
            if (idle != null && pressed != null)
            {
                button.transition = Selectable.Transition.SpriteSwap;
                button.spriteState = new SpriteState
                {
                    highlightedSprite = idle,
                    selectedSprite = idle,
                    pressedSprite = eliminated ? eliminatedButton : pressed,
                    disabledSprite = eliminated ? eliminatedButton : idle
                };
                image.color = !button.interactable && !eliminated ? new Color(.76f, .71f, .62f) : Color.white;
            }
            else
            {
                button.transition = Selectable.Transition.ColorTint;
                var colors = ColorBlock.defaultColorBlock;
                colors.pressedColor = new Color(.8f, .71f, .52f);
                colors.disabledColor = new Color(.7f, .65f, .57f);
                button.colors = colors;
            }
            if (primary)
            {
                var outline = image.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(.96f, .8f, .43f, .75f);
                outline.effectDistance = new Vector2(1, -1);
                outline.useGraphicAlpha = true;
            }
        }

        static void ApplyImage(Image image, Sprite sprite, Color fallback)
        {
            image.sprite = sprite;
            image.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            image.fillCenter = true;
            image.color = sprite != null ? Color.white : fallback;
        }
    }
}
