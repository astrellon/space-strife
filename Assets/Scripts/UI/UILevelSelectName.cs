using System;
using UnityEngine;
using TMPro;

#nullable enable

namespace Orbits
{
    public class UILevelSelectName : MonoBehaviour
    {
        #region Fields
        public LevelContainer LevelContainer;
        public StarSystem ForStarSystem;
        public UIForWorldTarget ForWorldTarget;
        public TMP_Text NameText;
        public TMP_Text DescriptionText;
        public float SmallFontSize = 20.0f;
        public float LargeFontSize = 32.0f;
        private UILevelSelectContainer uiParent;
        #endregion

        #region Unity Methods
        private void Start()
        {
            this.ForWorldTarget.OnBeforeDisable += this.OnBeforeDisable;
            this.DescriptionText.gameObject.SetActive(false);
        }

        private void Update()
        {
            this.NameText.fontSize = Mathf.Lerp(this.SmallFontSize, this.LargeFontSize, this.ForWorldTarget.LerpToUIPosition);
            if (this.ForWorldTarget.LerpToUIPosition > 0.0f)
            {
                if (!this.DescriptionText.gameObject.activeSelf)
                {
                    this.DescriptionText.text = this.LevelContainer.GetDescription();
                    this.DescriptionText.gameObject.SetActive(true);
                }

                var scale = Mathf.Clamp01((this.ForWorldTarget.LerpToUIPosition - 0.5f) / 0.5f);
                this.DescriptionText.transform.localScale = Vector3.one * Easing.Back.Out(scale);
            }
        }
        #endregion

        #region Methods
        private void OnBeforeDisable()
        {
            UIManager.Instance.OnStateChange -= this.OnStateChange;
            GameObjectPools.Instance.Release(this.uiParent.NamePrefab.gameObject, this.gameObject);
            this.DescriptionText.text = "";
            this.DescriptionText.gameObject.SetActive(false);
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

            this.NameText.text = this.LevelContainer.LevelPrefab.LevelName;
        }

        private void OnStateChange(InterfaceState newState, InterfaceState prevState)
        {
            this.CheckForShow(newState);
        }

        private void CheckForShow(InterfaceState state)
        {
            var correctUIState = state == InterfaceState.LevelSelect && UIManager.Instance.CurrentStarSystem == this.ForStarSystem;
            var currentlySelected = state == InterfaceState.SelectedLevel && UIManager.Instance.CurrentLevelSelected == this.LevelContainer;
            var levelUnlocked = PlayerState.Instance.IsLevelUnlocked(this.ForStarSystem.Id, this.LevelContainer.LevelPrefab.Id);
            this.ForWorldTarget.ToUIPosition = currentlySelected && levelUnlocked;
            this.ForWorldTarget.SetToShow((correctUIState || currentlySelected) && levelUnlocked);
        }
        #endregion
    }
}