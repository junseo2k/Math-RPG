using System.Collections.Generic;
using System.IO;
using MathRPG.Core;
using UnityEditor;
using UnityEngine;

namespace MathRPG.EditorTools
{
    /// <summary>
    /// 캔버스를 RenderTexture로 직접 렌더링해 PNG로 저장한다.
    /// UI 레이아웃과 한글 폰트가 실제로 어떻게 그려지는지 확인하는 용도.
    ///
    /// Game 뷰 캡처(ScreenCapture)는 Game 창이 실제로 다시 그려질 때만 파일을 남겨서
    /// 자동화에 쓰기 어렵다. 여기서는 임시 카메라로 직접 렌더링하므로
    /// 플레이 모드가 아니어도, Game 창이 보이지 않아도 동작한다.
    ///
    /// 저장 위치는 Temp 폴더라 저장소에 들어가지 않는다.
    /// </summary>
    public static class GameViewCapture
    {
        private const string OutputDirectory = "Temp/Captures";
        private const int CaptureWidth = 1600;
        private const int CaptureHeight = 900;

        [MenuItem("MathRPG/Diagnostics/Capture Main Menu Screens", priority = 92)]
        public static void CaptureMainMenuScreens()
        {
            Canvas canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas == null)
            {
                Debug.LogError("[GameViewCapture] 씬에서 Canvas를 찾지 못했습니다. MainMenu 씬을 열어두세요.");
                return;
            }

            Directory.CreateDirectory(OutputDirectory);

            GameObject slotPanel = FindChild(canvas.transform, "SaveSlotPanel");
            GameObject settingsPanel = FindChild(canvas.transform, "SettingsPanel");
            GameObject confirmDialog = slotPanel != null ? FindChild(slotPanel.transform, "ConfirmDialog") : null;

            // 원래 활성 상태를 기억했다가 끝나면 되돌린다.
            var restore = new Dictionary<GameObject, bool>();
            Remember(restore, slotPanel);
            Remember(restore, settingsPanel);
            Remember(restore, confirmDialog);

            try
            {
                SetActive(slotPanel, false);
                SetActive(settingsPanel, false);
                Capture(canvas, Path.Combine(OutputDirectory, "01_MainMenu.png"));

                SetActive(slotPanel, true);
                SetActive(confirmDialog, false);
                Capture(canvas, Path.Combine(OutputDirectory, "02_SaveSlots.png"));

                SetActive(confirmDialog, true);
                Capture(canvas, Path.Combine(OutputDirectory, "03_ConfirmDelete.png"));

                SetActive(confirmDialog, false);
                SetActive(slotPanel, false);
                SetActive(settingsPanel, true);
                Capture(canvas, Path.Combine(OutputDirectory, "04_Settings.png"));
            }
            finally
            {
                foreach (KeyValuePair<GameObject, bool> pair in restore)
                {
                    if (pair.Key != null)
                    {
                        pair.Key.SetActive(pair.Value);
                    }
                }
            }

            Debug.Log("[GameViewCapture] 캡처 완료: " + Path.GetFullPath(OutputDirectory));
        }

        /// <summary>
        /// 플레이 중에 실제 메뉴 흐름을 거치며 캡처한다.
        /// 편집 모드 캡처와 달리 슬롯 목록 갱신·해상도 목록 채우기가 실제로 실행된 결과를 본다.
        ///
        /// 검사용 세이브는 비어 있는 슬롯에만 만들고, 끝나면 반드시 지운다.
        /// </summary>
        [MenuItem("MathRPG/Diagnostics/Capture Runtime Menu Flow", priority = 93)]
        public static void CaptureRuntimeFlow()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[GameViewCapture] 플레이 모드에서 실행하세요. MainMenu 씬을 재생한 뒤 다시 호출합니다.");
                return;
            }

            var controller = Object.FindFirstObjectByType<MathRPG.UI.MainMenuController>(FindObjectsInactive.Include);
            Canvas canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);

            if (controller == null || canvas == null)
            {
                Debug.LogError("[GameViewCapture] MainMenuController 또는 Canvas를 찾지 못했습니다.");
                return;
            }

            Directory.CreateDirectory(OutputDirectory);

            var createdSlots = new List<int>();
            try
            {
                createdSlots.Add(CreateSampleSave(ChapterId.Chapter1, 3, 5400d));
                createdSlots.Add(CreateSampleSave(ChapterId.Chapter3, 12, 190d));
                createdSlots.RemoveAll(slot => slot < 0);

                controller.OpenSaveSlots();
                Capture(canvas, Path.Combine(OutputDirectory, "10_SaveSlots_Runtime.png"));

                controller.OpenSettings();
                Capture(canvas, Path.Combine(OutputDirectory, "11_Settings_Runtime.png"));
            }
            finally
            {
                for (int i = 0; i < createdSlots.Count; i++)
                {
                    SaveSystem.Delete(createdSlots[i]);
                }
                GameSession.End();
            }

            Debug.Log("[GameViewCapture] 런타임 캡처 완료. 검사용 세이브 " + createdSlots.Count + "개는 삭제했습니다.");
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

                Object.DestroyImmediate(texture);
                Object.DestroyImmediate(renderTexture);
                Object.DestroyImmediate(cameraGo);

                canvas.renderMode = originalMode;
                canvas.worldCamera = originalCamera;
                canvas.planeDistance = originalPlaneDistance;
            }
        }

        private static GameObject FindChild(Transform parent, string name)
        {
            Transform found = parent.Find(name);
            return found != null ? found.gameObject : null;
        }

        private static void Remember(Dictionary<GameObject, bool> map, GameObject go)
        {
            if (go != null && !map.ContainsKey(go))
            {
                map.Add(go, go.activeSelf);
            }
        }

        private static void SetActive(GameObject go, bool active)
        {
            if (go != null)
            {
                go.SetActive(active);
            }
        }
    }
}
