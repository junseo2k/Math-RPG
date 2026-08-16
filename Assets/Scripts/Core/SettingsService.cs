using System;
using System.Collections.Generic;
using UnityEngine;

namespace MathRPG.Core
{
    /// <summary>해상도 목록에 쓰는 가로×세로 한 쌍. 주사율은 구분하지 않는다.</summary>
    public struct ScreenSize : IEquatable<ScreenSize>
    {
        public readonly int Width;
        public readonly int Height;

        public ScreenSize(int width, int height)
        {
            Width = width;
            Height = height;
        }

        public bool Equals(ScreenSize other)
        {
            return Width == other.Width && Height == other.Height;
        }

        public override bool Equals(object obj)
        {
            return obj is ScreenSize && Equals((ScreenSize)obj);
        }

        public override int GetHashCode()
        {
            return (Width * 397) ^ Height;
        }

        public override string ToString()
        {
            return Width + " × " + Height;
        }
    }

    /// <summary>
    /// 사용자 설정의 저장·적용을 담당한다.
    ///
    /// 소리에 대한 현재 한계: 아직 오디오 시스템이 없어서 마스터 볼륨만
    /// <see cref="AudioListener.volume"/>에 실제로 적용된다. BGM/효과음 값은
    /// 보관·노출만 하고, M1에서 사운드 레이어링(기획서 5-7)을 구현할 때
    /// AudioMixer 그룹에 연결한다. 그때 이 클래스의 공개 API는 그대로 두고
    /// <see cref="Apply"/> 내부만 교체하면 된다.
    /// </summary>
    public static class SettingsService
    {
        private const string PlayerPrefsKey = "MathRPG.Settings";

        /// <summary>설정이 적용될 때마다 발생. 오디오 시스템·UI가 구독한다.</summary>
        public static event Action<GameSettingsData> SettingsApplied;

        private static GameSettingsData _current;

        public static GameSettingsData Current
        {
            get
            {
                if (_current == null)
                {
                    Load();
                }
                return _current;
            }
        }

        public static float MasterVolume { get { return Current.masterVolume; } }
        public static float BgmVolume { get { return Current.bgmVolume; } }
        public static float SfxVolume { get { return Current.sfxVolume; } }

        /// <summary>저장된 설정을 읽어 메모리에 올린다. 없으면 기본값을 만든다.</summary>
        public static void Load()
        {
            string json = PlayerPrefs.GetString(PlayerPrefsKey, string.Empty);

            if (string.IsNullOrEmpty(json))
            {
                _current = GameSettingsData.CreateDefault();
            }
            else
            {
                try
                {
                    _current = JsonUtility.FromJson<GameSettingsData>(json) ?? GameSettingsData.CreateDefault();
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[SettingsService] 설정을 읽지 못해 기본값으로 되돌립니다: " + e.Message);
                    _current = GameSettingsData.CreateDefault();
                }
            }

            _current.Clamp();
        }

        /// <summary>주어진 설정을 현재 값으로 삼아 저장하고 즉시 적용한다.</summary>
        public static void Apply(GameSettingsData settings, bool persist = true)
        {
            if (settings == null)
            {
                Debug.LogError("[SettingsService] null 설정은 적용할 수 없습니다.");
                return;
            }

            settings.Clamp();
            _current = settings;

            // 화면
            if (Screen.width != settings.resolutionWidth ||
                Screen.height != settings.resolutionHeight ||
                Screen.fullScreenMode != settings.FullScreenMode)
            {
                Screen.SetResolution(settings.resolutionWidth, settings.resolutionHeight, settings.FullScreenMode);
            }

            // 소리 — 현재는 마스터만 실제로 반영된다 (클래스 주석 참고).
            AudioListener.volume = settings.masterVolume;

            if (persist)
            {
                Save();
            }

            Action<GameSettingsData> handler = SettingsApplied;
            if (handler != null)
            {
                handler.Invoke(settings);
            }
        }

        public static void Save()
        {
            if (_current == null)
            {
                return;
            }

            PlayerPrefs.SetString(PlayerPrefsKey, JsonUtility.ToJson(_current));
            PlayerPrefs.Save();
        }

        public static void ResetToDefault()
        {
            Apply(GameSettingsData.CreateDefault());
        }

        /// <summary>
        /// 이 기기가 지원하는 해상도 목록을 큰 것부터 정렬해 돌려준다.
        /// 같은 가로×세로가 주사율만 다르게 여러 번 나오므로 중복을 제거한다.
        /// </summary>
        public static List<ScreenSize> GetAvailableResolutions()
        {
            var seen = new HashSet<ScreenSize>();
            var result = new List<ScreenSize>();

            Resolution[] resolutions = Screen.resolutions;
            for (int i = 0; i < resolutions.Length; i++)
            {
                var size = new ScreenSize(resolutions[i].width, resolutions[i].height);
                if (seen.Add(size))
                {
                    result.Add(size);
                }
            }

            // 전체화면 전용 환경 등에서 목록이 비면 현재 해상도라도 넣어준다.
            if (result.Count == 0)
            {
                result.Add(new ScreenSize(Screen.width, Screen.height));
            }

            result.Sort((a, b) =>
            {
                int byWidth = b.Width.CompareTo(a.Width);
                return byWidth != 0 ? byWidth : b.Height.CompareTo(a.Height);
            });

            return result;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlayModeStart()
        {
            SettingsApplied = null;
            _current = null;
        }

        /// <summary>게임 시작 시 저장된 설정을 자동으로 적용한다.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ApplyOnLaunch()
        {
            Load();
            // 실행 직후 해상도를 강제로 바꾸면 에디터에서 거슬리므로 소리만 반영한다.
            AudioListener.volume = _current.masterVolume;
        }
    }
}
