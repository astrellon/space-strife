using UnityEngine;

#nullable enable

namespace Orbits
{
    public class StarGlow : MonoBehaviour
    {
        #region Fields
        public float StartScale = 0.28f;
        public float MinDistance = 50.0f;
        public float OneDistance = 1000.0f;
        #endregion

        #region Unity Methods
        private void Update()
        {
            var distance = Vector3.Distance(Camera.main.transform.position, this.transform.position);
            if (distance < this.MinDistance)
            {
                this.transform.localScale = Vector3.one * this.StartScale;
            }

            var normalised = (distance - this.MinDistance) / (this.OneDistance - this.MinDistance);
            var scale = normalised * (1.0f - this.StartScale) + this.StartScale;
            this.transform.localScale = Vector3.one * scale;
        }
        #endregion

        #region Methods
        #endregion
    }
}