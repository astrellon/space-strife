using System.Collections;
using UnityEngine;

#nullable enable

namespace Orbits
{
    public class TimeTriggerWave : WaveTrigger
    {
        #region Fields
        public Behaviour Target;
        public float TimeUntilEnabled = 10.0f;
        #endregion

        #region Unity Methods
        void OnEnable()
        {
            StartCoroutine(this.WaitUntilEnabled());
        }
        #endregion

        #region Methods
        private IEnumerator WaitUntilEnabled()
        {
            yield return new WaitForSeconds(this.TimeUntilEnabled);
            if (!this.Triggered)
            {
                this.Target.enabled = true;
            }
        }
        #endregion
    }
}