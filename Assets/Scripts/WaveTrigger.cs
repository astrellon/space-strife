using UnityEngine;

#nullable enable

namespace Orbits
{
    public class WaveTrigger : MonoBehaviour
    {
        #region Fields
        public bool Triggered;

        public delegate void TriggerHandler();
        public event TriggerHandler? OnTriggered;
        #endregion

        #region Methods
        public void SetTriggered()
        {
            if (!this.Triggered)
            {
                this.Triggered = true;
                this.OnTriggered?.Invoke();
            }
        }
        #endregion
    }
}