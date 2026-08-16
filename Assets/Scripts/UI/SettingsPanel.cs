using System;
using System.Collections.Generic;
using MathRPG.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MathRPG.UI
{
    /// <summary>
    /// 설정 화면. 현재는 화면(해상도·전체화면)과 소리(마스터/BGM/효과음)만 다룬다.
    ///
    /// 편집 중인 값은 <see cref="_draft"/>에만 반영되고, [적용]을 눌러야 실제로 저장된다.
    /// 슬라이더를 만질 때마다 해상도가 바뀌는 것을 막기 위한 구조다.
    /// </summary>
    public sealed class SettingsPanel : MonoBehaviour
    {
        [SerializeField] private GameObject root;

        [Header("화면")]
        [SerializeField] private TMP_Dropdown resolutionDropdown;
        [SerializeField] private Toggle fullScreenToggle;

        [Header("소리")]
        [SerializeField] private Slider masterSlider;
        [SerializeField] private Slider bgmSlider;
        [SerializeField] private Slider sfxSlider;
        [SerializeField] private TextMeshProUGUI masterValueLabel;
        [SerializeField] private TextMeshProUGUI bgmValueLabel;
        [SerializeField] private TextMeshProUGUI sfxValueLabel;

        [Header("버튼")]
        [SerializeField] private Button applyButton;
        [SerializeField] private Button resetButton;
        [SerializeField] private Button backButton;

        /// <summary>패널이 닫힐 때 발생. 메인 메뉴가 버튼을 다시 보여주는 데 쓴다.</summary>
        public event Action Closed;

        private readonly List<ScreenSize> _resolutions = new List<ScreenSize>();
        private GameSettingsData _draft;

        private void Awake()
        {
            BuildResolutionOptions();

            if (masterSlider != null)
            {
                masterSlider.onValueChanged.AddListener(OnMasterChanged);
            }

            if (bgmSlider != null)
            {
                bgmSlider.onValueChanged.AddListener(OnBgmChanged);
            }

            if (sfxSlider != null)
            {
                sfxSlider.onValueChanged.AddListener(OnSfxChanged);
            }

            if (applyButton != null)
            {
                applyButton.onClick.AddListener(ApplyDraft);
            }

            if (resetButton != null)
            {
                resetButton.onClick.AddListener(ResetToDefault);
            }

            if (backButton != null)
            {
                backButton.onClick.AddListener(Hide);
            }
        }

        private void OnDestroy()
        {
            if (masterSlider != null)
            {
                masterSlider.onValueChanged.RemoveListener(OnMasterChanged);
            }

            if (bgmSlider != null)
            {
                bgmSlider.onValueChanged.RemoveListener(OnBgmChanged);
            }

            if (sfxSlider != null)
            {
                sfxSlider.onValueChanged.RemoveListener(OnSfxChanged);
            }

            if (applyButton != null)
            {
                applyButton.onClick.RemoveListener(ApplyDraft);
            }

            if (resetButton != null)
            {
                resetButton.onClick.RemoveListener(ResetToDefault);
            }

            if (backButton != null)
            {
                backButton.onClick.RemoveListener(Hide);
            }
        }

        public void Show()
        {
            _draft = SettingsService.Current.Clone();

            // 활성화를 먼저 해야 Awake가 돌아 해상도 목록이 채워진다.
            // 순서를 바꾸면 드롭다운이 빈 상태에서 값을 넣게 된다.
            SetVisible(true);
            PushDraftToControls();
        }

        public void Hide()
        {
            SetVisible(false);

            Action handler = Closed;
            if (handler != null)
            {
                handler.Invoke();
            }
        }

        private void BuildResolutionOptions()
        {
            _resolutions.Clear();
            _resolutions.AddRange(SettingsService.GetAvailableResolutions());

            if (resolutionDropdown == null)
            {
                return;
            }

            var labels = new List<string>(_resolutions.Count);
            for (int i = 0; i < _resolutions.Count; i++)
            {
                labels.Add(_resolutions[i].ToString());
            }

            resolutionDropdown.ClearOptions();
            resolutionDropdown.AddOptions(labels);
        }

        /// <summary>편집 중인 값을 각 컨트롤에 반영한다. 이 동안은 콜백이 draft를 덮어쓰지 않게 주의.</summary>
        private void PushDraftToControls()
        {
            if (resolutionDropdown != null)
            {
                int index = FindResolutionIndex(_draft.resolutionWidth, _draft.resolutionHeight);
                resolutionDropdown.SetValueWithoutNotify(Mathf.Max(0, index));
                resolutionDropdown.RefreshShownValue();
            }

            if (fullScreenToggle != null)
            {
                fullScreenToggle.SetIsOnWithoutNotify(_draft.FullScreenMode != FullScreenMode.Windowed);
            }

            SetSliderSilently(masterSlider, _draft.masterVolume);
            SetSliderSilently(bgmSlider, _draft.bgmVolume);
            SetSliderSilently(sfxSlider, _draft.sfxVolume);

            UpdateVolumeLabels();
        }

        private static void SetSliderSilently(Slider slider, float value)
        {
            if (slider != null)
            {
                slider.SetValueWithoutNotify(value);
            }
        }

        private int FindResolutionIndex(int width, int height)
        {
            for (int i = 0; i < _resolutions.Count; i++)
            {
                if (_resolutions[i].Width == width && _resolutions[i].Height == height)
                {
                    return i;
                }
            }

            return -1;
        }

        private void OnMasterChanged(float value)
        {
            if (_draft != null)
            {
                _draft.masterVolume = value;
            }

            // 마스터만은 즉시 반영한다 — 소리를 들으면서 조절해야 의미가 있다.
            AudioListener.volume = value;
            UpdateVolumeLabels();
        }

        private void OnBgmChanged(float value)
        {
            if (_draft != null)
            {
                _draft.bgmVolume = value;
            }

            UpdateVolumeLabels();
        }

        private void OnSfxChanged(float value)
        {
            if (_draft != null)
            {
                _draft.sfxVolume = value;
            }

            UpdateVolumeLabels();
        }

        private void UpdateVolumeLabels()
        {
            if (_draft == null)
            {
                return;
            }

            SetPercentLabel(masterValueLabel, _draft.masterVolume);
            SetPercentLabel(bgmValueLabel, _draft.bgmVolume);
            SetPercentLabel(sfxValueLabel, _draft.sfxVolume);
        }

        private static void SetPercentLabel(TextMeshProUGUI label, float value01)
        {
            if (label != null)
            {
                label.text = Mathf.RoundToInt(value01 * 100f) + "%";
            }
        }

        private void ApplyDraft()
        {
            if (_draft == null)
            {
                return;
            }

            if (resolutionDropdown != null && _resolutions.Count > 0)
            {
                int index = Mathf.Clamp(resolutionDropdown.value, 0, _resolutions.Count - 1);
                _draft.resolutionWidth = _resolutions[index].Width;
                _draft.resolutionHeight = _resolutions[index].Height;
            }

            if (fullScreenToggle != null)
            {
                _draft.FullScreenMode = fullScreenToggle.isOn
                    ? FullScreenMode.FullScreenWindow
                    : FullScreenMode.Windowed;
            }

            SettingsService.Apply(_draft);
            _draft = SettingsService.Current.Clone();
        }

        private void ResetToDefault()
        {
            SettingsService.ResetToDefault();
            _draft = SettingsService.Current.Clone();
            PushDraftToControls();
        }

        private void SetVisible(bool visible)
        {
            GameObject target = root != null ? root : gameObject;
            target.SetActive(visible);
        }

#if UNITY_EDITOR
        public void EditorBindRoot(GameObject panelRoot)
        {
            root = panelRoot;
        }

        public void EditorBindScreen(TMP_Dropdown dropdown, Toggle fullScreen)
        {
            resolutionDropdown = dropdown;
            fullScreenToggle = fullScreen;
        }

        public void EditorBindAudio(Slider master, TextMeshProUGUI masterLabel,
                                    Slider bgm, TextMeshProUGUI bgmLabel,
                                    Slider sfx, TextMeshProUGUI sfxLabel)
        {
            masterSlider = master;
            masterValueLabel = masterLabel;
            bgmSlider = bgm;
            bgmValueLabel = bgmLabel;
            sfxSlider = sfx;
            sfxValueLabel = sfxLabel;
        }

        public void EditorBindButtons(Button apply, Button reset, Button back)
        {
            applyButton = apply;
            resetButton = reset;
            backButton = back;
        }
#endif
    }
}
