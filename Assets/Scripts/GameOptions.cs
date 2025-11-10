using System;
using System.Linq;
using UnityEngine;

#nullable enable

namespace Orbits
{
    public enum SettingType
    {
        Bloom, NativeFrameRate, ShowFps, Vsync, MasterVolume, MusicVolume, TankGlow, SFXVolume,
        DialogueVolume, UIVolume, GameSpeedScale, WaveSpeedScale,
        ToggleTankFire, PauseGameInMenus
    }

    public static class GameOptions
    {
        public delegate void ChangeHandler(SettingType type);
        public static event ChangeHandler? OnChange;

        #region Fields
        public static bool EnableBloom
        {
            get => GetBool(SettingType.Bloom, true);
            set => ChangeValue(SettingType.Bloom, value);
        }

        public static bool NativeFrameRate
        {
            get => GetBool(SettingType.NativeFrameRate, true);
            set => ChangeValue(SettingType.NativeFrameRate, value);
        }

        public static bool ShowFps
        {
            get => GetBool(SettingType.ShowFps, false);
            set => ChangeValue(SettingType.ShowFps, value);
        }

        public static bool HighQuality
        {
            get => QualitySettings.GetQualityLevel() > 0;
            set
            {
                QualitySettings.SetQualityLevel(value ? QualitySettings.names.Length - 1 : 0);
                SetVsync(Vsync);
            }
        }

        public static bool Vsync
        {
            get => GetBool(SettingType.Vsync, false);
            set
            {
                ChangeValue(SettingType.Vsync, value);
                SetVsync(value);
            }
        }

        public static bool FullScreen
        {
            get => Screen.fullScreen;
            set
            {
                if (value)
                {
                    Screen.SetResolution(Screen.width, Screen.height, true);
                }
                else
                {
                    Screen.fullScreen = false;
                }
            }
        }

        public static float MasterVolume
        {
            get => GetFloat(SettingType.MasterVolume, 1.0f);
            set => ChangeValue(SettingType.MasterVolume, Mathf.Clamp01(value));
        }

        public static float MusicVolume
        {
            get => GetFloat(SettingType.MusicVolume, 1.0f);
            set => ChangeValue(SettingType.MusicVolume, Mathf.Clamp01(value));
        }

        public static float SFXVolume
        {
            get => GetFloat(SettingType.SFXVolume, 1.0f);
            set => ChangeValue(SettingType.SFXVolume, Mathf.Clamp01(value));
        }

        public static float DialogueVolume
        {
            get => GetFloat(SettingType.DialogueVolume, 1.0f);
            set => ChangeValue(SettingType.DialogueVolume, Mathf.Clamp01(value));
        }

        public static float UIVolume
        {
            get => GetFloat(SettingType.UIVolume, 1.0f);
            set => ChangeValue(SettingType.UIVolume, Mathf.Clamp01(value));
        }

        public static float TankGlow
        {
            get => GetFloat(SettingType.TankGlow, 0.5f);
            set => ChangeValue(SettingType.TankGlow, Mathf.Clamp01(value));
        }

        public static float GameSpeedScale
        {
            get => GetFloat(SettingType.GameSpeedScale, 1.0f);
            set => ChangeValue(SettingType.GameSpeedScale, Mathf.Clamp(value, 0.1f, 3.0f));
        }

        public static float WaveSpeedScale
        {
            get => GetFloat(SettingType.WaveSpeedScale, 1.0f);
            set => ChangeValue(SettingType.WaveSpeedScale, Mathf.Clamp(value, 0.1f, 3.0f));
        }

        public static bool ToggleTankFire
        {
            get => GetBool(SettingType.ToggleTankFire, false);
            set => ChangeValue(SettingType.ToggleTankFire, value);
        }

        public static bool PauseGameInMenus
        {
            get => GetBool(SettingType.PauseGameInMenus, false);
            set => ChangeValue(SettingType.PauseGameInMenus, value);
        }

        private static bool GetBool(SettingType type, bool defaultValue)
        {
            return PlayerPrefs.GetInt(type.ToString(), defaultValue ? 1 : 0) > 0;
        }

        private static int GetInt(SettingType type, int defaultValue)
        {
            return PlayerPrefs.GetInt(type.ToString(), defaultValue);
        }

        private static float GetFloat(SettingType type, float defaultValue)
        {
            return PlayerPrefs.GetFloat(type.ToString(), defaultValue);
        }

        private static bool ChangeValue(SettingType type, float newValue)
        {
            var str = type.ToString();
            if (PlayerPrefs.HasKey(str) && PlayerPrefs.GetFloat(str) == newValue)
            {
                return false;
            }

            PlayerPrefs.SetFloat(str, newValue);
            OnChange?.Invoke(type);
            return true;
        }

        private static bool ChangeValue(SettingType type, int newValue)
        {
            var str = type.ToString();
            if (PlayerPrefs.HasKey(str) && PlayerPrefs.GetInt(str) == newValue)
            {
                return false;
            }

            PlayerPrefs.SetInt(str, newValue);
            OnChange?.Invoke(type);
            return true;
        }

        private static bool ChangeValue(SettingType type, bool newValue)
        {
            var value = newValue ? 1 : 0;
            var str = type.ToString();
            if (PlayerPrefs.HasKey(str) && PlayerPrefs.GetInt(str) == value)
            {
                return false;
            }

            PlayerPrefs.SetInt(str, value);
            OnChange?.Invoke(type);
            return true;
        }

        private static void SetVsync(bool value)
        {
            QualitySettings.vSyncCount = value ? 1 : 0;
        }
        #endregion
    }
}