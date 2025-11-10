using UnityEngine;
using TMPro;

#nullable enable

namespace Orbits
{
    public class UINextLevel : MonoBehaviour
    {
        #region Fields
        public TMP_Text NextLevelText;
        public string WinText;
        public UIChangeState NextLevelButton;
        public GameObject FinishedGameButton;
        #endregion

        #region Unity Methods
        #endregion

        #region Methods
        public void Init(string text, bool hasFinishedGame, bool returnToStarSystem)
        {
            this.NextLevelButton.gameObject.SetActive(!hasFinishedGame);
            this.NextLevelButton.State = returnToStarSystem ? InterfaceState.StarSystemSelect : InterfaceState.LevelSelect;

            if (hasFinishedGame)
            {
                text = this.WinText + "\n" + text;
            }
            this.NextLevelText.text = text;

            this.FinishedGameButton.SetActive(hasFinishedGame);
        }
        #endregion
    }
}