using UnityEngine;
using TMPro;
using System.Collections.Generic;

#nullable enable

namespace Orbits
{
    public class UITankUpgrade : MonoBehaviour
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
        public GameObject FullyUpgraded;
        public GameObject SellTankPanel;
        public TMP_Text SellTankText;

        public Color FromDamage;
        public Color ToDamage;
        public Color FromSpeed;
        public Color ToSpeed;
        public Color FromCooldown;
        public Color ToCooldown;
        public Color UpgradeColour;

        public StateType State;

        public Level? CurrentLevel { get; private set; }
        private Tank? currentTank;
        private int currentWeaponLevel;
        private int currentWeaponLevelBoosted;
        #endregion

        #region Unity Methods
        private void Start()
        {
            this.UpdateGraphics();
            UIManager.Instance.OnStateChange += this.OnUIStateChange;
            GameManager.Instance.OnLevelLoad += this.OnLevelLoaded;
        }
        #endregion

        #region Methods
        public void SetTankIndex(int tankIndex)
        {
            if (this.TankIndex != tankIndex)
            {
                this.TankIndex = tankIndex;
                this.UpdateGraphics();
            }
        }

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

            if (this.CurrentLevel.BuyTank(type, this.TankIndex))
            {
                this.UpdateGraphics();
                PlayerState.Instance.MarkUpgradeTutorial();
                return true;
            }

            return false;
        }

        public void SellTank()
        {
            if (this.CurrentLevel == null)
            {
                return;
            }

            if (this.CurrentLevel.SellTank(this.TankIndex))
            {
                this.UpdateGraphics();
            }
        }

        public void UpgradeTank()
        {
            if (this.currentTank == null || this.CurrentLevel == null)
            {
                return;
            }

            if (this.CurrentLevel.UpgradeTank(this.currentTank))
            {
                this.UpdateGraphics();
                PlayerState.Instance.MarkUpgradeTutorial();
            }
        }

        public void UpdateGraphics()
        {
            this.UpdateState();

            if (this.State == StateType.Unknown || this.State == StateType.NotAvailable)
            {
                return;
            }

            var showCreate = this.State == StateType.BuyTank;
            var showUpgrade = this.State == StateType.BuyUpgrade;
            var showFullyUpgraded = this.State == StateType.FullyUpgraded;
            var showSell = !showCreate;

            if (this.currentTank != null)
            {
                this.TankName.text = this.currentTank.TankName;
                this.TankType.text = $"{this.currentTank.CurrentWeaponType} Lvl {this.currentWeaponLevel + 1}";
            }
            else
            {
                this.TankName.text = "";
                this.TankType.text = "";
            }

            this.CreateTankPanel.SetActive(showCreate);
            this.UpgradeTankPanel.SetActive(showUpgrade);
            this.FullyUpgraded.SetActive(showFullyUpgraded);
            this.SellTankPanel.SetActive(showSell);

            if (showSell && this.currentTank != null)
            {
                var cost = EquipmentManager.Instance.GetCostOfTank(this.currentTank.CurrentWeaponType, this.currentWeaponLevel);
                this.SellTankText.text = $"Sell: ${Utils.ColouredText(this.UpgradeColour, cost)}";
            }
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
                this.currentWeaponLevelBoosted = boostedLevel;
            }

            if (PlayerState.Instance.TryGetUpgrade(this.currentTank.CurrentWeaponType, out var upgradeList))
            {
                if (this.currentWeaponLevel < upgradeList.Levels.Count)
                {
                    this.UpgradeText.text = GetUpgradeText(upgradeList, this.currentWeaponLevel);
                    this.State = StateType.BuyUpgrade;
                }
            }
        }

        private string GetUpgradeText(WeaponUpgradeList upgradeList, int currentWeaponLevel)
        {
            var nextLevelCost = upgradeList.Levels[currentWeaponLevel].Cost;

            if (!EquipmentManager.Instance.TryGetProjectileWeaponPrefab(upgradeList.Type, currentWeaponLevel, out var currentWeapon) ||
                !EquipmentManager.Instance.TryGetProjectileWeaponPrefab(upgradeList.Type, currentWeaponLevel + 1, out var nextWeapon))
            {
                return "Cannot upgrade";
            }

            var result = new List<string>
            {
                $"Upgrade\n<b>${Utils.ColouredText(this.UpgradeColour, nextLevelCost)}</b>\n"
            };

            if (nextWeapon.Speed != currentWeapon.Speed)
            {
                result.Add($"Speed:\n{Utils.FromToColours(currentWeapon.Speed, nextWeapon.Speed, this.FromSpeed, this.ToSpeed)}");
            }
            if (nextWeapon.Damage != currentWeapon.Damage)
            {
                result.Add($"Damage:\n{Utils.FromToColours(currentWeapon.Damage, nextWeapon.Damage, this.FromDamage, this.ToDamage)}");
            }
            if (nextWeapon.Cooldown != currentWeapon.Cooldown)
            {
                result.Add($"Cooldown:\n{Utils.FromToColours(currentWeapon.Cooldown, nextWeapon.Cooldown, this.FromCooldown, this.ToCooldown)}");
            }

            return string.Join("\n", result);
        }

        private void OnUIStateChange(InterfaceState newState, InterfaceState prevState)
        {
            if (newState == InterfaceState.Equipment)
            {
                this.UpdateGraphics();
            }
        }

        private void OnLevelLoaded(Level level)
        {
            this.TankIndex = 0;
        }
        #endregion
    }
}