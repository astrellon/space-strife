using UnityEngine;
using UnityEngine.Rendering;

#nullable enable

namespace Orbits
{
    public class SaturationEffect : MonoBehaviour
    {
        #region Fields
        public static SaturationEffect Instance;

        public bool Show;
        public new Renderer renderer;
        private Material material;
        public float showAmount = 0.0f;

        public Vector3 PlanetPosition = Vector3.zero;
        public float MaxDistance = 1.0f;
        private Transform? following;
        #endregion

        #region Unity Methods
        private void Start()
        {
            Instance = this;

            this.material = this.renderer.material;

            if (!this.Show)
            {
                this.gameObject.SetActive(false);
                this.showAmount = 0.0f;
            }
        }

        private void Update()
        {
            if (this.showAmount <= 0.0f && !this.Show)
            {
                this.gameObject.SetActive(false);
                this.following = null;
                return;
            }

            var direction = this.Show ? Time.unscaledDeltaTime : -Time.unscaledDeltaTime;
            this.showAmount = Mathf.Clamp(this.showAmount + direction * 0.75f, 0f, 1.5f);
            var size = Easing.Quadratic.In(this.showAmount);

            var position = this.following == null ? this.PlanetPosition : this.following.position;

            var cameraPosition = Camera.main.transform.position;

            var toCamera = cameraPosition - position;
            var toCameraMag = toCamera.magnitude;
            if (toCameraMag > Mathf.Epsilon)
            {
                var distance = Mathf.Min(toCameraMag - 1.2f, this.MaxDistance);
                var clamped = Vector3.ClampMagnitude(toCamera, distance);
                this.transform.position = position + clamped;
            }

            this.material.SetFloat("_Size", size);
        }
        #endregion

        #region Methods
        public bool SetShow(bool value)
        {
            if (this.Show == value)
            {
                return false;
            }

            this.Show = value;
            this.gameObject.SetActive(true);
            return true;
        }

        public void SetShow(bool value, Vector3 planetPosition, float maxDistance)
        {
            if (this.SetShow(value))
            {
                this.PlanetPosition = planetPosition;
                this.MaxDistance = maxDistance;
            }
        }

        public void SetShow(bool value, Transform following, float maxDistance)
        {
            if (this.SetShow(value))
            {
                this.following = following;
                this.MaxDistance = maxDistance;
            }
        }
        #endregion
    }
}