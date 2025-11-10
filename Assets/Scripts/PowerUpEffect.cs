using UnityEngine;

#nullable enable

namespace Orbits
{
    public class PowerUpEffect : MonoBehaviour
    {
        #region Fields
        public static PowerUpEffect Instance;

        public bool Show;
        public ParticleSystem ParticleSystem;
        public Light Light;
        public float CloseTimeout = 1.0f;
        public float currentCloseTimeout = 0.0f;
        private float originalIntensity = 1.0f;
        private float intensityVelocity = 0.0f;
        private Transform? following;
        #endregion

        #region Unity Methods
        private void Start()
        {
            Instance = this;
            this.originalIntensity = this.Light.intensity;
            if (!this.Show)
            {
                this.gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            if (!this.Show && this.currentCloseTimeout <= 0.0f)
            {
                this.gameObject.SetActive(false);
                return;
            }

            var dt = Time.deltaTime * GameManager.Instance.PlayerDeltaTimeScale;
            if (!this.Show)
            {
                this.currentCloseTimeout = Mathf.Clamp(this.currentCloseTimeout - dt, 0.0f, 100.0f);
            }
            else
            {
                this.currentCloseTimeout = this.CloseTimeout;
            }

            var targetLightIntensity = this.Show ? this.originalIntensity : 0.0f;
            this.Light.intensity = Mathf.SmoothDamp(this.Light.intensity, targetLightIntensity, ref this.intensityVelocity, dt * 20.0f);

            if (this.following != null)
            {
                this.transform.position = this.following.position;
            }

            this.transform.Rotate(0, dt * 36.0f, 0, Space.World);
        }
        #endregion

        #region Methods
        public bool SetShow(bool value)
        {
            if (this.Show == value)
            {
                return false;
            }

            if (!value)
            {
                this.following = null;
            }

            this.Show = value;
            this.gameObject.SetActive(true);

            if (value)
            {
                this.ParticleSystem.Play(true);
            }
            else
            {
                this.ParticleSystem.Stop(true);
            }
            return true;
        }

        public void SetShow(bool value, Vector3 position, float scale)
        {
            if (this.SetShow(value))
            {
                this.transform.position = position;
                this.transform.localScale = Vector3.one * scale;
            }
        }

        public void SetShow(bool value, Transform following, float scale)
        {
            if (this.SetShow(value))
            {
                this.following = following;
                this.transform.position = following.position;
                this.transform.localScale = Vector3.one * scale;
            }
        }
        #endregion
    }
}