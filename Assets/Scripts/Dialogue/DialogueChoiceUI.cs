using UnityEngine;
using TMPro;

namespace Orbits
{
    public class DialogueChoiceUI : MonoBehaviour
    {
        #region Fields
        public string ChoiceText;
        public int ChoiceIndex;
        public LevelVM LevelVM;

        public TMP_Text ButtonText;
        #endregion

        #region Unity Methods
        void Start()
        {
            this.ButtonText.text = this.ChoiceText;
        }
        #endregion

        #region Methods
        public void SelectChoice()
        {
            this.LevelVM.SelectChoice(this.ChoiceIndex);
        }
        #endregion
    }
}