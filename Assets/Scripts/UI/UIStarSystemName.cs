using UnityEngine;
using TMPro;

#nullable enable

namespace Orbits
{
    public class UIStarSystemName : MonoBehaviour
    {
        #region Fields
        public StarSystem StarSystem;
        public TMP_Text Text;
        public UIForWorldTarget ForWorldTarget;
        #endregion

        #region Unity Methods
        private void Start()
        {
            this.ForWorldTarget.WorldTarget = this.StarSystem.UIPoint;

            UIManager.Instance.OnStateChange += this.OnStateChange;
            this.gameObject.SetActive(UIManager.Instance.State == InterfaceState.StarSystemSelect);
            this.Text.text = this.StarSystem.StarSystemName;
        }

        private void OnDestroy()
        {
            UIManager.Instance.OnStateChange -= this.OnStateChange;
        }
        #endregion

        #region Methods
        private void OnStateChange(InterfaceState newState, InterfaceState prevState)
        {
            var correctUIState = newState == InterfaceState.StarSystemSelect ||
                (newState == InterfaceState.LevelSelect && UIManager.Instance.CurrentStarSystem == this.StarSystem);
            var starSystemIsUnlocked = PlayerState.Instance.IsStarSystemUnlocked(this.StarSystem.Id);

            this.ForWorldTarget.SetToShow(correctUIState && starSystemIsUnlocked);
        }
        #endregion
    }
}