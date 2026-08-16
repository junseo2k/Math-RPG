using System;
using System.Collections.Generic;
using System.IO;
using MathRPG.Core;
using MathRPG.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MathRPG.EditorTools
{
    /// <summary>
    /// 메뉴 씬들을 차례로 열어 캔버스를 PNG로 저장한다.
    /// UI 레이아웃과 한글 폰트가 실제로 어떻게 그려지는지 확인하는 용도.
    ///
    /// Game 뷰 캡처(ScreenCapture)는 Game 창이 실제로 다시 그려질 때만 파일을 남겨서
    /// 자동화에 쓰기 어렵다. 여기서는 임시 카메라로 직접 렌더링하므로
    /// 플레이 모드가 아니어도, Game 창이 보이지 않아도 동작한다.
    ///
    /// 검사용 세이브를 잠시 만들어 슬롯 표시를 확인하고, 끝나면 반드시 지운다.
    /// 저장 위치는 Temp 폴더라 저장소에 들어가지 않는다.
    /// </summary>
    public static class GameViewCapture
    {
        private const string OutputDirectory = "Temp/Captures";
        private const string SceneFolder = "Assets/Scenes/";
        private const int CaptureWidth = 1600;
        private const int CaptureHeight = 900;

        [MenuItem("MathRPG/Diagnostics/Capture Menu Screens", priority = 92)]
        public static void CaptureMenuScreens()
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("[GameViewCapture] 편집 모드에서 실행하세요. 씬을 직접 열어 캡처합니다.");
                return;
            }

            Directory.CreateDirectory(OutputDirectory);

            var createdSlots = new List<int>();
            try
            {
                AddIfValid(createdSlots, CreateSampleSave(ChapterId.Chapter1, 3, 5400d));
                AddIfValid(createdSlots, CreateSampleSave(ChapterId.Chapter3, 12, 190d));

                OpenAndCapture(SceneNames.MainMenu, "01_MainMenu.png", null);
                OpenAndCapture(SceneNames.SaveSlots, "02_SaveSlots.png", PrepareSaveSlots);
                OpenAndCapture(SceneNames.SaveSlots, "03_ConfirmDelete.png", PrepareConfirmDialog);
                OpenAndCapture(SceneNames.Settings, "04_Settings.png", PrepareSettings);
            }
            finally
            {
                for (int i = 0; i < createdSlots.Count; i++)
                {
                    SaveSystem.Delete(createdSlots[i]);
                }

                GameSession.End();

                // 캡처 중 씬을 건드렸으므로 저장하지 않고 메인 메뉴를 다시 연다.
                EditorSceneManager.OpenScene(ScenePath(SceneNames.MainMenu), OpenSceneMode.Single);
            }

            Debug.Log("[GameViewCapture] 캡처 완료: " + Path.GetFullPath(OutputDirectory) +
                      " (검사용 세이브 " + createdSlots.Count + "개는 삭제했습니다)");
        }

        private static string ScenePath(string sceneName)
        {
            return SceneFolder + sceneName + ".unity";
        }

        /// <summary>씬을 열고, 준비 작업을 한 뒤 캡처한다. 씬은 저장하지 않는다.</summary>
        private static void OpenAndCapture(string sceneName, string fileName, Action prepare)
        {
            string path = ScenePath(sceneName);
            if (!File.Exists(path))
            {
                Debug.LogWarning("[GameViewCapture] 씬이 없어 건너뜁니다: " + path);
                return;
            }

            EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

            if (prepare != null)
            {
                prepare.Invoke();
            }

            Canvas canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas == null)
            {
                Debug.LogWarning("[GameViewCapture] " + sceneName + " 씬에서 Canvas를 찾지 못했습니다.");
                return;
            }

            Capture(canvas, Path.Combine(OutputDirectory, fileName));
        }

        // ---------------------------------------------------------------- 화면별 준비

        /// <summary>편집 모드에서도 슬롯 표시를 실제 세이브 내용으로 채운다.</summary>
        private static void PrepareSaveSlots()
        {
            var screen = UnityEngine.Object.FindFirstObjectByType<SaveSlotScreen>(FindObjectsInactive.Include);
            if (screen != null)
            {
                screen.RefreshAll();
            }
        }

        private static void PrepareConfirmDialog()
        {
            PrepareSaveSlots();

            // 확인 창은 평소 비활성이라 GameObject.Find로는 찾을 수 없다. 컴포넌트로 직접 찾는다.
            var dialogComponent = UnityEngine.Object.FindFirstObjectByType<ConfirmDialog>(FindObjectsInactive.Include);
            if (dialogComponent == null)
            {
                Debug.LogWarning("[GameViewCapture] ConfirmDialog를 찾지 못했습니다.");
                return;
            }

            GameObject dialog = dialogComponent.gameObject;
            dialog.SetActive(true);

            Transform message = dialog.transform.Find("Box/Message");
            if (message != null)
            {
                var label = message.GetComponent<TextMeshProUGUI>();
                if (label != null)
                {
                    label.text = "슬롯 1의 저장 데이터를 삭제할까요?\n되돌릴 수 없습니다.";
                }
            }
        }

        private static void PrepareSettings()
        {
            var screen = UnityEngine.Object.FindFirstObjectByType<SettingsScreen>(FindObjectsInactive.Include);
            if (screen != null)
            {
                screen.RefreshFromSettings();
            }
        }

        // ---------------------------------------------------------------- 검사용 세이브

        private static void AddIfValid(List<int> slots, int slot)
        {
            if (slot >= 0)
            {
                slots.Add(slot);
            }
        }

        /// <summary>비어 있는 슬롯에 예시 세이브를 만든다. 빈 슬롯이 없으면 -1.</summary>
        private static int CreateSampleSave(ChapterId chapter, int nodeIndex, double playTimeSeconds)
        {
            for (int slot = 0; slot < SaveSystem.SlotCount; slot++)
            {
                if (SaveSystem.Exists(slot))
                {
                    continue;
                }

                SaveData data = SaveData.CreateNew();
                data.ChapterId = chapter;
                data.nodeIndex = nodeIndex;
                data.playTimeSeconds = playTimeSeconds;

                return SaveSystem.Save(slot, data) ? slot : -1;
            }

            return -1;
        }

        // ---------------------------------------------------------------- 렌더링

        private static void Capture(Canvas canvas, string outputPath)
        {
            RenderMode originalMode = canvas.renderMode;
            Camera originalCamera = canvas.worldCamera;
            float originalPlaneDistance = canvas.planeDistance;

            var cameraGo = new GameObject("~CaptureCamera");
            var camera = cameraGo.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = UiBuildKit.Background;
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 200f;
            cameraGo.transform.position = new Vector3(0f, 0f, -10f);

            // Screen Space - Overlay 캔버스는 카메라로 찍히지 않으므로 잠시 카메라 모드로 바꾼다.
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 10f;

            Canvas.ForceUpdateCanvases();

            var renderTexture = new RenderTexture(CaptureWidth, CaptureHeight, 24, RenderTextureFormat.ARGB32);
            var texture = new Texture2D(CaptureWidth, CaptureHeight, TextureFormat.RGB24, false);
            RenderTexture previousActive = RenderTexture.active;

            try
            {
                camera.targetTexture = renderTexture;
                camera.Render();

                RenderTexture.active = renderTexture;
                texture.ReadPixels(new Rect(0f, 0f, CaptureWidth, CaptureHeight), 0, 0);
                texture.Apply();

                File.WriteAllBytes(outputPath, texture.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previousActive;
                camera.targetTexture = null;

                UnityEngine.Object.DestroyImmediate(texture);
                UnityEngine.Object.DestroyImmediate(renderTexture);
                UnityEngine.Object.DestroyImmediate(cameraGo);

                canvas.renderMode = originalMode;
                canvas.worldCamera = originalCamera;
                canvas.planeDistance = originalPlaneDistance;
            }
        }
    }
}
