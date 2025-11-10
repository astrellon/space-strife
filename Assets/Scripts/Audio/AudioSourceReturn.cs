using UnityEngine;

#nullable enable

namespace Orbits
{
    public class AudioSourceReturn : MonoBehaviour
    {
        #region Fields
        public AudioSource? Source;
        public AudioSource? OriginalPrefab;
        #endregion

        #region Unity Methods
        void Update()
        {
            if (this.Source != null && !this.Source.isPlaying)
            {
                AudioSourcePools.Instance.Release(this.OriginalPrefab, this.Source);
            }
        }
        #endregion
    }
}