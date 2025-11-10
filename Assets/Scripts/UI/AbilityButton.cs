using UnityEngine;
using TMPro;

#nullable enable

namespace Orbits
{
    public class AbilityButton : MonoBehaviour
    {
        #region Fields
        public TMP_Text ButtonText;
        public RectTransform Cooldown;
        public Ability Ability;
        #endregion

        #region Unity Methods
        private void Start()
        {
            this.ButtonText.text = this.Ability.Name;
        }

        private void Update()
        {
            var cooldown = this.Ability.CooldownPercent;
            this.Cooldown.gameObject.SetActive(cooldown < 1.0f);
            this.Cooldown.anchorMin = new Vector2(1.0f - cooldown, this.Cooldown.anchorMin.y);
        }
        #endregion

        #region Methods
        public void Activate()
        {
            GameManager.Instance.TriggerAbility(this.Ability);
        }
        #endregion
    }
}