using System;
using MathRPG.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MathRPG.UI
{
    /// <summary>
    /// 세이브 슬롯 한 칸의 표시와 입력을 담당한다.
    /// 데이터 판단(불러오기/새로 만들기)은 하지 않고 요청만 위로 올린다 —
    /// 실제 처리는 <see cref="SaveSlotPanel"/>이 한다.
    /// </summary>
    public sealed class SaveSlotView : MonoBehaviour
    {
        [SerializeField] private int slotIndex;

        [Header("표시")]
        [SerializeField] private TextMeshProUGUI slotNumberLabel;
        [SerializeField] private TextMeshProUGUI summaryLabel;
        [SerializeField] private TextMeshProUGUI detailLabel;

        [Header("버튼")]
        [SerializeField] private Button selectButton;
        [SerializeField] private TextMeshProUGUI selectButtonLabel;
        [SerializeField] private Button deleteButton;

        /// <summary>슬롯 번호와 "비어 있는가"를 함께 올린다.</summary>
        public event Action<int, bool> SelectRequested;
        public event Action<int> DeleteRequested;

        public int SlotIndex
        {
            get { return slotIndex; }
        }

        private bool _isEmpty = true;

        private void Awake()
        {
            if (selectButton != null)
            {
                selectButton.onClick.AddListener(HandleSelect);
            }

            if (deleteButton != null)
            {
                deleteButton.onClick.AddListener(HandleDelete);
            }
        }

        private void OnDestroy()
        {
            if (selectButton != null)
            {
                selectButton.onClick.RemoveListener(HandleSelect);
            }

            if (deleteButton != null)
            {
                deleteButton.onClick.RemoveListener(HandleDelete);
            }
        }

        /// <summary>디스크 상태를 읽어 표시를 다시 그린다.</summary>
        public void Refresh()
        {
            if (slotNumberLabel != null)
            {
                slotNumberLabel.text = "슬롯 " + (slotIndex + 1);
            }

            SaveData data = SaveSystem.Load(slotIndex);
            bool fileExists = SaveSystem.Exists(slotIndex);

            if (data == null)
            {
                // 표시는 "빈 슬롯"과 "손상된 세이브"로 구분하되,
                // 동작은 둘 다 "새 게임(덮어쓰기)"으로 통일한다 —
                // 읽지 못하는 파일을 이어하기로 시도해봐야 실패만 반복된다.
                _isEmpty = true;
                ShowEmpty(fileExists);
                return;
            }

            _isEmpty = false;
            ShowSaved(data);
        }

        private void ShowEmpty(bool corrupted)
        {
            if (summaryLabel != null)
            {
                summaryLabel.text = corrupted ? "손상된 세이브" : "빈 슬롯";
            }

            if (detailLabel != null)
            {
                detailLabel.text = corrupted
                    ? "파일을 읽을 수 없습니다. 삭제 후 새로 시작하세요."
                    : "새 게임을 시작할 수 있습니다.";
            }

            if (selectButtonLabel != null)
            {
                selectButtonLabel.text = "새 게임";
            }

            if (selectButton != null)
            {
                selectButton.interactable = true;
            }

            if (deleteButton != null)
            {
                deleteButton.gameObject.SetActive(corrupted);
            }
        }

        private void ShowSaved(SaveData data)
        {
            ChapterId chapter = data.ChapterId;

            if (summaryLabel != null)
            {
                summaryLabel.text = "현재 스테이지: " + chapter.ToDisplayName() + " · 노드 " + data.nodeIndex;
            }

            if (detailLabel != null)
            {
                detailLabel.text = chapter.ToGradeRange()
                                   + " · 플레이 " + FormatPlayTime(data.playTimeSeconds)
                                   + " · " + FormatSaveTime(data.LastSavedUtc);
            }

            if (selectButtonLabel != null)
            {
                selectButtonLabel.text = "이어하기";
            }

            if (selectButton != null)
            {
                selectButton.interactable = true;
            }

            if (deleteButton != null)
            {
                deleteButton.gameObject.SetActive(true);
            }
        }

        private static string FormatPlayTime(double totalSeconds)
        {
            if (totalSeconds < 60d)
            {
                return "1분 미만";
            }

            var span = TimeSpan.FromSeconds(totalSeconds);
            int hours = (int)span.TotalHours;

            return hours > 0
                ? hours + "시간 " + span.Minutes + "분"
                : span.Minutes + "분";
        }

        private static string FormatSaveTime(DateTime utc)
        {
            if (utc == DateTime.MinValue)
            {
                return "저장 시각 없음";
            }

            return utc.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
        }

        private void HandleSelect()
        {
            Action<int, bool> handler = SelectRequested;
            if (handler != null)
            {
                handler.Invoke(slotIndex, _isEmpty);
            }
        }

        private void HandleDelete()
        {
            Action<int> handler = DeleteRequested;
            if (handler != null)
            {
                handler.Invoke(slotIndex);
            }
        }

#if UNITY_EDITOR
        /// <summary>씬 빌더가 슬롯 번호를 지정할 때 쓴다.</summary>
        public void EditorSetSlotIndex(int index)
        {
            slotIndex = index;
        }

        public void EditorBind(TextMeshProUGUI number, TextMeshProUGUI summary, TextMeshProUGUI detail,
                               Button select, TextMeshProUGUI selectLabel, Button delete)
        {
            slotNumberLabel = number;
            summaryLabel = summary;
            detailLabel = detail;
            selectButton = select;
            selectButtonLabel = selectLabel;
            deleteButton = delete;
        }
#endif
    }
}
