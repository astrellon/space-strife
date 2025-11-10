using UnityEngine;
using TMPro;
using UnityEngine.UI;

#nullable enable

namespace Orbits
{
    public class CharacterSelect : MonoBehaviour
    {
        #region Fields
        public TMP_Text CharacterName;
        public TMP_Text CharacterNameShadow;
        public TMP_Text Description;
        public TMP_Text ButtonText;
        public Image Portrait;
        public RectTransform PortraitRect;
        public GameCharacter Character;
        public RectTransform Holder;
        public bool Show;
        public float ShowSpeed = 0.6f;
        public float ShowOffset = 0.0f;
        private float showAmount = 0.0f;
        private float showCountdown = 0.0f;
        private Easing.Function easing;

        public bool IsShowing => this.Show || this.showAmount > 0.0f;
        #endregion

        #region Unity Methods
        private void Start()
        {
            this.CharacterName.text = this.Character.NameWithColour;
            this.CharacterNameShadow.text = this.Character.Name;
            this.Description.text = this.Character.Description;
            this.Portrait.sprite = this.Character.Portrait;
            this.Holder.anchoredPosition3D = new Vector3(0, -420.0f, 0);
            this.easing = Easing.Map[this.Character.PortraitEasing];
        }

        private void OnEnable()
        {
            this.UpdateButton();
            UIManager.Instance.OnCharacterSelected += this.UpdateButton;
        }

        private void OnDisable()
        {
            UIManager.Instance.OnCharacterSelected -= this.UpdateButton;
        }

        private void Update()
        {
            if (this.Show && this.showCountdown < this.ShowOffset)
            {
                this.showCountdown = Mathf.Clamp(this.showCountdown + Time.unscaledDeltaTime, 0, this.ShowOffset);
            }
            else if (!this.Show && this.showCountdown > 0.0f)
            {
                this.showCountdown = Mathf.Clamp(this.showCountdown - Time.unscaledDeltaTime, 0, this.ShowOffset);
            }
            else
            {
                var delta = (this.Show ? Time.unscaledDeltaTime : -Time.unscaledDeltaTime) / this.ShowSpeed;
                this.showAmount = Mathf.Clamp01(this.showAmount + delta);
                this.Holder.anchoredPosition3D = new Vector3(0, this.easing(this.showAmount) * 420.0f - 420.0f, 0);
            }
        }
        #endregion

        #region Methods
        public void SetShow()
        {
            this.Show = true;
            this.gameObject.SetActive(true);
        }

        public void UpdateButton()
        {
            var currentlySelected = UIManager.Instance.CurrentCharactersSelected.Contains(this.Character);
            this.Portrait.color = currentlySelected ? UICharacterPortrait.Selected : UICharacterPortrait.Unselected;
            this.ButtonText.text = currentlySelected ? "Unselect" : "Select";
        }

        public void Select()
        {
            UIManager.Instance.ToggleSelectedCharacter(this.Character);
        }
        #endregion
    }
}