using System;
using UnityEngine;

#nullable enable

namespace Orbits
{
    public class UILevelSelect2 : MonoBehaviour
    {
        #region Fields
        public LevelContainer LevelContainer;
        public StarSystem ForStarSystem;
        public UIForWorldTarget ForWorldTarget;
        private UILevelSelectContainer uiParent;
        #endregion

        #region Unity Methods
        private void Start()
        {
            this.ForWorldTarget.OnBeforeDisable += this.OnBeforeDisable;
        }
        #endregion

        #region Methods
        private void OnBeforeDisable()
        {
            UIManager.Instance.OnStateChange -= this.OnStateChange;
            GameObjectPools.Instance.Release(this.uiParent.SelectPrefab.gameObject, this.gameObject);
            this.uiParent.Remove(this);
        }

        public void Init(UILevelSelectContainer uiParent, LevelContainer levelContainer, StarSystem forStarSystem)
        {
            this.uiParent = uiParent;
            this.LevelContainer = levelContainer;
            this.ForStarSystem = forStarSystem;
            this.ForWorldTarget.WorldTarget = this.LevelContainer.Target;

            this.CheckForShow(UIManager.Instance.State);
            UIManager.Instance.OnStateChange += this.OnStateChange;
        }

        private void OnStateChange(InterfaceState newState, InterfaceState prevState)
        {
            this.CheckForShow(newState);
        }

        private void CheckForShow(InterfaceState state)
        {
            var correctUIState = state == InterfaceState.LevelSelect && UIManager.Instance.CurrentStarSystem == this.ForStarSystem;
            var levelUnlocked = PlayerState.Instance.IsLevelUnlocked(this.ForStarSystem.Id, this.LevelContainer.LevelPrefab.Id);
            this.ForWorldTarget.SetToShow(correctUIState && levelUnlocked);
        }

        public void Select()
        {
            UIManager.Instance.CurrentLevelSelected = this.LevelContainer;
            UIManager.Instance.State = InterfaceState.SelectedLevel;
        }
        #endregion
    }
}