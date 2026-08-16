using System;
using UnityEngine;

namespace MathRPG.Core
{
    /// <summary>
    /// 사용자 설정 값. PlayerPrefs에 JSON 문자열로 보관된다.
    /// JsonUtility로 직렬화되므로 public 필드만 사용한다.
    /// </summary>
    [Serializable]
    public sealed class GameSettingsData
    {
        public const int Version = 1;

        public int version = Version;

        [Header("화면")]
        public int resolutionWidth;
        public int resolutionHeight;

        /// <summary><see cref="FullScreenMode"/>를 int로 보관. 0=ExclusiveFullScreen, 1=FullScreenWindow, 3=Windowed.</summary>
        public int fullScreenMode = (int)FullScreenMode.FullScreenWindow;

        [Header("소리 (0~1)")]
        public float masterVolume = 1f;
        public float bgmVolume = 0.8f;
        public float sfxVolume = 1f;

        public FullScreenMode FullScreenMode
        {
            get { return (FullScreenMode)fullScreenMode; }
            set { fullScreenMode = (int)value; }
        }

        /// <summary>현재 화면 상태를 기준으로 한 기본값.</summary>
        public static GameSettingsData CreateDefault()
        {
            return new GameSettingsData
            {
                version = Version,
                resolutionWidth = Screen.currentResolution.width,
                resolutionHeight = Screen.currentResolution.height,
                fullScreenMode = (int)FullScreenMode.FullScreenWindow,
                masterVolume = 1f,
                bgmVolume = 0.8f,
                sfxVolume = 1f
            };
        }

        public void Clamp()
        {
            masterVolume = Mathf.Clamp01(masterVolume);
            bgmVolume = Mathf.Clamp01(bgmVolume);
            sfxVolume = Mathf.Clamp01(sfxVolume);

            if (resolutionWidth <= 0 || resolutionHeight <= 0)
            {
                resolutionWidth = Screen.currentResolution.width;
                resolutionHeight = Screen.currentResolution.height;
            }
        }

        public GameSettingsData Clone()
        {
            return (GameSettingsData)MemberwiseClone();
        }
    }
}
