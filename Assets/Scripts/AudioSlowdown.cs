using UnityEngine;

#nullable enable

namespace Orbits
{
    public class AudioSlowdown : MonoBehaviour
    {
        #region Fields
        public AudioSource audio;
        #endregion

        #region Unity Methods
        private void Update()
        {
            this.audio.pitch = GameManager.Instance.TimeScaleRatio;
        }
        #endregion
    }
}