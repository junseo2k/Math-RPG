using System;
using System.Collections.Generic;
using MathRPG.Core;
using UnityEngine;
using UnityEngine.UI;

namespace MathRPG.UI
{
    /// <summary>
    /// 세이브 슬롯 5칸을 관리한다.
    /// 슬롯 선택 시 비어 있으면 새 게임, 저장되어 있으면 이어하기로 분기한다.
    /// </summary>
    public sealed class SaveSlotPanel : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private List<SaveSlotView> slotViews = new List<SaveSlotView>();
        [SerializeField] private Button backButton;
        [SerializeField] private ConfirmDialog confirmDialog;

        [SerializeField, Tooltip("슬롯을 고른 뒤 이동할 씬. 정식 게임 씬이 생기면 교체한다.")]
        private string gameSceneName = SceneNames.CombatSandbox;

        /// <summary>패널이 닫힐 때 발생. 메인 메뉴가 버튼을 다시 보여주는 데 쓴다.</summary>
        public event Action Closed;

        private void Awake()
        {
            for (int i = 0; i < slotViews.Count; i++)
            {
                SaveSlotView view = slotViews[i];
                if (view == null)
                {
                    continue;
                }

                view.SelectRequested += HandleSelectRequested;
                view.DeleteRequested += HandleDeleteRequested;
            }

            if (backButton != null)
            {
                backButton.onClick.AddListener(Hide);
            }
        }

        private void OnDestroy()
        {
            for (int i = 0; i < slotViews.Count; i++)
            {
                SaveSlotView view = slotViews[i];
                if (view == null)
                {
                    continue;
                }

                view.SelectRequested -= HandleSelectRequested;
                view.DeleteRequested -= HandleDeleteRequested;
            }

            if (backButton != null)
            {
                backButton.onClick.RemoveListener(Hide);
            }
        }

        public void Show()
        {
            SetVisible(true);
            RefreshAll();
        }

        public void Hide()
        {
            if (confirmDialog != null)
            {
                confirmDialog.Hide();
            }

            SetVisible(false);

            Action handler = Closed;
            if (handler != null)
            {
                handler.Invoke();
            }
        }

        public void RefreshAll()
        {
            for (int i = 0; i < slotViews.Count; i++)
            {
                if (slotViews[i] != null)
                {
                    slotViews[i].Refresh();
                }
            }
        }

        private void HandleSelectRequested(int slot, bool isEmpty)
        {
            bool started = isEmpty
                ? GameSession.StartNew(slot)
                : GameSession.Continue(slot);

            if (!started)
            {
                // 실패 원인은 GameSession/SaveSystem이 이미 로그로 남긴다. 목록만 갱신해 상태를 다시 보여준다.
                RefreshAll();
                return;
            }

            SceneLoader.Load(gameSceneName);
        }

        private void HandleDeleteRequested(int slot)
        {
            string message = "슬롯 " + (slot + 1) + "의 저장 데이터를 삭제할까요?\n되돌릴 수 없습니다.";

            if (confirmDialog == null)
            {
                // 확인 창이 없으면 삭제를 실행하지 않는다 — 실수로 진행이 날아가는 게 더 나쁘다.
                Debug.LogError("[SaveSlotPanel] ConfirmDialog가 연결되지 않아 삭제를 취소했습니다.");
                return;
            }

            confirmDialog.Show(message, () =>
            {
                SaveSystem.Delete(slot);
                RefreshAll();
            });
        }

        private void SetVisible(bool visible)
        {
            GameObject target = root != null ? root : gameObject;
            target.SetActive(visible);
        }

#if UNITY_EDITOR
        public void EditorBind(GameObject panelRoot, List<SaveSlotView> views, Button back, ConfirmDialog dialog)
        {
            root = panelRoot;
            slotViews = views;
            backButton = back;
            confirmDialog = dialog;
        }
#endif
    }
}
