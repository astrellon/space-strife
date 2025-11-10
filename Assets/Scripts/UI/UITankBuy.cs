using UnityEngine;
using TMPro;

#nullable enable

namespace Orbits
{
    [DefaultExecutionOrder(10)]
    public class UITankBuy : MonoBehaviour
    {
        #region Fields
        public TMP_Text Name;
        public TMP_Text Cost;
        public UITankUpgrade Parent;
        public WeaponType Type;
        #endregion

        #region Methods
        private void Start()
        {
            if (GameManager.Instance.CurrentLevel != null && EquipmentManager.Instance.TryGetTank(this.Type, out var tankBuy))
            {
                this.Cost.text = Utils.ColouredText(this.Parent.UpgradeColour, "$" + tankBuy.Cost);
            }
        }

        public void Buy()
        {
            this.Parent.BuyTank(this.Type);
        }
        #endregion
    }
}