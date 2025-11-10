using UnityEngine;
using UnityEngine.EventSystems;

#nullable enable

namespace Orbits
{
    public class AutoSelectOnEnable : MonoBehaviour
    {
        #region Fields
        #endregion

        #region Unity Methods
        private void OnEnable()
        {
            EventSystem.current.SetSelectedGameObject(this.gameObject);
        }
        #endregion
    }
}