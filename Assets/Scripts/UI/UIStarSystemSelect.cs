using UnityEngine;

#nullable enable

namespace Orbits
{
    public class UIStarSystemSelect : MonoBehaviour
    {
        #region Fields
        public StarSystem StarSystem;
        public UIForWorldTarget ForWorldTarget;
        #endregion

        #region Unity Methods
        private void Start()
        {
            this.ForWorldTarget.WorldTarget = this.StarSystem.UIPoint;

            UIManager.Instance.OnStateChange += this.OnStateChange;
            this.gameObject.SetActive(UIManager.Instance.State == InterfaceState.StarSystemSelect);
        }

        private void OnDestroy()
        {
            UIManager.Instance.OnStateChange -= this.OnStateChange;
        }
        #endregion

        #region Methods
        private void OnStateChange(InterfaceState newState, InterfaceState prevState)
        {
            var correctUIState = newState == InterfaceState.StarSystemSelect;
            var starSystemUnlocked = PlayerState.Instance.IsStarSystemUnlocked(this.StarSystem.Id);
            this.ForWorldTarget.SetToShow(correctUIState && starSystemUnlocked);
        }

        public void Select()
        {
            UIManager.Instance.CurrentStarSystem = this.StarSystem;
            UIManager.Instance.State = InterfaceState.LevelSelect;
        }
        #endregion
    }
}