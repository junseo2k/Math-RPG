using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MathRPG.UI
{
    /// <summary>
    /// 되돌릴 수 없는 동작 앞에 띄우는 확인 창.
    /// 지금은 세이브 삭제에만 쓰이지만, 이후 "저장하지 않고 나가기" 등에도 재사용한다.
    /// </summary>
    public sealed class ConfirmDialog : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private TextMeshProUGUI messageLabel;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;

        private Action _onConfirm;

        private void Awake()
        {
            if (confirmButton != null)
            {
                confirmButton.onClick.AddListener(HandleConfirm);
            }

            if (cancelButton != null)
            {
                cancelButton.onClick.AddListener(Hide);
            }

            // 여기서 Hide()를 부르면 안 된다.
            // 이 오브젝트는 평소 비활성 상태라 Awake가 Show() 안에서 활성화되는 순간 실행되는데,
            // 그때 Hide()를 부르면 방금 설정한 콜백을 지우고 창을 다시 닫아버린다.
            // 초기 숨김은 씬에 비활성으로 배치하는 것으로 대신한다.
        }

        private void OnDestroy()
        {
            if (confirmButton != null)
            {
                confirmButton.onClick.RemoveListener(HandleConfirm);
            }

            if (cancelButton != null)
            {
                cancelButton.onClick.RemoveListener(Hide);
            }
        }

        public void Show(string message, Action onConfirm)
        {
            _onConfirm = onConfirm;

            if (messageLabel != null)
            {
                messageLabel.text = message;
            }

            SetVisible(true);
        }

        public void Hide()
        {
            _onConfirm = null;
            SetVisible(false);
        }

        private void HandleConfirm()
        {
            Action callback = _onConfirm;
            Hide();

            if (callback != null)
            {
                callback.Invoke();
            }
        }

        private void SetVisible(bool visible)
        {
            GameObject target = root != null ? root : gameObject;
            target.SetActive(visible);
        }
    }
}
