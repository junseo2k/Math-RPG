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
    /// 메뉴 씬 3개(메인 메뉴 · 세이브 슬롯 · 설정)를 코드로 생성한다.
    ///
    /// 화면 하나당 씬 하나로 나눈 구조라, 각 화면을 따로 열어 작업할 수 있고
    /// 한 씬이 커져도 다른 화면에 영향을 주지 않는다.
    ///
    /// 씬을 손으로 조립하지 않고 스크립트로 만드는 이유:
    ///  - 레이아웃을 갈아엎어도 메뉴 한 번으로 재생성된다
    ///  - 참조 연결(버튼 → 컨트롤러)이 코드로 남아 있어 누락되지 않는다
    ///  - 생성 결과는 평범한 .unity 에셋이라 이후 에디터에서 자유롭게 수정 가능
    ///
    /// 실행: 메뉴 MathRPG/Build/All Menu Scenes
    /// 주의: 실행하면 기존 씬 파일을 덮어쓴다. 손으로 고친 내용이 있다면 사라진다.
    /// </summary>
    public static class MenuScenesBuilder
    {
        private const string SceneFolder = "Assets/Scenes/";
        private const string MainMenuScenePath = SceneFolder + SceneNames.MainMenu + ".unity";
        private const string SaveSlotsScenePath = SceneFolder + SceneNames.SaveSlots + ".unity";
        private const string SettingsScenePath = SceneFolder + SceneNames.Settings + ".unity";
        private const string CombatSandboxScenePath = SceneFolder + SceneNames.CombatSandbox + ".unity";

        private const float ReferenceWidth = 1920f;
        private const float ReferenceHeight = 1080f;

        private static readonly Color TextOnAccent = new Color(0.05f, 0.08f, 0.12f, 1f);

        [MenuItem("MathRPG/Build/All Menu Scenes", priority = 20)]
        public static void BuildAll()
        {
            BuildMainMenuScene();
            BuildSaveSlotsScene();
            BuildSettingsScene();

            RegisterBuildSettings();

            Debug.Log("[MenuScenesBuilder] 메뉴 씬 3개 생성 완료:\n" +
                      "  " + MainMenuScenePath + "\n" +
                      "  " + SaveSlotsScenePath + "\n" +
                      "  " + SettingsScenePath);
        }

        // ---------------------------------------------------------------- 씬별 구성

        private static void BuildMainMenuScene()
        {
            Scene scene = NewSceneWithShell(out Transform canvas);

            TextMeshProUGUI title = UiBuildKit.CreateText("Title", canvas, "수학 전투 RPG", 96f,
                UiBuildKit.TextPrimary, TextAlignmentOptions.Center);
            title.fontStyle = FontStyles.Bold;
            UiBuildKit.Place(title.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(1400f, 130f), new Vector2(0f, -120f));

            TextMeshProUGUI subtitle = UiBuildKit.CreateText("Subtitle", canvas,
                "문제를 풀어 힘을 얻고, 그 힘으로 싸운다", 30f, UiBuildKit.TextMuted, TextAlignmentOptions.Center);
            UiBuildKit.Place(subtitle.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(1400f, 44f), new Vector2(0f, -252f));

            GameObject buttons = UiBuildKit.CreateUiObject("MainButtons", canvas);
            UiBuildKit.Place(buttons, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(440f, 320f), new Vector2(0f, -80f));

            var layout = buttons.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 22f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            TextMeshProUGUI unused;
            Button start = CreateMenuButton(buttons.transform, "StartButton", "Start",
                UiBuildKit.Accent, TextOnAccent, out unused);
            Button settings = CreateMenuButton(buttons.transform, "SettingsButton", "Setting",
                UiBuildKit.PanelRaised, UiBuildKit.TextPrimary, out unused);
            Button exit = CreateMenuButton(buttons.transform, "ExitButton", "Exit",
                UiBuildKit.PanelRaised, UiBuildKit.TextPrimary, out unused);

            var controller = CreateControllerObject<MainMenuController>("MainMenuController", canvas);
            controller.EditorBind(start, settings, exit);

            SaveScene(scene, MainMenuScenePath);
        }

        private static void BuildSaveSlotsScene()
        {
            Scene scene = NewSceneWithShell(out Transform canvas);

            CreateScreenHeader(canvas, "게임 시작 — 슬롯 선택");

            GameObject list = UiBuildKit.CreateUiObject("SlotList", canvas);
            UiBuildKit.Place(list, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(1280f, 660f), new Vector2(0f, -210f));

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

            Button back = CreateScreenBackButton(canvas, "뒤로");

            // 확인 창은 슬롯 목록보다 뒤에 만들어야 위에 그려진다.
            ConfirmDialog dialog = BuildConfirmDialog(canvas);

            var screen = CreateControllerObject<SaveSlotScreen>("SaveSlotScreen", canvas);
            screen.EditorBind(slotViews, back, dialog);

            SaveScene(scene, SaveSlotsScenePath);
        }

        private static void BuildSettingsScene()
        {
            Scene scene = NewSceneWithShell(out Transform canvas);

            CreateScreenHeader(canvas, "설정");

            GameObject box = UiBuildKit.CreateUiObject("Box", canvas);
            UiBuildKit.AddImage(box, UiBuildKit.Panel);
            UiBuildKit.Place(box, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(1040f, 700f), new Vector2(0f, -30f));

            CreateSectionLabel(box.transform, "화면", -40f);
            TMP_Dropdown resolutionDropdown = BuildResolutionRow(box.transform, -92f);
            Toggle fullScreenToggle = BuildFullScreenRow(box.transform, -166f);

            CreateSectionLabel(box.transform, "소리", -250f);

            TextMeshProUGUI masterLabel;
            Slider master = BuildVolumeRow(box.transform, "마스터", -302f, out masterLabel);

            TextMeshProUGUI bgmLabel;
            Slider bgm = BuildVolumeRow(box.transform, "배경음(BGM)", -376f, out bgmLabel);

            TextMeshProUGUI sfxLabel;
            Slider sfx = BuildVolumeRow(box.transform, "효과음(SFX)", -450f, out sfxLabel);

            TextMeshProUGUI applyLabel;
            Button apply = UiBuildKit.CreateButton("ApplyButton", box.transform, "적용", 30f,
                UiBuildKit.Accent, TextOnAccent, out applyLabel);
            applyLabel.fontStyle = FontStyles.Bold;
            UiBuildKit.Place(apply.gameObject, new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(200f, 68f), new Vector2(-48f, 40f));

            TextMeshProUGUI resetLabel;
            Button reset = UiBuildKit.CreateButton("ResetButton", box.transform, "기본값", 30f,
                UiBuildKit.PanelRaised, UiBuildKit.TextPrimary, out resetLabel);
            UiBuildKit.Place(reset.gameObject, new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(200f, 68f), new Vector2(-268f, 40f));

            Button back = CreateScreenBackButton(canvas, "닫기");

            var screen = CreateControllerObject<SettingsScreen>("SettingsScreen", canvas);
            screen.EditorBindScreen(resolutionDropdown, fullScreenToggle);
            screen.EditorBindAudio(master, masterLabel, bgm, bgmLabel, sfx, sfxLabel);
            screen.EditorBindButtons(apply, reset, back);

            SaveScene(scene, SettingsScenePath);
        }

        // ---------------------------------------------------------------- 공통 뼈대

        /// <summary>카메라 · EventSystem · 캔버스 · 배경까지 갖춘 빈 씬을 만든다.</summary>
        private static Scene NewSceneWithShell(out Transform canvas)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateCamera();
            CreateEventSystem();
            canvas = CreateCanvas();
            CreateBackground(canvas);

            return scene;
        }

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

        /// <summary>화면 좌상단 제목.</summary>
        private static void CreateScreenHeader(Transform canvas, string text)
        {
            TextMeshProUGUI header = UiBuildKit.CreateText("Header", canvas, text, 52f,
                UiBuildKit.TextPrimary, TextAlignmentOptions.Left);
            header.fontStyle = FontStyles.Bold;
            UiBuildKit.Place(header.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(1200f, 70f), new Vector2(320f, -90f));
        }

        /// <summary>화면 좌하단 뒤로 가기 버튼.</summary>
        private static Button CreateScreenBackButton(Transform canvas, string label)
        {
            TextMeshProUGUI unused;
            Button back = UiBuildKit.CreateButton("BackButton", canvas, label, 30f,
                UiBuildKit.PanelRaised, UiBuildKit.TextPrimary, out unused);
            UiBuildKit.Place(back.gameObject, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(220f, 72f), new Vector2(320f, 80f));
            return back;
        }

        /// <summary>화면 컨트롤러를 담을 빈 오브젝트를 만든다.</summary>
        private static T CreateControllerObject<T>(string name, Transform canvas) where T : Component
        {
            GameObject go = UiBuildKit.CreateUiObject(name, canvas);
            UiBuildKit.Place(go, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            return go.AddComponent<T>();
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

        // ---------------------------------------------------------------- 세이브 슬롯 조각

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
                new Vector2(820f, 40f), new Vector2(28f, -46f));

            TextMeshProUGUI detail = UiBuildKit.CreateText("Detail", row.transform, string.Empty, 20f,
                UiBuildKit.TextMuted, TextAlignmentOptions.Left);
            UiBuildKit.Place(detail.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(820f, 28f), new Vector2(28f, -86f));

            TextMeshProUGUI selectLabel;
            Button select = UiBuildKit.CreateButton("SelectButton", row.transform, "새 게임", 28f,
                UiBuildKit.Accent, TextOnAccent, out selectLabel);
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

        private static ConfirmDialog BuildConfirmDialog(Transform canvas)
        {
            GameObject root = UiBuildKit.CreateUiObject("ConfirmDialog", canvas);
            UiBuildKit.AddImage(root, UiBuildKit.Overlay, sliced: false);
            UiBuildKit.Stretch(root);

            GameObject box = UiBuildKit.CreateUiObject("Box", root.transform);
            UiBuildKit.AddImage(box, UiBuildKit.PanelRaised);
            UiBuildKit.Place(box, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(720f, 320f), Vector2.zero);

            TextMeshProUGUI message = UiBuildKit.CreateText("Message", box.transform, string.Empty, 30f,
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

            var serialized = new SerializedObject(dialog);
            serialized.FindProperty("root").objectReferenceValue = root;
            serialized.FindProperty("messageLabel").objectReferenceValue = message;
            serialized.FindProperty("confirmButton").objectReferenceValue = confirm;
            serialized.FindProperty("cancelButton").objectReferenceValue = cancel;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            root.SetActive(false);
            return dialog;
        }

        // ---------------------------------------------------------------- 설정 조각

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

        // ---------------------------------------------------------------- 저장 · 빌드 설정

        private static void SaveScene(Scene scene, string path)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, path);
        }

        /// <summary>메뉴 씬들을 앞쪽에 두고, 기존에 등록된 다른 씬은 뒤에 유지한다.</summary>
        private static void RegisterBuildSettings()
        {
            string[] ordered =
            {
                MainMenuScenePath,
                SaveSlotsScenePath,
                SettingsScenePath,
                CombatSandboxScenePath
            };

            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

            // 먼저 순서를 강제할 씬들을 목록에서 빼낸다.
            var others = scenes.FindAll(s => System.Array.IndexOf(ordered, s.path) < 0);

            var result = new List<EditorBuildSettingsScene>(ordered.Length + others.Count);
            for (int i = 0; i < ordered.Length; i++)
            {
                if (!System.IO.File.Exists(ordered[i]))
                {
                    continue;
                }

                result.Add(new EditorBuildSettingsScene(ordered[i], true));
            }

            result.AddRange(others);
            EditorBuildSettings.scenes = result.ToArray();
        }
    }
}
