using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#nullable enable

namespace Orbits
{
    public class UIUpgrades : MonoBehaviour
    {
        #region Fields
        public List<UITankUpgradePanel> Panels = new();
        #endregion

        #region Unity Methods
        private void OnEnable()
        {
            if (this.Panels.Count == 0)
            {
                this.Panels = this.GetComponentsInChildren<UITankUpgradePanel>(true).ToList();
            }

            foreach (var panel in this.Panels)
            {
                panel.UpdateGraphics();
            }
        }
        #endregion

        #region Methods
        #endregion
    }
}