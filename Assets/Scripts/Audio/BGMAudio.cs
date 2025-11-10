using UnityEngine;

#nullable enable

namespace Orbits
{
    public class BGMAudio : MonoBehaviour
    {
        #region Fields
        public double LoopStartSeconds;
        public double LoopEndSeconds;
        public AudioSource AudioSource;

        private int loopStartSamples;
        private int loopEndSamples;
        private int loopLengthSamples;
        #endregion

        #region Unity Methods
        private void Update()
        {
            if (this.loopEndSamples > 0 && this.AudioSource.timeSamples >= this.loopEndSamples)
            {
                this.AudioSource.timeSamples -= this.loopLengthSamples;
            }
        }
        #endregion

        #region Methods
        public void InitFromEntry(AudioClip clip, AudioManager.BGMEntry entry)
        {
            this.AudioSource.clip = clip;
            this.LoopStartSeconds = entry.LoopStart;
            this.LoopEndSeconds = entry.LoopEnd;

            var frequency = clip.frequency;
            this.loopStartSamples = (int)(frequency * entry.LoopStart);
            this.loopEndSamples = (int)(frequency * entry.LoopEnd);
            this.loopLengthSamples = this.loopEndSamples - this.loopStartSamples;
            this.AudioSource.loop = entry.BuiltinLoop;

            if (entry.TestStart > 0.0f)
            {
                this.AudioSource.timeSamples = (int)(clip.frequency * entry.TestStart);
            }
        }
        #endregion
    }
}