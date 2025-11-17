using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#nullable enable

namespace Orbits
{
    public class UIOptionsMenu : MonoBehaviour
    {
        public enum TabType
        {
            Selector, Gameplay, Graphics, Audio
        }

        private class GameResolution : IComparable<GameResolution>
        {
            public readonly int Width;
            public readonly int Height;
            public readonly float AspectRatio;

            public GameResolution(int width, int height)
            {
                this.Width = width;
                this.Height = height;
                if (height > 0)
                {
                    this.AspectRatio = (float)width / height;
                }
            }

            public int CompareTo(GameResolution other)
            {
                var widthCompare = other.Width.CompareTo(this.Width);
                if (widthCompare != 0)
                {
                    return widthCompare;
                }

                return other.Height.CompareTo(this.Height);
            }
        }

        #region Fields
        private const float Aspect16_10 = 16.0f / 10.0f;
        private const float Aspect16_9 = 16.0f / 9.0f;
        private const float Aspect4_3 = 4.0f / 3.0f;

        public TabType CurrentTab = TabType.Selector;

        public GameObject? SelectorUI;
        public GameObject? GameplayUI;
        public GameObject? GraphicsUI;
        public GameObject? AudioUI;

        public Toggle? EnableBloom;
        public Toggle? NativeFrameRate;
        public Toggle? FullScreen;
        public Toggle? HighQuality;
        public Toggle? ShowFps;
        public Toggle? Vsync;
        public Slider? MasterVolumeSlider;
        public Slider? MusicVolumeSlider;
        public Slider? SFXVolumeSlider;
        public Slider? DialogueVolumeSlider;
        public Slider? UIVolumeSlider;
        public Slider? TankGlowSlider;
        public Slider? GameSpeedScaleSlider;
        public Slider? WaveSpeedScaleSlider;
        public Toggle? ToggleTankFire;
        public Toggle? PauseGameInMenus;
        public TMP_Dropdown? ResolutionsMenu;
        public TMP_Dropdown? RefreshRateMenu;

        private readonly List<GameResolution> gameResolutions = new();
        private readonly List<RefreshRate> gameRefreshRates = new();
        #endregion

        #region Unity Methods
        private void Start()
        {
            if (this.EnableBloom == null ||
                this.NativeFrameRate == null ||
                this.FullScreen == null ||
                this.HighQuality == null ||
                this.ShowFps == null ||
                this.Vsync == null ||
                this.MasterVolumeSlider == null ||
                this.MusicVolumeSlider == null ||
                this.SFXVolumeSlider == null ||
                this.DialogueVolumeSlider == null ||
                this.UIVolumeSlider == null ||
                this.TankGlowSlider == null ||
                this.GameSpeedScaleSlider == null ||
                this.WaveSpeedScaleSlider == null ||

                this.SelectorUI == null ||
                this.GraphicsUI == null ||
                this.GameplayUI == null ||
                this.AudioUI == null ||

                this.ToggleTankFire == null ||
                this.PauseGameInMenus == null ||

                this.ResolutionsMenu == null ||
                this.RefreshRateMenu == null)
            {
                Debug.LogError($"UIOptionsMenu missing set game objects sets");
                return;
            }

            this.EnableBloom.isOn = GameOptions.EnableBloom;
            this.NativeFrameRate.isOn = GameOptions.NativeFrameRate;

            if (Application.platform != RuntimePlatform.Android &&
                Application.platform != RuntimePlatform.IPhonePlayer)
            {
                this.FullScreen.isOn = GameOptions.FullScreen;
            }
            else
            {
                this.FullScreen.gameObject.SetActive(false);
            }

            this.HighQuality.isOn = GameOptions.HighQuality;
            this.ShowFps.isOn = GameOptions.ShowFps;
            this.Vsync.isOn = GameOptions.Vsync;
            this.MasterVolumeSlider.value = GameOptions.MasterVolume;
            this.MusicVolumeSlider.value = GameOptions.MusicVolume;
            this.SFXVolumeSlider.value = GameOptions.SFXVolume;
            this.DialogueVolumeSlider.value = GameOptions.DialogueVolume;
            this.UIVolumeSlider.value = GameOptions.UIVolume;
            this.TankGlowSlider.value = GameOptions.TankGlow;
            this.GameSpeedScaleSlider.value = GameOptions.GameSpeedScale;
            this.WaveSpeedScaleSlider.value = GameOptions.WaveSpeedScale;
            this.ToggleTankFire.isOn = GameOptions.ToggleTankFire;
            this.PauseGameInMenus.isOn = GameOptions.PauseGameInMenus;

            var showResolutionMenu = true;
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                showResolutionMenu = false;
            }
            else
            {
                this.UpdateResolutions();
            }

