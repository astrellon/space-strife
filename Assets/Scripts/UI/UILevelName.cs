using System;
using UnityEngine;
using TMPro;

#nullable enable

namespace Orbits
{
    public class UILevelName : MonoBehaviour
    {
        #region Fields
        public TMP_Text Text;
        #endregion

        #region Unity Methods
        private void OnEnable()
        {
            var level = GameManager.Instance.CurrentLevel;
            if (level != null)
            {
                this.Text.text = level.LevelName;
            }
            else
            {
                this.Text.text = "";
            }
        }
        #endregion
    }
}