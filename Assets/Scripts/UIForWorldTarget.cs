using System;
using UnityEngine;

#nullable enable

namespace Orbits
{
    public class UIForWorldTarget : MonoBehaviour
    {
        #region Fields
        public SelectableGameObject WorldTarget;
        public Vector3 Offset = new(0, 0.1f, 0);
        public float ShowSpeed = 1.0f;
        public bool ToShow = false;
        public bool ToUIPosition = false;
        public RectTransform? UIPosition;
        public float LerpToUIPosition;

        private RectTransform? rect;
        private float lerpToShow = 0.0f;

        public event Action? OnBeforeDisable;
        #endregion

        #region Unity Methods
        private void Start()
        {
            this.rect = this.GetComponent<RectTransform>();
        }

        private void Update()
        {
            this.lerpToShow = Mathf.Clamp01(this.lerpToShow + (this.ToShow ? Time.unscaledDeltaTime : -Time.unscaledDeltaTime) / this.ShowSpeed);
            if (this.lerpToShow <= 0.0f && !this.ToShow)
            {
                this.OnBeforeDisable?.Invoke();
                this.gameObject.SetActive(false);
                return;
            }

            this.LerpToUIPosition = Mathf.Clamp01(this.LerpToUIPosition + (this.ToUIPosition ? Time.unscaledDeltaTime : -Time.unscaledDeltaTime) / this.ShowSpeed);

            this.transform.localScale = Vector3.one * Easing.Back.Out(this.lerpToShow);

            var buttonPosition = this.WorldTarget.CalculateButtonOffset() + this.Offset;

            if (this.UIPosition != null && this.LerpToUIPosition > 0.0f)
            {
                var uiPosition = this.UIPosition.position;
                uiPosition.x /= Screen.width;
                uiPosition.y /= Screen.height;

                var lerpedT = Easing.Quadratic.InOut(this.LerpToUIPosition);
                buttonPosition = Vector3.Lerp(buttonPosition, uiPosition, lerpedT);
            }

            // This could maybe be changed to use position instead like the select buttons, but there's a few forgotten steps in how.
            this.rect.anchorMax = buttonPosition;
            this.rect.anchorMin = buttonPosition;

            // Need to reset the position for some reason
            this.rect.anchoredPosition3D = Vector3.zero;
        }
        #endregion

        #region Methods
        public void SetToShow(bool toShow)
        {
            this.ToShow = toShow;
            if (toShow)
            {
                this.gameObject.SetActive(true);
            }
        }
        #endregion
    }
}