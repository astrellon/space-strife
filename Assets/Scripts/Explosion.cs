using UnityEngine;

#nullable enable

namespace Orbits
{
    public class Explosion : MonoBehaviour
    {
        #region Fields
        public AudioSource? Audio;
		public float RandomPitchPercent = 10.0f;
        public Light? Light;
        public ReturnToPool? ReturnToPool;

        private float originalLightIntensity;
        private float counter = 0.0f;
        #endregion

        #region Unity Methods
        private void Awake()
        {
            if (this.Light != null)
            {
                this.originalLightIntensity = this.Light.intensity;
            }
        }

        private void OnEnable()
        {
            if (this.Audio != null)
            {
                this.Audio.pitch *= 1.0f + Random.Range(-this.RandomPitchPercent / 100.0f, this.RandomPitchPercent / 100.0f);
            }

            if (this.Light != null)
            {
                this.Light.intensity = this.originalLightIntensity;
            }
        }

        private void Update()
        {
            if (this.Audio != null)
            {
                this.Audio.pitch = GameManager.Instance.TimeScaleRatio;
            }
            if (this.Light != null)
            {
                this.counter += Time.deltaTime;

                var duration = this.ReturnToPool != null ? this.ReturnToPool.Duration : 1.0f;
                var t = Mathf.Clamp01(this.counter / duration);
                this.Light.intensity = this.originalLightIntensity * t;
            }
        }
        #endregion

        #region Methods
        #endregion
    }
}