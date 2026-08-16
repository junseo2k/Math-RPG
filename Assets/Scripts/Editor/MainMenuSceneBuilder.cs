using System.Collections.Generic;
using MathRPG.Core;
using MathRPG.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MathRPG.EditorTools
{
    /// <summary>
    /// 메인 메뉴 씬을 코드로 생성한다.
    ///
    /// 씬을 손으로 조립하지 않고 스크립트로 만드는 이유:
    ///  - 레이아웃을 갈아엎어도 메뉴 한 번으로 재생성된다
    ///  - 참조 연결(버튼 → 패널)이 코드로 남아 있어 누락되지 않는다
    ///  - 생성 결과는 평범한 .unity 에셋이라 이후 에디터에서 자유롭게 수정 가능
    ///
    /// 실행: 메뉴 MathRPG/Build/Main Menu Scene
    /// 주의: 실행하면 기존 MainMenu.unity를 덮어쓴다. 손으로 고친 내용이 있다면 사라진다.
    /// </summary>
    public static class MainMenuSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/" + SceneNames.MainMenu + ".unity";

        private const float ReferenceWidth = 1920f;
        private const float ReferenceHeight = 1080f;

        private static readonly Color ButtonTextOnAccent = new Color(0.05f, 0.08f, 0.12f, 1f);

        [MenuItem("MathRPG/Build/Main Menu Scene", priority = 20)]
        public static void Build()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateCamera();
            CreateEventSystem();
            Transform canvas = CreateCanvas();

            CreateBackground(canvas);
            CreateTitle(canvas);

            // uGUI는 계층 순서대로 그린다 — 뒤에 만든 것이 위에 그려진다.
            // 따라서 메인 버튼을 먼저 만들고 패널을 나중에 만들어야 패널이 버튼을 덮는다.
            Button startButton;
            Button settingsButton;
            Button exitButton;
            GameObject buttonsRoot = BuildMainButtons(canvas, out startButton, out settingsButton, out exitButton);

            SettingsPanel settingsPanel = BuildSettingsPanel(canvas);
            SaveSlotPanel slotPanel = BuildSaveSlotPanel(canvas);

            GameObject controllerGo = UiBuildKit.CreateUiObject("MainMenuController", canvas);
            UiBuildKit.Place(controllerGo, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            var controller = controllerGo.AddComponent<MainMenuController>();
            controller.EditorBind(buttonsRoot, startButton, settingsButton, exitButton, slotPanel, settingsPanel);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);

            RegisterInBuildSettings();

            Debug.Log("[MainMenuSceneBuilder] 메인 메뉴 씬 생성 완료: " + ScenePath);
        }

        // ---------------------------------------------------------------- 기반

        private static void CreateCamera()
        {
            var go = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            go.tag = "MainCamera";

            var camera = go.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = UiBuildKit.Background;
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            go.transform.position = new Vector3(0f, 0f, -10f);
        }

        private static void CreateEventSystem()
        {
            // 이 프로젝트는 Input System 패키지만 쓰므로(activeInputHandler=1)
            // 구형 StandaloneInputModule을 붙이면 UI 클릭이 아예 동작하지 않는다.
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        private static Transform CreateCanvas()
        {
            var go = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            return go.transform;
        }

        private static void CreateBackground(Transform canvas)
        {
            GameObject go = UiBuildKit.CreateUiObject("Background", canvas);
            UiBuildKit.AddImage(go, UiBuildKit.Background, sliced: false);
            UiBuildKit.Stretch(go);
        }

        private static void CreateTitle(Transform canvas)
        {
            TextMeshProUGUI title = UiBuildKit.CreateText("Title", canvas, "수학 전투 RPG", 96f,
                UiBuildKit.TextPrimary, TextAlignmentOptions.Center);
            title.fontStyle = FontStyles.Bold;
            UiBuildKit.Place(title.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(1400f, 130f), new Vector2(0f, -120f));

            TextMeshProUGUI subtitle = UiBuildKit.CreateText("Subtitle", canvas,
                "문제를 풀어 힘을 얻고, 그 힘으로 싸운다", 30f, UiBuildKit.TextMuted, TextAlignmentOptions.Center);
            UiBuildKit.Place(subtitle.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(1400f, 44f), new Vector2(0f, -252f));
        }

        // ---------------------------------------------------------------- 메인 버튼

        private static GameObject BuildMainButtons(Transform canvas, out Button startButton,
                                                   out Button settingsButton, out Button exitButton)
        {
            GameObject root = UiBuildKit.CreateUiObject("MainButtons", canvas);
            UiBuildKit.Place(root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(440f, 320f), new Vector2(0f, -80f));

            var layout = root.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 22f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            TextMeshProUGUI unusedLabel;
            startButton = CreateMenuButton(root.transform, "StartButton", "Start", UiBuildKit.Accent,
                ButtonTextOnAccent, out unusedLabel);
            settingsButton = CreateMenuButton(root.transform, "SettingsButton", "Setting", UiBuildKit.PanelRaised,
                UiBuildKit.TextPrimary, out unusedLabel);
            exitButton = CreateMenuButton(root.transform, "ExitButton", "Exit", UiBuildKit.PanelRaised,
                UiBuildKit.TextPrimary, out unusedLabel);

            return root;
        }

        private static Button CreateMenuButton(Transform parent, string name, string label,
                                               Color background, Color labelColor, out TextMeshProUGUI labelText)
        {
            Button button = UiBuildKit.CreateButton(name, parent, label, 38f, background, labelColor, out labelText);
            labelText.fontStyle = FontStyles.Bold;

            var element = button.gameObject.AddComponent<LayoutElement>();
            element.preferredHeight = 84f;
            element.preferredWidth = 440f;

            return button;
        }

        // ---------------------------------------------------------------- 세이브 슬롯

        private static SaveSlotPanel BuildSaveSlotPanel(Transform canvas)
        {
            GameObject root = UiBuildKit.CreateUiObject("SaveSlotPanel", canvas);
            UiBuildKit.AddImage(root, UiBuildKit.Overlay, sliced: false);
            UiBuildKit.Stretch(root);

            GameObject box = UiBuildKit.CreateUiObject("Box", root.transform);
            UiBuildKit.AddImage(box, UiBuildKit.Panel);
            UiBuildKit.Place(box, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(1180f, 880f), Vector2.zero);

            TextMeshProUGUI header = UiBuildKit.CreateText("Header", box.transform, "게임 시작 — 슬롯 선택", 44f,
                UiBuildKit.TextPrimary, TextAlignmentOptions.Left);
            header.fontStyle = FontStyles.Bold;
            UiBuildKit.Place(header.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(800f, 60f), new Vector2(48f, -40f));

            GameObject list = UiBuildKit.CreateUiObject("SlotList", box.transform);
            UiBuildKit.Place(list, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(1084f, 660f), new Vector2(0f, -120f));

            var layout = list.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 14f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var slotViews = new List<SaveSlotView>(SaveSystem.SlotCount);
            for (int i = 0; i < SaveSystem.SlotCount; i++)
            {
                slotViews.Add(BuildSlotRow(list.transform, i));
            }

            TextMeshProUGUI backLabel;
            Button back = UiBuildKit.CreateButton("BackButton", box.transform, "뒤로", 30f,
                UiBuildKit.PanelRaised, UiBuildKit.TextPrimary, out backLabel);
            UiBuildKit.Place(back.gameObject, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(200f, 68f), new Vector2(48f, 40f));

            ConfirmDialog dialog = BuildConfirmDialog(root.transform);

            var panel = root.AddComponent<SaveSlotPanel>();
            panel.EditorBind(root, slotViews, back, dialog);

            root.SetActive(false);
            return panel;
        }

        private static SaveSlotView BuildSlotRow(Transform parent, int slotIndex)
        {
            GameObject row = UiBuildKit.CreateUiObject("Slot" + slotIndex, parent);
            UiBuildKit.AddImage(row, UiBuildKit.PanelRaised);

            var element = row.AddComponent<LayoutElement>();
            element.preferredHeight = 118f;

            TextMeshProUGUI number = UiBuildKit.CreateText("Number", row.transform, "슬롯 " + (slotIndex + 1), 24f,
                UiBuildKit.Accent, TextAlignmentOptions.Left);
            number.fontStyle = FontStyles.Bold;
            UiBuildKit.Place(number.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(300f, 30f), new Vector2(28f, -14f));

            TextMeshProUGUI summary = UiBuildKit.CreateText("Summary", row.transform, "빈 슬롯", 32f,
                UiBuildKit.TextPrimary, TextAlignmentOptions.Left);
            UiBuildKit.Place(summary.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(660f, 40f), new Vector2(28f, -46f));

            TextMeshProUGUI detail = UiBuildKit.CreateText("Detail", row.transform, "", 20f,
                UiBuildKit.TextMuted, TextAlignmentOptions.Left);
            UiBuildKit.Place(detail.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(660f, 28f), new Vector2(28f, -86f));

            TextMeshProUGUI selectLabel;
            Button select = UiBuildKit.CreateButton("SelectButton", row.transform, "새 게임", 28f,
                UiBuildKit.Accent, new Color(0.05f, 0.08f, 0.12f, 1f), out selectLabel);
            selectLabel.fontStyle = FontStyles.Bold;
            UiBuildKit.Place(select.gameObject, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(190f, 72f), new Vector2(-140f, 0f));

            TextMeshProUGUI deleteLabel;
            Button delete = UiBuildKit.CreateButton("DeleteButton", row.transform, "삭제", 26f,
                UiBuildKit.Danger, UiBuildKit.TextPrimary, out deleteLabel);
            UiBuildKit.Place(delete.gameObject, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(110f, 72f), new Vector2(-20f, 0f));

            var view = row.AddComponent<SaveSlotView>();
            view.EditorSetSlotIndex(slotIndex);
            view.EditorBind(number, summary, detail, select, selectLabel, delete);

            return view;
        }

        private static ConfirmDialog BuildConfirmDialog(Transform parent)
        {
            GameObject root = UiBuildKit.CreateUiObject("ConfirmDialog", parent);
            UiBuildKit.AddImage(root, UiBuildKit.Overlay, sliced: false);
            UiBuildKit.Stretch(root);

            GameObject box = UiBuildKit.CreateUiObject("Box", root.transform);
            UiBuildKit.AddImage(box, UiBuildKit.PanelRaised);
            UiBuildKit.Place(box, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(720f, 320f), Vector2.zero);

            TextMeshProUGUI message = UiBuildKit.CreateText("Message", box.transform, "", 30f,
                UiBuildKit.TextPrimary, TextAlignmentOptions.Center);
            UiBuildKit.Place(message.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(640f, 150f), new Vector2(0f, -40f));

            TextMeshProUGUI confirmLabel;
            Button confirm = UiBuildKit.CreateButton("ConfirmButton", box.transform, "삭제", 28f,
                UiBuildKit.Danger, UiBuildKit.TextPrimary, out confirmLabel);
            UiBuildKit.Place(confirm.gameObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(220f, 68f), new Vector2(-125f, 40f));

            TextMeshProUGUI cancelLabel;
            Button cancel = UiBuildKit.CreateButton("CancelButton", box.transform, "취소", 28f,
                UiBuildKit.Panel, UiBuildKit.TextPrimary, out cancelLabel);
            UiBuildKit.Place(cancel.gameObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(220f, 68f), new Vector2(125f, 40f));

            var dialog = root.AddComponent<ConfirmDialog>();
            BindConfirmDialog(dialog, root, message, confirm, cancel);

            root.SetActive(false);
            return dialog;
        }

        private static void BindConfirmDialog(ConfirmDialog dialog, GameObject root, TextMeshProUGUI message,
                                              Button confirm, Button cancel)
        {
            var serialized = new SerializedObject(dialog);
            serialized.FindProperty("root").objectReferenceValue = root;
            serialized.FindProperty("messageLabel").objectReferenceValue = message;
            serialized.FindProperty("confirmButton").objectReferenceValue = confirm;
            serialized.FindProperty("cancelButton").objectReferenceValue = cancel;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------------------------------------------------------------- 설정

        private static SettingsPanel BuildSettingsPanel(Transform canvas)
        {
            GameObject root = UiBuildKit.CreateUiObject("SettingsPanel", canvas);
            UiBuildKit.AddImage(root, UiBuildKit.Overlay, sliced: false);
            UiBuildKit.Stretch(root);

            GameObject box = UiBuildKit.CreateUiObject("Box", root.transform);
            UiBuildKit.AddImage(box, UiBuildKit.Panel);
            UiBuildKit.Place(box, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(1040f, 760f), Vector2.zero);

            TextMeshProUGUI header = UiBuildKit.CreateText("Header", box.transform, "설정", 44f,
                UiBuildKit.TextPrimary, TextAlignmentOptions.Left);
            header.fontStyle = FontStyles.Bold;
            UiBuildKit.Place(header.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(600f, 60f), new Vector2(48f, -36f));

            CreateSectionLabel(box.transform, "화면", -120f);

            TMP_Dropdown resolutionDropdown = BuildResolutionRow(box.transform, -172f);
            Toggle fullScreenToggle = BuildFullScreenRow(box.transform, -246f);

            CreateSectionLabel(box.transform, "소리", -330f);

            TextMeshProUGUI masterLabel;
            Slider master = BuildVolumeRow(box.transform, "마스터", -382f, out masterLabel);

            TextMeshProUGUI bgmLabel;
            Slider bgm = BuildVolumeRow(box.transform, "배경음(BGM)", -456f, out bgmLabel);

            TextMeshProUGUI sfxLabel;
            Slider sfx = BuildVolumeRow(box.transform, "효과음(SFX)", -530f, out sfxLabel);

            TextMeshProUGUI applyLabel;
            Button apply = UiBuildKit.CreateButton("ApplyButton", box.transform, "적용", 30f,
                UiBuildKit.Accent, new Color(0.05f, 0.08f, 0.12f, 1f), out applyLabel);
            applyLabel.fontStyle = FontStyles.Bold;
            UiBuildKit.Place(apply.gameObject, new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(200f, 68f), new Vector2(-48f, 40f));

            TextMeshProUGUI resetLabel;
            Button reset = UiBuildKit.CreateButton("ResetButton", box.transform, "기본값", 30f,
                UiBuildKit.PanelRaised, UiBuildKit.TextPrimary, out resetLabel);
            UiBuildKit.Place(reset.gameObject, new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(200f, 68f), new Vector2(-268f, 40f));

            TextMeshProUGUI backLabel;
            Button back = UiBuildKit.CreateButton("BackButton", box.transform, "닫기", 30f,
                UiBuildKit.PanelRaised, UiBuildKit.TextPrimary, out backLabel);
            UiBuildKit.Place(back.gameObject, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(200f, 68f), new Vector2(48f, 40f));

            var panel = root.AddComponent<SettingsPanel>();
            panel.EditorBindRoot(root);
            panel.EditorBindScreen(resolutionDropdown, fullScreenToggle);
            panel.EditorBindAudio(master, masterLabel, bgm, bgmLabel, sfx, sfxLabel);
            panel.EditorBindButtons(apply, reset, back);

            root.SetActive(false);
            return panel;
        }

        private static void CreateSectionLabel(Transform parent, string text, float y)
        {
            TextMeshProUGUI label = UiBuildKit.CreateText("Section_" + text, parent, text, 26f,
                UiBuildKit.Accent, TextAlignmentOptions.Left);
            label.fontStyle = FontStyles.Bold;
            UiBuildKit.Place(label.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(400f, 36f), new Vector2(48f, y));
        }

        private static void CreateRowLabel(Transform parent, string text, float y)
        {
            TextMeshProUGUI label = UiBuildKit.CreateText("Label_" + text, parent, text, 28f,
                UiBuildKit.TextPrimary, TextAlignmentOptions.Left);
            UiBuildKit.Place(label.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(340f, 44f), new Vector2(64f, y));
        }

        private static TMP_Dropdown BuildResolutionRow(Transform parent, float y)
        {
            CreateRowLabel(parent, "해상도", y);

            TMP_Dropdown dropdown = UiBuildKit.CreateDropdown("ResolutionDropdown", parent, 26f,
                UiBuildKit.PanelRaised, UiBuildKit.TextPrimary, UiBuildKit.Accent);
            UiBuildKit.Place(dropdown.gameObject, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(420f, 56f), new Vector2(-48f, y));

            return dropdown;
        }

        private static Toggle BuildFullScreenRow(Transform parent, float y)
        {
            CreateRowLabel(parent, "전체화면", y);

            Toggle toggle = UiBuildKit.CreateToggle("FullScreenToggle", parent,
                UiBuildKit.PanelRaised, UiBuildKit.Accent);
            UiBuildKit.Place(toggle.gameObject, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(420f, 48f), new Vector2(-48f, y));

            return toggle;
        }

        private static Slider BuildVolumeRow(Transform parent, string label, float y, out TextMeshProUGUI valueLabel)
        {
            CreateRowLabel(parent, label, y);

            Slider slider = UiBuildKit.CreateSlider("Slider_" + label, parent,
                UiBuildKit.Panel, UiBuildKit.Accent, UiBuildKit.TextPrimary);
            UiBuildKit.Place(slider.gameObject, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(420f, 40f), new Vector2(-170f, y - 10f));

            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
            slider.value = 1f;

            valueLabel = UiBuildKit.CreateText("Value_" + label, parent, "100%", 26f,
                UiBuildKit.TextMuted, TextAlignmentOptions.Right);
            UiBuildKit.Place(valueLabel.gameObject, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(110f, 40f), new Vector2(-48f, y));

            return slider;
        }

        // ---------------------------------------------------------------- 빌드 설정

        /// <summary>MainMenu를 첫 씬(인덱스 0)으로 등록한다.</summary>
        private static void RegisterInBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            scenes.RemoveAll(s => s.path == ScenePath);
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
