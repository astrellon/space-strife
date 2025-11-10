using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

#nullable enable

namespace Orbits
{
    public class OptionsManager : MonoBehaviour
    {
        #region Fields
        public Volume? Volume;
        public Bloom? Bloom;
        public GameObject FPSCounter;
        #endregion

        #region Unity Methods
        private void Start()
        {
            if (this.Volume != null)
            {
                this.Volume.profile.TryGet<Bloom>(out this.Bloom);
            }

            this.UpdateBloom();
            this.UpdateFrameRate();
            this.UpdateShowFps();
            GameOptions.OnChange += this.OnChange;
        }
        #endregion

        #region Methods
        private void OnChange(SettingType type)
        {
            switch (type)
            {
                case SettingType.Bloom: this.UpdateBloom(); return;
                case SettingType.NativeFrameRate: this.UpdateFrameRate(); return;
                case SettingType.ShowFps: this.UpdateShowFps(); return;
            }
        }

        private void UpdateBloom()
        {
            if (this.Bloom == null)
            {
                Debug.LogWarning("Missing Bloom");
                return;
            }

            this.Bloom.active = GameOptions.EnableBloom;
        }

        private void UpdateShowFps()
        {
            this.FPSCounter.SetActive(GameOptions.ShowFps);
        }

        private void UpdateFrameRate()
        {
            var frameRate = 30;
            if (GameOptions.NativeFrameRate)
            {
                frameRate = -1;

                // There seems to be an issue where requesting the default frame rate on these
                // platforms defaults to 30 instead of native, so instead we'll choose a higher frame rate.
                if (Application.platform == RuntimePlatform.Android ||
                    Application.platform == RuntimePlatform.IPhonePlayer ||
                    Application.platform == RuntimePlatform.WebGLPlayer)
                {
                    frameRate = 120;
                }
            }

            Application.targetFrameRate = frameRate;
        }
        #endregion
    }
}