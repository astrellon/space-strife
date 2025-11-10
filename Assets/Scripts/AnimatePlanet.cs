using UnityEngine;

#nullable enable

namespace Orbits
{
    public class AnimatePlanet : MonoBehaviour
    {
        public delegate void ShowTypeChangeHandler(ShowType previous, ShowType current, AnimatePlanet source);
        public enum ShowType
        {
            Hide, Primary, Secondary, Regular
        }

        #region Fields
        private const float EnableThreshold = 0.001f;

        public float Speed = 1.0f;
        public float PrimaryScale = 2.0f;
        public float SecondaryScale = 0.5f;
        public float RegularScale = 1.0f;
        public ShowType Show = ShowType.Hide;
        private float scale = 0.0f;
        private float time = 0.0f;

        public HideIfNotAvailable? HideIfNotAvailable;
        public event ShowTypeChangeHandler? OnShowTypeChange;
        #endregion

        #region Unity Methods
        private void Start()
        {
            var targetScale = this.GetTargetScale();
            this.transform.localScale = Vector3.one * targetScale;
            this.gameObject.SetActive(targetScale > EnableThreshold);
            this.scale = targetScale;
        }

        private void Update()
        {
            if (this.Speed < 0)
            {
                return;
            }

            this.time += Time.unscaledDeltaTime;
            var scaledTime = this.time / this.Speed;

            var targetScale = this.GetTargetScale();
            var lerpedScale = Mathf.Lerp(this.scale, targetScale, Easing.Quadratic.InOut(Mathf.Clamp01(scaledTime)));
            this.transform.localScale = lerpedScale * Vector3.one;

            if (this.time >= 3.0f)
            {
                this.scale = targetScale;
                this.enabled = false;
                if (targetScale < EnableThreshold)
                {
                    this.gameObject.SetActive(false);
                }
            }
        }

        private float GetTargetScale()
        {
            if (this.HideIfNotAvailable != null && this.HideIfNotAvailable.ShouldHide)
            {
                return 0.0f;
            }

            return this.Show switch
            {
                ShowType.Hide => 0.0f,
                ShowType.Primary => this.PrimaryScale,
                ShowType.Secondary => this.SecondaryScale,
                ShowType.Regular => this.RegularScale,
                _ => throw new System.Exception($"Unknown scale type: {this.Show}"),
            };
        }

        public void SetShow(ShowType type, float atTime = 0.0f)
        {
            if (type == this.Show)
            {
                return;
            }

            var prev = this.Show;

            this.Show = type;
            if (this.Speed < 0)
            {
                this.scale = this.GetTargetScale();
                this.transform.localScale = Vector3.one * this.scale;
            }
            else
            {
                this.scale = this.transform.localScale.x;
            }
            this.time = atTime;
            this.enabled = true;
            this.gameObject.SetActive(true);

            this.OnShowTypeChange?.Invoke(prev, type, this);
        }

        public void Finish()
        {
            this.time = this.Speed;
        }
        #endregion
    }
}