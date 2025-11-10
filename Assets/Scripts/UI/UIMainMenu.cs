using UnityEngine;

#nullable enable

namespace Orbits
{
    public class UIMainMenu : MonoBehaviour
    {
        #region Fields
        public GameObject ExitButton;
        #endregion

        #region Unity Methods
        private void OnEnable()
        {
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                this.ExitButton.SetActive(false);
            }
        }
        #endregion
    }
}