using System.Collections;
using UnityEngine;

#nullable enable

namespace Orbits
{
    public class TimeTriggerAfterWave : WaveTrigger
    {
        #region Fields
        public Wave AfterWave;
        public Component Target;
        public float TimeUntilEnabled = 10.0f;
        #endregion

        #region Unity Methods
        private void Update()
        {
            if (!this.Triggered && this.AfterWave.enabled)
            {
                this.Triggered = true;
                StartCoroutine(this.WaitUntilEnabled());
            }
        }
        #endregion

        #region Methods
        private IEnumerator WaitUntilEnabled()
        {
            yield return new WaitForSeconds(this.TimeUntilEnabled);
            this.enabled = true;
        }
        #endregion
    }
}