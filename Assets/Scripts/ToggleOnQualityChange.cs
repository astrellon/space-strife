using System;
using UnityEngine;

#nullable enable

namespace Orbits
{
    public class ToggleOnQualityChange : MonoBehaviour
    {
        #region Fields
        public bool ShowForLowQuality;
        public bool ShowForHighQuality;
        #endregion

        #region Unity Methods
        private void Awake()
        {
            this.CheckForQuality();
            QualitySettings.activeQualityLevelChanged += this.OnQualityChange;
        }

        private void OnDestroy()
        {
            QualitySettings.activeQualityLevelChanged -= this.OnQualityChange;
        }
        #endregion

        #region Methods
        private void OnQualityChange(int previousQuality, int currentQuality)
        {
            this.CheckForQuality();
        }

        private void CheckForQuality()
        {
            var qualityLevel = QualitySettings.GetQualityLevel();
            var isLowQuality = qualityLevel == 0;
            var show = (isLowQuality && this.ShowForLowQuality) || (!isLowQuality && this.ShowForHighQuality);
            this.gameObject.SetActive(show);
        }
        #endregion
    }
}