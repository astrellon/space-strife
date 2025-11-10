using System;
using UnityEngine;

#nullable enable

namespace Orbits
{
    public class UIScaleForMobile : MonoBehaviour
    {
        #region Fields
        public float LandscapeScale = 1.75f;
        public float PortraitScale = 1.5f;
        #endregion

        #region Unity Methods
        private void Start()
        {
            if (Application.platform != RuntimePlatform.Android && Application.platform != RuntimePlatform.IPhonePlayer)
            {
                this.enabled = false;
                return;
            }

            this.UpdateScale(GameManager.Instance.IsLandscape);
            GameManager.Instance.OnOrientationChange += this.OnOrientationChange;
        }
        #endregion

        #region Methods
        private void OnOrientationChange(bool landscape)
        {
            this.UpdateScale(landscape);
        }

        private void UpdateScale(bool landscape)
        {
            this.transform.localScale = Vector3.one * (landscape ? this.LandscapeScale : this.PortraitScale);
        }
        #endregion
    }
}