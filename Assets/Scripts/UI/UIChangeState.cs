using UnityEngine;

#nullable enable

namespace Orbits
{
    public class UIChangeState : MonoBehaviour
    {
        #region Fields
        public InterfaceState State;
        #endregion

        #region Methods
        public void Invoke()
        {
            UIManager.Instance.State = this.State;
        }
        #endregion
    }
}