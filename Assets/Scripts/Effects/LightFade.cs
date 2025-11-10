using UnityEngine;

#nullable enable

namespace Orbits
{
    public class LightFade : MonoBehaviour
    {
        #region Fields
        public Light? Light;
        public float LifeTime = 0.2f;
        private float startingIntensity = 1.0f;
        private float startTime = 0.0f;
        #endregion

        #region Unity Methods
        private void Start()
        {
            if (this.Light == null)
            {
                this.Light = this.GetComponent<Light>();
            }

            if (this.Light == null)
            {
                this.enabled = false;
                return;
            }

            this.startingIntensity = this.Light.intensity;
        }

        private void OnEnable()
        {
            this.startTime = Time.timeSinceLevelLoad;
        }

        private void Update()
        {
            if (this.Light == null)
            {
                this.enabled = false;
                return;
            }

            var t = Mathf.Clamp01((Time.timeSinceLevelLoad - this.startTime) / this.LifeTime);
            var eased = Easing.Quadratic.Out(1.0f - t);
            var intensity = eased * this.startingIntensity;
            this.Light.intensity = intensity;
        }
        #endregion

        #region Methods
        #endregion
    }
}