using System.Diagnostics.CodeAnalysis;
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

        #region Fields
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
                this.PauseGameInMenus == null)
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

            this.SelectorUI.SetActive(true);
            this.GraphicsUI.SetActive(false);
            this.GameplayUI.SetActive(false);
            this.AudioUI.SetActive(false);
        }
        #endregion

        #region Methods
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