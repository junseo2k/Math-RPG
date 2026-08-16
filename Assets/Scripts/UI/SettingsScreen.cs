using System.Collections.Generic;
using MathRPG.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MathRPG.UI
{
    /// <summary>
    /// 설정 씬의 컨트롤러. 현재는 화면(해상도·전체화면)과 소리(마스터/BGM/효과음)만 다룬다.
    ///
    /// 편집 중인 값은 <see cref="_draft"/>에만 반영되고, [적용]을 눌러야 실제로 저장된다.
    /// 슬라이더를 만질 때마다 해상도가 바뀌는 것을 막기 위한 구조다.
    ///
    /// 뒤로 가기는 <see cref="MenuNavigation"/>이 기억한 곳으로 돌아가므로,
    /// 나중에 게임 중 일시정지에서 설정을 열어도 그대로 동작한다.
    /// </summary>
    public sealed class SettingsScreen : MonoBehaviour
    {
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

        private readonly List<ScreenSize> _resolutions = new List<ScreenSize>();
        private GameSettingsData _draft;

        /// <summary>이 화면에 들어올 때의 볼륨. 저장하지 않고 나가면 되돌린다.</summary>
        private float _masterVolumeOnEnter;

        private void Awake()
        {
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
                backButton.onClick.AddListener(GoBack);
            }
        }

        private void Start()
        {
            RefreshFromSettings();
        }

        /// <summary>
        /// 저장된 설정을 읽어 해상도 목록과 각 컨트롤을 채운다.
        /// 에디터 미리보기 도구에서도 호출한다.
        /// </summary>
        public void RefreshFromSettings()
        {
            BuildResolutionOptions();

            _masterVolumeOnEnter = SettingsService.Current.masterVolume;
            _draft = SettingsService.Current.Clone();
            PushDraftToControls();
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
                backButton.onClick.RemoveListener(GoBack);
            }
        }

        public void GoBack()
        {
            // 마스터 볼륨은 미리듣기를 위해 즉시 반영했으므로,
            // 적용하지 않고 나가면 들어올 때 값으로 되돌려 놓는다.
            AudioListener.volume = _masterVolumeOnEnter;
            MenuNavigation.GoBack();
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
            _masterVolumeOnEnter = _draft.masterVolume;
        }

        private void ResetToDefault()
        {
            SettingsService.ResetToDefault();
            _draft = SettingsService.Current.Clone();
            _masterVolumeOnEnter = _draft.masterVolume;
            PushDraftToControls();
        }

#if UNITY_EDITOR
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
