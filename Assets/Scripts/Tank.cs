using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#nullable enable

namespace Orbits
{
    [Serializable]
    public class WeaponLevel
    {
        public WeaponType WeaponType;
        public int Base;
        public int Boosted;

        public int Level => this.Base + this.Boosted;

        public void SetLevels(int baseLevel, int boostedLevel)
        {
            this.Base = baseLevel;
            this.Boosted = boostedLevel;
        }
    }

    [Serializable]
    public class TankFirePoint
    {
        public Transform Base;
        public Transform FirePoint;
        public float Angle;

        public void UpdateTransforms()
        {
            var barrelRotation = Quaternion.Euler(90.0f, this.Angle, 0.0f);
            this.Base.transform.localRotation = barrelRotation;
        }
    }

    public class Tank : MonoBehaviour
    {
        #region Fields
        public delegate void UpgradeHandler(WeaponType weaponType, int level);
        public event UpgradeHandler? OnWeaponUpgrade;

        public string TankName = "";
        public Target Target;
        public GameObject Planet;
        public float PlanetRadius;
        public ProjectileWeapon? CurrentWeapon;
        public WeaponType CurrentWeaponType = WeaponType.Bolt;
        public List<WeaponLevel> WeaponLevels = new();
        public ITankContainer ParentContainer;

        public bool InputFire = false;

        public Vector3 FireAt;
        public List<TankFirePoint> Barrels = new();
        public GameObject? SpawnEffect;
        public GameObject? UpgradeEffect;
        public GameObject? Glow;

        public bool FireSplit = false;
        #endregion

        #region Unity Methods
        private void Start()
        {
            if (string.IsNullOrWhiteSpace(this.TankName))
            {
                this.TankName = TankNames.GetRandomName();
            }

            this.UpdateWeapon();
            foreach (var barrel in this.Barrels)
            {
                barrel.UpdateTransforms();
            }

            this.DoEffect(this.SpawnEffect);
            GameOptions.OnChange += this.OnGameOptionChange;
        }

        private void Update()
        {
            foreach (var barrel in this.Barrels)
            {
                var angle = Utils.CalculateBarrelAngleTo(this.transform, this.FireAt, barrel.Base.position);
                barrel.Angle = Mathf.Clamp(angle * Mathf.Rad2Deg, 9, 171);
                barrel.UpdateTransforms();
            }
        }

        private void FixedUpdate()
        {
            if (!this.Target.IsAlive)
            {
                return;
            }

            if (this.CurrentWeapon != null)
            {
                this.CurrentWeapon.InputFire = this.InputFire;
                var dt = Time.fixedDeltaTime;
                this.CurrentWeapon.UpdateWeapon(this.Target, isPlayer: true, fireSplit: this.FireSplit, dt);
            }
        }

        private void OnDestroy()
        {
            GameOptions.OnChange -= this.OnGameOptionChange;
        }
        #endregion

        #region Methods
        public void SetWeapon(WeaponType type)
        {
            if (this.CurrentWeaponType != type)
            {
                this.CurrentWeaponType = type;
                this.UpdateWeapon();
            }
        }

        public void UpdateWeapon()
        {
            if (this.CurrentWeapon != null)
            {
                Destroy(this.CurrentWeapon.gameObject);
            }

            var level = Mathf.Max(0, this.GetWeaponLevel(this.CurrentWeaponType));

            this.CurrentWeapon = EquipmentManager.Instance.SpawnProjectileWeapon(this.CurrentWeaponType, level);
            this.CurrentWeapon.transform.SetParent(this.transform, false);
            this.CurrentWeapon.Init(this.Barrels.Select(b => b.FirePoint));
        }

        public void BoostUpgradeWeapon(Level forLevel)
        {
            this.GetWeaponLevel(this.CurrentWeaponType, out int baseLevel, out int boostedLevel, out int level);
            var maxLevel = EquipmentManager.Instance.GetMaxLevel(this.CurrentWeaponType);
            var newBaseLevel = Mathf.Min(level + forLevel.WeaponLevelsBoosted, maxLevel);
            var newLevel = Mathf.Min(newBaseLevel + boostedLevel, maxLevel);

            if (newBaseLevel != baseLevel)
            {
                if (level != newLevel)
                {
                    this.ChangeWeaponLevel(this.CurrentWeaponType, baseLevel, forLevel.WeaponLevelsBoosted);
                }
                else
                {
                    this.SetWeaponLevel(this.CurrentWeaponType, newBaseLevel, forLevel.WeaponLevelsBoosted);
                }
            }
        }

        public bool UpgradeWeapon(WeaponType type)
        {
            this.GetWeaponLevel(type, out int baseLevel, out int boostedLevel, out int level);

            var maxLevel = EquipmentManager.Instance.GetMaxLevel(type);
            var newBaseLevel = Mathf.Min(baseLevel + 1, maxLevel);
            var newLevel = Mathf.Min(newBaseLevel + boostedLevel, maxLevel);

            if (newBaseLevel != baseLevel)
            {
                if (level != newLevel)
                {
                    this.ChangeWeaponLevel(type, newBaseLevel, boostedLevel);
                }
                else
                {
                    this.SetWeaponLevel(this.CurrentWeaponType, newBaseLevel, boostedLevel);
                }
                return true;
            }

            return false;
        }

        private void ChangeWeaponLevel(WeaponType type, int baseLevel, int boostedLevel)
        {
            this.SetWeaponLevel(type, baseLevel, boostedLevel);
            this.OnWeaponUpgrade?.Invoke(type, baseLevel + boostedLevel);

            if (type == this.CurrentWeaponType)
            {
                this.UpdateWeapon();
            }

            this.DoEffect(this.UpgradeEffect);
        }

        public void DoEffect(GameObject? effectPrefab)
        {
            if (effectPrefab != null)
            {
                var effect = GameObjectPools.Instance.Spawn(effectPrefab);
                effect.transform.SetPositionAndRotation(this.transform.position, this.transform.rotation);
                effect.transform.Rotate(0, 90, 0, Space.Self);
            }
        }

        public int GetWeaponLevel(WeaponType type)
        {
            for (var i = 0; i < this.WeaponLevels.Count; i++)
            {
                if (this.WeaponLevels[i].WeaponType == type)
                {
                    return this.WeaponLevels[i].Level;
                }
            }

            return -1;
        }

        public bool GetWeaponLevel(WeaponType type, out int baseLevel, out int boostedLevel, out int level)
        {
            for (var i = 0; i < this.WeaponLevels.Count; i++)
            {
                if (this.WeaponLevels[i].WeaponType == type)
                {
                    baseLevel = this.WeaponLevels[i].Base;
                    boostedLevel = this.WeaponLevels[i].Boosted;
                    level = this.WeaponLevels[i].Level;
                    return true;
                }
            }

            baseLevel = -1;
            boostedLevel = -1;
            level = -1;
            return false;
        }

        public void SetWeaponLevel(WeaponType type, int baseLevel, int boostedLevel)
        {
            for (var i = 0; i < this.WeaponLevels.Count; i++)
            {
                if (this.WeaponLevels[i].WeaponType == type)
                {
                    this.WeaponLevels[i].SetLevels(baseLevel, boostedLevel);
                    break;
                }
            }
        }

        private void OnGameOptionChange(SettingType type)
        {
            if (type == SettingType.TankGlow && this.Glow != null)
            {
                var value = GameOptions.TankGlow;
                var show = value > Mathf.Epsilon;
                this.Glow.SetActive(show);
            }
        }
        #endregion
    }
}