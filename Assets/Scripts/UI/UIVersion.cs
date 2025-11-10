using UnityEngine;
using TMPro;

#nullable enable

namespace Orbits
{
    public class UIVersion : MonoBehaviour
    {
        #region Fields
        public TMP_Text Text;
        #endregion

        #region Unity Methods
        private void Start()
        {
            this.Text.text = $"v{Application.version}";
        }
        #endregion
    }
}