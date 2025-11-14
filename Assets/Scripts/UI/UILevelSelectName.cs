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
        public TMP_Text LevelUnlockHeaderText;
        public Transform LevelUnlockItemsParent;
        public Transform LevelUnlockParent;
        public UILevelUnlock LevelUnlockPrefab;
        public float SmallFontSize = 20.0f;
        public float LargeFontSize = 32.0f;
        private UILevelSelectContainer uiParent;
        #endregion

        #region Unity Methods
        private void Start()
        {
            this.ForWorldTarget.OnBeforeDisable += this.OnBeforeDisable;
            this.DescriptionText.gameObject.SetActive(false);
            this.LevelUnlockParent.gameObject.SetActive(false);
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
                if (!this.LevelUnlockParent.gameObject.activeSelf)
                {
                    this.SetupUnlocks();
                    this.LevelUnlockParent.gameObject.SetActive(true);
                }

                var scale = Mathf.Clamp01((this.ForWorldTarget.LerpToUIPosition - 0.5f) / 0.5f);
                var easedScale = Vector3.one * Easing.Back.Out(scale);
                this.DescriptionText.transform.localScale = easedScale;
                this.LevelUnlockParent.transform.localScale = easedScale;
            }
            else
            {
                if (this.DescriptionText.gameObject.activeSelf)
                {
                    this.DescriptionText.gameObject.SetActive(false);
                }
                if (this.LevelUnlockParent.gameObject.activeSelf)
                {
                    this.LevelUnlockParent.gameObject.SetActive(false);
                }
            }
        }
        #endregion

        #region Methods
        private void SetupUnlocks()
        {
            Utils.RemoveAllChildren(this.LevelUnlockItemsParent);

            var levelInfo = this.GetPlayerStateLevelInfo();
            if (PlayerState.Instance.LevelRepeatUnlocked)
            {
                if (levelInfo.HasFinishedLevelMaxHealth)
                {
                    this.LevelUnlockHeaderText.text = "Upgrades Unlocked:";
                }
                else
                {
                    this.LevelUnlockHeaderText.text = "Unlock With Full Health:";
                }

                var level = UIManager.Instance.CurrentLevelSelected.LevelPrefab;
                if (level != null)
                {
                    foreach (var unlock in level.LevelFullHealthUnlocks)
                    {
                        var create = Instantiate(this.LevelUnlockPrefab);
                        create.transform.SetParent(this.LevelUnlockItemsParent, false);
                        create.transform.localScale = Vector3.one;
                        create.Data = unlock;
                    }
                }
            }
            else
            {
                if (levelInfo.HasFinishedLevelBefore)
                {
                    this.LevelUnlockHeaderText.text = "Planet Defended";
                }
                else
                {
                    this.LevelUnlockHeaderText.text = "";
                }

                this.LevelUnlockParent.gameObject.SetActive(false);
            }
        }

        private PlayerStateLevelInfo GetPlayerStateLevelInfo()
        {
            var starSystem = UIManager.Instance.CurrentStarSystem;
            var level = UIManager.Instance.CurrentLevelSelected.LevelPrefab;

            return PlayerStateLevelInfo.CreateFrom(starSystem, level, PlayerState.Instance);
        }

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