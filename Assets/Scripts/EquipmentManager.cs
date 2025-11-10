using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using UnityEngine;

#nullable enable

namespace Orbits
{
    [Serializable]
    public class TankPurchasePrefab
    {
        public WeaponType Type;
        public int Cost;
        public Tank? Prefab;
    }

    [Serializable]
    public class WeaponUpgradePrefab
    {
        public int Cost;
    }

    [Serializable]
    public class WeaponUpgradeList
    {
        public WeaponType Type;
        public List<WeaponUpgradePrefab> Levels = new();

        public WeaponUpgradeList LimitLevels(int maxLevels)
        {
            if (maxLevels >= this.Levels.Count)
            {
                return this;
            }

            return new WeaponUpgradeList
            {
                Type = this.Type,
                Levels = this.Levels.Take(maxLevels).ToList()
            };
        }
    }

    [Serializable]
    public class ProjectileWeaponPrefabs
    {
        public WeaponType Type;
        public List<ProjectileWeapon> Levels = new();
    }


    public class EquipmentManager : MonoBehaviour
    {
        #region Fields
        public static EquipmentManager Instance;
        public List<WeaponUpgradeList> UpgradeLevels = new();
        public List<TankPurchasePrefab> TankPrefabs = new();
        public List<ProjectileWeaponPrefabs> ProjectileWeapons = new();
        public List<WeaponLevel> StartingWeaponLevels = new();

        public GameObject? SellEffect;
        #endregion

        #region Unity Methods
        private void Awake()
        {
            Instance = this;
        }
        #endregion

        #region Methods
        public int GetStartingWeaponLevel(WeaponType weaponType)
        {
            foreach (var info in this.StartingWeaponLevels)
            {
                if (info.WeaponType == weaponType)
                {
                    return info.Level;
                }
            }

            return 0;
        }

        public bool TryGetTank(WeaponType type, [NotNullWhen(true)] out TankPurchasePrefab? result)
        {
            if (PlayerState.Instance.TankLevels.ContainsKey(type) ||
                (GameManager.Instance.CurrentLevel != null && GameManager.Instance.CurrentLevel.StartingTank == type))
            {
                foreach (var upgrade in this.TankPrefabs)
                {
                    if (upgrade.Type == type)
                    {
                        result = upgrade;
                        return true;
                    }
                }
            }

            result = null;
            return false;
        }

        public bool TryGetUpgrade(WeaponType type, [NotNullWhen(true)] out WeaponUpgradeList? result)
        {
            foreach (var upgrade in this.UpgradeLevels)
            {
                if (upgrade.Type == type)
                {
                    result = upgrade;
                    return true;
                }
            }

            result = null;
            return false;
        }

        public ProjectileWeapon SpawnProjectileWeapon(WeaponType type, int level)
        {
            if (TryGetProjectileWeaponPrefab(type, level, out var weaponPrefab))
            {
                return Instantiate(weaponPrefab);
            }

            throw new Exception($"Unable to spawn weapon: {type} @ level {level}");
        }

        public bool TryGetProjectileWeaponPrefab(WeaponType type, int level, [NotNullWhen(true)] out ProjectileWeapon? prefab)
        {
            for (var i = 0; i < this.ProjectileWeapons.Count; i++)
            {
                var weapons = this.ProjectileWeapons[i];
                if (weapons.Type == type)
                {
                    if (level >= weapons.Levels.Count)
                    {
                        prefab = null;
                        return false;
                    }

                    prefab = weapons.Levels[level];
                    return true;
                }
            }

            prefab = null;
            return false;
        }

        public int GetCostOfTank(WeaponType type, int level)
        {
            if (!this.TryGetTank(type, out var tankPrefab) ||
                !this.TryGetUpgrade(type, out var upgrades))
            {
                return 0;
            }

            var cost = tankPrefab.Cost;
            var maxIndex = Math.Min(level, upgrades.Levels.Count);
            for (var i = 0; i < maxIndex; i++)
            {
                cost += upgrades.Levels[i].Cost;
            }
            return cost;
        }

        public int GetMaxLevel(WeaponType type)
        {
            for (var i = 0; i < this.ProjectileWeapons.Count; i++)
            {
                var weapons = this.ProjectileWeapons[i];
                if (weapons.Type == type)
                {
                    return weapons.Levels.Count - 1;
                }
            }

            return -1;
        }
        #endregion
    }
}