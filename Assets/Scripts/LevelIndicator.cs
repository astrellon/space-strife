using UnityEngine;
using TMPro;

#nullable enable

namespace Orbits
{
    public class LevelIndicator : MonoBehaviour
    {
        #region Fields
        public TMP_Text Text;
        public Color MoneyColour;
        public Color HealthColour;
        #endregion

        #region Unity Methods
        void Update()
        {
            var currentLevel = GameManager.Instance.CurrentLevel;
            var currentLevelContainer = GameManager.Instance.CurrentLevelContainer;
            if (currentLevel == null || currentLevelContainer == null)
            {
                this.Text.text = "";
                return;
            }

            var message = "";
            if (currentLevelContainer.ShipFocusedLevel)
            {
                var shipHealth = Mathf.RoundToInt(currentLevelContainer.Ship.Target.CurrentHealth);
                var shipMaxHealth = Mathf.RoundToInt(currentLevelContainer.Ship.Target.MaxHealth);
                message = $"Ship Health: {Utils.ColouredText(this.HealthColour, shipHealth)}/{Utils.ColouredText(this.HealthColour, shipMaxHealth)}\n";
            }
            else
            {
                var planetHealth = Mathf.RoundToInt(currentLevel.PlanetCurrentHealth);
                message = $"Planet Health: {Utils.ColouredText(this.HealthColour, planetHealth)}\n";
            }

            message += Utils.ColouredText(this.MoneyColour, "$" + currentLevel.Money);

            if (AlienShip.Instance != null)
            {
                message += $"\n\nAlien Ship Health: {Utils.ColouredText(this.HealthColour, AlienShip.Instance.Target.CurrentHealth)}";
            }

            this.Text.text = message;
        }
        #endregion
    }
}