using System;
using UnityEngine;

#nullable enable

namespace Orbits
{
    public class UIOrientationMove : MonoBehaviour
    {
        #region Fields
        public Vector2 LandscapePosition = Vector2.zero;
        public Vector2 PortraitPosition = Vector2.zero;
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

            this.UpdatePosition(GameManager.Instance.IsLandscape);
            GameManager.Instance.OnOrientationChange += this.OnOrientationChange;
        }

#if UNITY_EDITOR
        private void Update()
        {
            this.UpdatePosition(GameManager.Instance.IsLandscape);
        }
#endif
        #endregion

        #region Methods
        private void OnOrientationChange(bool landscape)
        {
            this.UpdatePosition(landscape);
        }

        private void UpdatePosition(bool landscape)
        {
            if (this.rect == null)
            {
                return;
            }

            this.rect.anchoredPosition = landscape ? this.LandscapePosition : this.PortraitPosition;
        }
        #endregion
    }
}