            this.RefreshRateMenu.gameObject.SetActive(showResolutionMenu);
            this.ResolutionsMenu.gameObject.SetActive(showResolutionMenu);
            this.FullScreen.gameObject.SetActive(!showResolutionMenu);

            this.SelectorUI.SetActive(true);
            this.GraphicsUI.SetActive(false);
            this.GameplayUI.SetActive(false);
            this.AudioUI.SetActive(false);
        }
        #endregion

        #region Methods
        private void UpdateResolutionList()
        {
            this.gameResolutions.Clear();
            this.gameRefreshRates.Clear();

            var resolutions = Screen.resolutions;
            var currentRes = Screen.currentResolution;

            var resolutionSet = new HashSet<long>();
            var refreshSet = new HashSet<ulong>();

            if (currentRes.refreshRateRatio.value > Mathf.Epsilon)
            {
                var key = (ulong)currentRes.refreshRateRatio.numerator << 32 | (ulong)currentRes.refreshRateRatio.denominator;
                refreshSet.Add(key);

                this.gameRefreshRates.Add(currentRes.refreshRateRatio);
            }

            for (var i = 0; i < resolutions.Length; i++)
            {
                var res = resolutions[i];

                var refreshKey = (ulong)res.refreshRateRatio.numerator << 32 | (ulong)res.refreshRateRatio.denominator;
                if (refreshSet.Add(refreshKey))
                {
                    this.gameRefreshRates.Add(res.refreshRateRatio);
                }

                var resKey = (long)res.width << 32 | (long)res.height;
                if (!resolutionSet.Add(resKey))
                {
                    continue;
                }

                this.gameResolutions.Add(new(res.width, res.height));
            }

            this.gameResolutions.Sort();
            this.gameResolutions.Insert(0, new(0, 0));
            this.gameRefreshRates.Sort((x, y) => x.value.CompareTo(y.value));
        }

        public void UpdateResolutions()
        {
            if (this.ResolutionsMenu == null || this.RefreshRateMenu == null)
            {
                return;
            }

            this.UpdateResolutionList();

            this.ResolutionsMenu.ClearOptions();
            this.RefreshRateMenu.ClearOptions();

            var selectedRes = Screen.fullScreen ? -1 : 0;
            var currentRes = Screen.currentResolution;

            var resOptions = new List<TMP_Dropdown.OptionData>(this.gameResolutions.Count);
            for (var i = 0; i < this.gameResolutions.Count; i++)
            {
                var resolution = this.gameResolutions[i];

                if (resolution.Width == 0)
                {
                    resOptions.Add(new("Windowed"));
                }
                else
                {
                    var text = $"{resolution.Width}x{resolution.Height}";
                    if (Mathf.Approximately(resolution.AspectRatio, Aspect16_10))
                    {
                        text += " (16:10)";
                    }
                    else if (Mathf.Approximately(resolution.AspectRatio, Aspect16_9))
                    {
                        text += " (16:9)";
                    }
                    else if (Mathf.Approximately(resolution.AspectRatio, Aspect4_3))
                    {
                        text += " (4:3)";
                    }

                    resOptions.Add(new(text));
                }

                if (selectedRes < 0 && resolution.Width == currentRes.width && resolution.Height == currentRes.height)
                {
                    selectedRes = i;
                }
            }

            this.ResolutionsMenu.AddOptions(resOptions);
            if (selectedRes >= 0)
            {
                this.ResolutionsMenu.value = selectedRes;
            }

            var selectedRate = -1;
            var rateOptions = new List<TMP_Dropdown.OptionData>(this.gameRefreshRates.Count);
            for (var i = 0; i < this.gameRefreshRates.Count; i++)
            {
                var rate = this.gameRefreshRates[i];
                if (selectedRate < 0 && rate.numerator == currentRes.refreshRateRatio.numerator && rate.denominator == currentRes.refreshRateRatio.denominator)
                {
                    selectedRate = i;
                }
                rateOptions.Add(new(rate.value.ToString("0.##")));
            }

            this.RefreshRateMenu.AddOptions(rateOptions);
            if (selectedRate >= 0)
            {
                this.RefreshRateMenu.value = selectedRate;
            }
        }

