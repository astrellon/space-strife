using System;
using UnityEngine;

#nullable enable

namespace Orbits
{
    public class UIOrientationSize : MonoBehaviour
    {
        #region Fields
        public Vector2 LandscapeSize = Vector2.zero;
        public Vector2 PortraitSize = Vector2.zero;
        private RectTransform? rect;
        #endregion

        #region Unity Methods
        private void Start()
        {
            this.rect = this.GetComponent<RectTransform>();

#if !UNITY_EDITOR
            if (Application.platform != RuntimePlatform.Android && Application.platform != RuntimePlatform.IPhonePlayer)
            {
                this.enabled = false;
                return;
            }
#endif

            this.UpdateSize(GameManager.Instance.IsLandscape);
            GameManager.Instance.OnOrientationChange += this.OnOrientationChange;
        }

#if UNITY_EDITOR
        private void Update()
        {
            this.UpdateSize(GameManager.Instance.IsLandscape);
        }
#endif
        #endregion

        #region Methods
        private void OnOrientationChange(bool landscape)
        {
            this.UpdateSize(landscape);
        }

        private void UpdateSize(bool landscape)
        {
            if (this.rect == null)
            {
                return;
            }

            this.rect.sizeDelta = landscape ? this.LandscapeSize : this.PortraitSize;
        }
        #endregion
    }
}