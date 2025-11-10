using System.Collections.Generic;
using UnityEngine;

#nullable enable

namespace Orbits
{
    public class Ship : MonoBehaviour
    {
        #region Fields
        public Target? Target;
        public ProjectileWeapon? CurrentWeapon;
        public int ShipId;

        public bool AutoFireAtInput = true;
        public bool InputFire = false;
        public bool HasFireAtPoint = false;
        public Vector3 FireAt;

        public int WeaponLevel;
        public WeaponType WeaponType = WeaponType.Bolt;
        public List<Transform> BarrelFirePoints = new();
        #endregion

        #region Unity Methods
        private void Start()
        {
            this.UpdateWeapon();
        }

        public void UpdateWeapon()
        {
            if (this.CurrentWeapon != null)
            {
                Destroy(this.CurrentWeapon.gameObject);
            }

            this.CurrentWeapon = EquipmentManager.Instance.SpawnProjectileWeapon(this.WeaponType, this.WeaponLevel);
            this.CurrentWeapon.transform.SetParent(this.transform, false);
            this.CurrentWeapon.Init(this.BarrelFirePoints);
        }

        private void FixedUpdate()
        {
            if (this.Target == null || !this.Target.IsAlive)
            {
                return;
            }

            if (this.CurrentWeapon != null)
            {
                if (this.AutoFireAtInput)
                {
                    this.CurrentWeapon.InputFire = this.HasFireAtPoint;
                }
                else
                {
                    this.CurrentWeapon.InputFire = this.InputFire;
                }

                if (this.HasFireAtPoint)
                {
                    foreach (var barrel in this.BarrelFirePoints)
                    {
                        var angle = Utils.CalculateBarrelAngleTo(this.transform, this.FireAt, barrel.position);
                        var degrees = Mathf.Rad2Deg * angle;
                        barrel.localRotation = Quaternion.Euler(0, degrees, 0);
                    }
                }
                this.CurrentWeapon.UpdateWeapon(this.Target, isPlayer: false, fireSplit: false, Time.fixedDeltaTime);
            }
        }
        #endregion
    }
}