        public void OnResolutionChanged(int index)
        {
            var option = this.gameResolutions[index];
            if (option.Width <= 0)
            {
                Screen.fullScreen = false;
                return;
            }

            Screen.SetResolution(option.Width, option.Height, FullScreenMode.FullScreenWindow);
        }

        public void OnRefreshRateChanged(int index)
        {
            var option = this.gameRefreshRates[index];
            Screen.SetResolution(Screen.width, Screen.height, Screen.fullScreenMode, option);
        }

        public void OnChangeBloom(bool value)
        {
            GameOptions.EnableBloom = value;
        }

        public void OnChangeNativeFrameRate(bool value)
        {
            GameOptions.NativeFrameRate = value;
        }

        public void OnChangeFullScreen(bool value)
        {
            GameOptions.FullScreen = value;
        }

        public void OnChangeHighQuality(bool value)
        {
            GameOptions.HighQuality = value;
        }

        public void OnChangeVsync(bool value)
        {
            GameOptions.Vsync = value;
        }

        public void OnChangeShowFps(bool value)
        {
            GameOptions.ShowFps = value;
        }

        public void OnMasterVolumeChange(float value)
        {
            GameOptions.MasterVolume = value;
        }

        public void OnMusicVolumeChange(float value)
        {
            GameOptions.MusicVolume = value;
        }

        public void OnSFXVolumeChange(float value)
        {
            GameOptions.SFXVolume = value;
        }

        public void OnDialogueVolumeChange(float value)
        {
            GameOptions.DialogueVolume = value;
        }

        public void OnUIVolumeChange(float value)
        {
            GameOptions.UIVolume = value;
        }

        public void OnTankGlowChange(float value)
        {
            GameOptions.TankGlow = value;
        }

        public void OnGameSpeedScaleChange(float value)
        {
            GameOptions.GameSpeedScale = value;
        }

        public void OnWaveSpeedScaleChange(float value)
        {
            GameOptions.WaveSpeedScale = value;
        }

        public void OnChangeToggleTankFire(bool value)
        {
            GameOptions.ToggleTankFire = value;
        }

        public void OnChangePauseInMenus(bool value)
        {
            GameOptions.PauseGameInMenus = value;
        }

        public void SetTab(TabType tab)
        {
            if (this.TryGetTabObject(this.CurrentTab, out var current))
            {
                current.SetActive(false);
            }

            this.CurrentTab = tab;

            if (this.TryGetTabObject(this.CurrentTab, out var next))
            {
                next.SetActive(true);
            }
        }

        public bool GoBack()
        {
            if (this.CurrentTab == TabType.Selector)
            {
                return true;
            }

            this.SetTab(TabType.Selector);
            return false;
        }

        public void GoToSelector()
        {
            this.SetTab(TabType.Selector);
        }

        public void GoToGameplay()
        {
            this.SetTab(TabType.Gameplay);
        }

        public void GoToGraphics()
        {
            this.SetTab(TabType.Graphics);
        }

        public void GoToAudio()
        {
            this.SetTab(TabType.Audio);
        }

        private bool TryGetTabObject(TabType tab, [NotNullWhen(true)] out GameObject? result)
        {
            if (tab == TabType.Gameplay)
            {
                result = this.GameplayUI;
                return result != null;
            }
            if (tab == TabType.Graphics)
            {
                result = this.GraphicsUI;
                return result != null;
            }
            if (tab == TabType.Audio)
            {
                result = this.AudioUI;
                return result != null;
            }
            if (tab == TabType.Selector)
            {
                result = this.SelectorUI;
                return result != null;
            }

            result = null;
            return false;
        }
        #endregion
    }
}