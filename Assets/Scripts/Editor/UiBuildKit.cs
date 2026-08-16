using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MathRPG.EditorTools
{
    /// <summary>
    /// 에디터에서 uGUI 계층을 코드로 조립할 때 쓰는 도우미 모음.
    /// 씬 빌더들이 공유한다.
    /// </summary>
    public static class UiBuildKit
    {
        // 공통 팔레트 — 화면 전체의 톤을 여기 한 곳에서 바꾼다.
        public static readonly Color Background = new Color(0.06f, 0.07f, 0.10f, 1f);
        public static readonly Color Overlay = new Color(0.02f, 0.03f, 0.05f, 0.86f);
        public static readonly Color Panel = new Color(0.12f, 0.14f, 0.18f, 1f);
        public static readonly Color PanelRaised = new Color(0.17f, 0.19f, 0.24f, 1f);
        public static readonly Color Accent = new Color(0.36f, 0.72f, 1f, 1f);
        public static readonly Color Danger = new Color(0.85f, 0.35f, 0.38f, 1f);
        public static readonly Color TextPrimary = new Color(0.93f, 0.95f, 0.97f, 1f);
        public static readonly Color TextMuted = new Color(0.58f, 0.62f, 0.69f, 1f);

        public static Sprite DefaultUiSprite
        {
            get { return AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"); }
        }

        public static GameObject CreateUiObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        /// <summary>부모를 가득 채우도록 늘린다.</summary>
        public static RectTransform Stretch(GameObject go, float left = 0f, float top = 0f, float right = 0f, float bottom = 0f)
        {
            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
            return rect;
        }

        /// <summary>앵커·피벗·크기·위치를 한 번에 지정한다.</summary>
        public static RectTransform Place(GameObject go, Vector2 anchor, Vector2 pivot, Vector2 size, Vector2 position)
        {
            var rect = (RectTransform)go.transform;
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return rect;
        }

        public static Image AddImage(GameObject go, Color color, bool sliced = true)
        {
            var image = go.AddComponent<Image>();
            image.color = color;

            if (sliced)
            {
                image.sprite = DefaultUiSprite;
                image.type = Image.Type.Sliced;
            }

            return image;
        }

        public static TextMeshProUGUI CreateText(string name, Transform parent, string content, float fontSize,
                                                 Color color, TextAlignmentOptions alignment)
        {
            GameObject go = CreateUiObject(name, parent);
            var text = go.AddComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.Normal;
            return text;
        }

        /// <summary>버튼 + 라벨을 만든다. 라벨은 out으로 돌려준다.</summary>
        public static Button CreateButton(string name, Transform parent, string label, float fontSize,
                                          Color background, Color labelColor, out TextMeshProUGUI labelText)
        {
            GameObject go = CreateUiObject(name, parent);
            Image image = AddImage(go, background);

            var button = go.AddComponent<Button>();
            button.targetGraphic = image;

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
            colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            labelText = CreateText("Label", go.transform, label, fontSize, labelColor, TextAlignmentOptions.Center);
            Stretch(labelText.gameObject, 12f, 4f, 12f, 4f);

            return button;
        }

        // ------------------------------------------------------------------
        // 복합 컨트롤
        //
        // Unity의 "GameObject/UI/..." 생성 메뉴를 쓰지 않고 직접 조립한다.
        // 메뉴 이름은 버전마다 바뀌어서(Unity 6에서는 아예 존재하지 않는 경로도 있다)
        // 빌더가 조용히 깨지는 원인이 된다. 직접 만들면 결과가 항상 같다.
        // ------------------------------------------------------------------

        /// <summary>가로 슬라이더를 만든다. 값 범위는 호출한 쪽에서 설정한다.</summary>
        public static Slider CreateSlider(string name, Transform parent, Color trackColor, Color fillColor, Color handleColor)
        {
            GameObject root = CreateUiObject(name, parent);
            var slider = root.AddComponent<Slider>();

            GameObject background = CreateUiObject("Background", root.transform);
            Image backgroundImage = AddImage(background, trackColor);
            var backgroundRect = (RectTransform)background.transform;
            backgroundRect.anchorMin = new Vector2(0f, 0.3f);
            backgroundRect.anchorMax = new Vector2(1f, 0.7f);
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;
            backgroundImage.raycastTarget = false;

            GameObject fillArea = CreateUiObject("Fill Area", root.transform);
            var fillAreaRect = (RectTransform)fillArea.transform;
            fillAreaRect.anchorMin = new Vector2(0f, 0.3f);
            fillAreaRect.anchorMax = new Vector2(1f, 0.7f);
            fillAreaRect.offsetMin = new Vector2(HandleRadius, 0f);
            fillAreaRect.offsetMax = new Vector2(-HandleRadius, 0f);

            GameObject fill = CreateUiObject("Fill", fillArea.transform);
            Image fillImage = AddImage(fill, fillColor);
            var fillRect = (RectTransform)fill.transform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            fillImage.raycastTarget = false;

            GameObject handleArea = CreateUiObject("Handle Slide Area", root.transform);
            var handleAreaRect = (RectTransform)handleArea.transform;
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.offsetMin = new Vector2(HandleRadius, 0f);
            handleAreaRect.offsetMax = new Vector2(-HandleRadius, 0f);

            GameObject handle = CreateUiObject("Handle", handleArea.transform);
            Image handleImage = AddImage(handle, handleColor);
            var handleRect = (RectTransform)handle.transform;
            handleRect.anchorMin = new Vector2(0f, 0f);
            handleRect.anchorMax = new Vector2(0f, 1f);
            handleRect.pivot = new Vector2(0.5f, 0.5f);
            handleRect.sizeDelta = new Vector2(HandleRadius * 2f, 0f);

            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImage;
            slider.direction = Slider.Direction.LeftToRight;

            return slider;
        }

        /// <summary>체크박스형 토글을 만든다. 부모 사각형의 왼쪽에 상자가 붙는다.</summary>
        public static Toggle CreateToggle(string name, Transform parent, Color boxColor, Color checkColor)
        {
            GameObject root = CreateUiObject(name, parent);
            var toggle = root.AddComponent<Toggle>();

            GameObject box = CreateUiObject("Background", root.transform);
            Image boxImage = AddImage(box, boxColor);
            var boxRect = (RectTransform)box.transform;
            boxRect.anchorMin = new Vector2(0f, 0.5f);
            boxRect.anchorMax = new Vector2(0f, 0.5f);
            boxRect.pivot = new Vector2(0f, 0.5f);
            boxRect.sizeDelta = new Vector2(44f, 44f);
            boxRect.anchoredPosition = Vector2.zero;

            GameObject checkmark = CreateUiObject("Checkmark", box.transform);
            var checkImage = checkmark.AddComponent<Image>();
            checkImage.color = checkColor;
            checkImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd");
            checkImage.raycastTarget = false;
            var checkRect = (RectTransform)checkmark.transform;
            checkRect.anchorMin = new Vector2(0.5f, 0.5f);
            checkRect.anchorMax = new Vector2(0.5f, 0.5f);
            checkRect.pivot = new Vector2(0.5f, 0.5f);
            checkRect.sizeDelta = new Vector2(34f, 34f);
            checkRect.anchoredPosition = Vector2.zero;

            toggle.targetGraphic = boxImage;
            toggle.graphic = checkImage;
            toggle.isOn = true;

            return toggle;
        }

        /// <summary>
        /// TMP 드롭다운을 만든다.
        /// 펼쳐지는 목록(Template)은 비활성 상태로 함께 만들어 두어야 한다 —
        /// TMP_Dropdown이 실행 중에 이걸 복제해서 쓴다.
        /// </summary>
        public static TMP_Dropdown CreateDropdown(string name, Transform parent, float fontSize,
                                                  Color background, Color textColor, Color itemHighlight)
        {
            GameObject root = CreateUiObject(name, parent);
            Image rootImage = AddImage(root, background);
            var dropdown = root.AddComponent<TMP_Dropdown>();
            dropdown.targetGraphic = rootImage;

            TextMeshProUGUI caption = CreateText("Label", root.transform, string.Empty, fontSize, textColor,
                TextAlignmentOptions.Left);
            Stretch(caption.gameObject, 16f, 4f, 44f, 4f);

            GameObject arrow = CreateUiObject("Arrow", root.transform);
            var arrowImage = arrow.AddComponent<Image>();
            arrowImage.color = textColor;
            arrowImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/DropdownArrow.psd");
            arrowImage.raycastTarget = false;
            Place(arrow, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(24f, 24f), new Vector2(-14f, 0f));

            // ---- 펼침 목록 ----
            GameObject template = CreateUiObject("Template", root.transform);
            AddImage(template, background);
            var templateRect = (RectTransform)template.transform;
            templateRect.anchorMin = new Vector2(0f, 0f);
            templateRect.anchorMax = new Vector2(1f, 0f);
            templateRect.pivot = new Vector2(0.5f, 1f);
            templateRect.sizeDelta = new Vector2(0f, DropdownTemplateHeight);
            templateRect.anchoredPosition = new Vector2(0f, 2f);

            var scrollRect = template.AddComponent<ScrollRect>();

            GameObject viewport = CreateUiObject("Viewport", template.transform);
            Image viewportImage = AddImage(viewport, Color.white);
            viewportImage.raycastTarget = true;
            var mask = viewport.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            var viewportRect = (RectTransform)viewport.transform;
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.pivot = new Vector2(0f, 1f);
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;

            GameObject content = CreateUiObject("Content", viewport.transform);
            var contentRect = (RectTransform)content.transform;
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.sizeDelta = new Vector2(0f, DropdownItemHeight);
            contentRect.anchoredPosition = Vector2.zero;

            GameObject item = CreateUiObject("Item", content.transform);
            var itemToggle = item.AddComponent<Toggle>();
            var itemRect = (RectTransform)item.transform;
            itemRect.anchorMin = new Vector2(0f, 0.5f);
            itemRect.anchorMax = new Vector2(1f, 0.5f);
            itemRect.pivot = new Vector2(0.5f, 0.5f);
            itemRect.sizeDelta = new Vector2(0f, DropdownItemHeight);

            GameObject itemBackground = CreateUiObject("Item Background", item.transform);
            Image itemBackgroundImage = AddImage(itemBackground, background);
            Stretch(itemBackground);

            GameObject itemCheckmark = CreateUiObject("Item Checkmark", item.transform);
            Image itemCheckImage = AddImage(itemCheckmark, itemHighlight, sliced: false);
            Place(itemCheckmark, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(6f, DropdownItemHeight),
                new Vector2(0f, 0f));

            TextMeshProUGUI itemLabel = CreateText("Item Label", item.transform, string.Empty, fontSize, textColor,
                TextAlignmentOptions.Left);
            Stretch(itemLabel.gameObject, 20f, 2f, 12f, 2f);

            itemToggle.targetGraphic = itemBackgroundImage;
            itemToggle.graphic = itemCheckImage;

            scrollRect.content = contentRect;
            scrollRect.viewport = viewportRect;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 24f;
            scrollRect.verticalScrollbar = null;

            dropdown.template = templateRect;
            dropdown.captionText = caption;
            dropdown.itemText = itemLabel;

            template.SetActive(false);

            return dropdown;
        }

        private const float HandleRadius = 14f;
        private const float DropdownItemHeight = 44f;
        private const float DropdownTemplateHeight = 260f;
    }
}
