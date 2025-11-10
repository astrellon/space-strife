using UnityEngine;

#nullable enable

namespace Orbits
{
    public class AudioRandomisePitch : MonoBehaviour
    {
        #region Fields
        public AudioSource? Source;
        public float PitchRange = 0.1f;
        private float originalPitch = -10.0f;
        #endregion

        #region Unity Methods
        private void OnEnable()
        {
            if (this.Source == null)
            {
                this.Source = this.GetComponent<AudioSource>();
            }

            if (this.Source == null)
            {
                return;
            }

            if (this.originalPitch < -9.0f)
            {
                this.originalPitch = this.Source.pitch;
            }

            var randomOffset = 1.0f + Random.Range(-this.PitchRange, this.PitchRange);
            this.Source.pitch = this.originalPitch * randomOffset;
        }
        #endregion
    }
}