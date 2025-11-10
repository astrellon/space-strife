using System.Collections.Generic;
using UnityEngine;

namespace Orbits
{
    [CreateAssetMenu(fileName="AudioOneOffs", menuName="Orbits/AudioOneOffs")]
    public class AudioOneOffs : ScriptableObject
    {
        #region Fields
        public List<AudioClip> Clips;
        public float RandomisePitch = 0.1f;
        #endregion

        #region Methods
        public AudioClip GetRandom()
        {
            return this.Clips[Random.Range(0, this.Clips.Count)];
        }
        #endregion
    }
}