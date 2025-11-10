using System.Collections.Generic;
using UnityEngine;

#nullable enable

namespace Orbits
{
    public class AudioOneOffRandomizer : MonoBehaviour
    {
        #region Fields
        public List<AudioClip> Clips = new();
        public AudioSource AudioSource;
        #endregion

        #region Unity Methods
        private void OnEnable()
        {
            var clip = this.Clips[Random.Range(0, this.Clips.Count)];
            this.AudioSource.clip = clip;
            this.AudioSource.Play();
        }
        #endregion
    }
}