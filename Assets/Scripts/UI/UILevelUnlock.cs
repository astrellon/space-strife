using TMPro;
using UnityEngine;

#nullable enable

namespace Orbits
{
    public class UILevelUnlock : MonoBehaviour
    {
        #region Fields
        public LevelUnlock Data;
        public TMP_Text Text;
        private bool isUnlocked;
        #endregion

        #region Unity Methods
        private void Start()
        {
            var message = "";
            if (this.Data.Type == LevelUnlock.UnlockType.UpgradeWeapon)
            {
                this.isUnlocked = PlayerState.Instance.HasTankUpgrade(this.Data.WeaponType, this.Data.WeaponUpgradeId);
                message = $"{Utils.ColouredText(UIManager.Instance.WeaponColour, this.Data.WeaponType.ToString())} {Utils.ColouredText(UIManager.Instance.UpgradeColour, this.Data.WeaponUpgradeId)}";
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            if (!this.isUnlocked)
            {
                this.Text.color = new Color(1, 1, 1, 0.45f);
            }

            this.Text.text = message;
        }
        #endregion

        #region Methods
        #endregion
    }
}