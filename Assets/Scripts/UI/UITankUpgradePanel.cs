using UnityEngine;
using TMPro;
using System.Text;

#nullable enable

namespace Orbits
{
    public class UITankUpgradePanel : MonoBehaviour
    {
        public enum StateType
        {
            Unknown, NotAvailable, BuyTank, BuyUpgrade, FullyUpgraded
        }

        #region Fields
        public int TankIndex;
        public TMP_Text TankName;
        public TMP_Text TankType;
        public TMP_Text UpgradeText;
        public GameObject CreateTankPanel;
        public GameObject UpgradeTankPanel;

        public StateType State;

        public Level? CurrentLevel { get; private set; }
        private Tank? currentTank;
        private int currentWeaponLevel;
        #endregion

        #region Unity Methods
        private void Start()
        {
            this.UpdateGraphics();
        }

        private void Update()
        {
            this.UpdateGraphics();
        }
        #endregion

        #region Methods
        public bool CanBuyTank(WeaponType type)
        {
            if (this.CurrentLevel == null || !EquipmentManager.Instance.TryGetTank(type, out var tankPrefab))
            {
                return false;
            }

            return this.CurrentLevel.Money >= tankPrefab.Cost;
        }

        public bool BuyTank(WeaponType type)
        {
            if (this.CurrentLevel == null)
            {
                return false;
            }

            return this.CurrentLevel.BuyTank(type, this.TankIndex);
        }

        public void UpgradeTank()
        {
            if (this.currentTank == null || this.CurrentLevel == null)
            {
                return;
            }

            this.CurrentLevel.UpgradeTank(this.currentTank);
        }

        public void UpdateGraphics()
        {
            this.UpdateState();

            if (this.State == StateType.Unknown || this.State == StateType.NotAvailable)
            {
                this.gameObject.SetActive(false);
                return;
            }

            this.gameObject.SetActive(true);

            var showCreate = this.State == StateType.BuyTank;
            var showUpgrade = this.State == StateType.BuyUpgrade;

            this.TankName.text = this.currentTank != null ? this.currentTank.TankName : "";
            var tankType = this.currentTank != null ? this.currentTank.CurrentWeaponType + " " : "";
            this.TankType.text = $"{tankType}Lvl {this.currentWeaponLevel + 1}";

            this.CreateTankPanel.SetActive(showCreate);
            this.UpgradeTankPanel.SetActive(showUpgrade);
        }

        private void UpdateState()
        {
            this.CurrentLevel = GameManager.Instance.CurrentLevel;
            this.currentTank = null;
            if (this.CurrentLevel == null)
            {
                this.State = StateType.Unknown;
                return;
            }

            var player = this.CurrentLevel.Player;
            if (this.TankIndex >= player.MaxTanks)
            {
                this.State = StateType.NotAvailable;
                return;
            }

            if (this.TankIndex >= player.Tanks.Count)
            {
                this.State = StateType.BuyTank;
                return;
            }

            this.currentTank = player.Tanks[this.TankIndex];
            if (this.currentTank == null)
            {
                this.State = StateType.BuyTank;
                return;
            }

            this.State = StateType.FullyUpgraded;
            this.UpgradeText.text = "Fully\nUpgraded";
            if (this.currentTank.GetWeaponLevel(this.currentTank.CurrentWeaponType, out int baseLevel, out int boostedLevel, out int level))
            {
                this.currentWeaponLevel = baseLevel;
            }

            if (PlayerState.Instance.TryGetUpgrade(this.currentTank.CurrentWeaponType, out var upgradeList))
            {
                if (this.currentWeaponLevel < upgradeList.Levels.Count)
                {
                    var nextLevel = upgradeList.Levels[this.currentWeaponLevel];
                    this.UpgradeText.text = $"Upgrade\n${nextLevel.Cost}";
                    this.State = StateType.BuyUpgrade;
                }
            }
        }
        #endregion
    }
}