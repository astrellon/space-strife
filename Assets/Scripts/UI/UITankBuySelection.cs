using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#nullable enable

namespace Orbits
{
    public class UITankBuySelection : MonoBehaviour
    {
        #region Fields
        public List<UITankBuy> Buttons = new();
        #endregion

        #region Unity Methods
        private void OnEnable()
        {
            this.Buttons = this.GetComponentsInChildren<UITankBuy>(true).ToList();
            var level = GameManager.Instance.CurrentLevel;
            if (level != null)
            {
                foreach (var button in this.Buttons)
                {
                    var isActive = EquipmentManager.Instance.TryGetUpgrade(button.Type, out _) &&
                        (PlayerState.Instance.TankLevels.ContainsKey(button.Type) ||
                        level.StartingTank == button.Type);
                    button.gameObject.SetActive(isActive);
                }
            }
        }
        #endregion

        #region Methods
        #endregion
    }
}