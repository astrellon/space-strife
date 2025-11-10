using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#nullable enable

namespace Orbits
{
    public class ProjectileWeapon : MonoBehaviour
    {
        #region Fields
        public Material Material;
        public Mesh Mesh;
        public FollowingProjectileEffect? FollowingObject;
        public int MaxNumOfProjectiles = 1000;
        public float MaxAge;
        public float MaxAgeVariance = 0.0f;
        public float Damage;
        public float Speed;
        public float SpeedVariance = 0.0f;
        public float Cooldown;
        public float MaxCooldown;
        public float QueryRange = 1.0f;
        public float HomingRange = 0.0f;
        public float HomingAngle = 5.0f;
        public float RandomFireRange = 2.0f;
        public float SplitRandomRange = 4.0f;
        public float GravityScale = 1.0f;
        public int ProjectilePerFire = 1;
        public int ProjectilePerFireSplit = 0;
        public int ContinuousFire = 0;
        public bool DidJustFire = false;
        public GameObject HitPrefab;
        public GameObject MissPrefab;

        public GameObject MuzzleFlashPrefab;
        public float MuzzleCooldownTime = 0.25f;
        public WeaponType WeaponType;
        public List<Transform> FireAtPoints = new();

        public bool InputFire;

        private int currentContinuousFire = 0;
        private float muzzleCooldown = 0.0f;
        private int fireAtPointIndex = 0;

        public bool CurrentlyFiring => this.currentContinuousFire > 0;
        #endregion

        #region Methods
        public void Init(IEnumerable<Transform> fireAtPoints)
        {
            this.FireAtPoints.AddRange(fireAtPoints);
        }

        public float GetRandomisedSpeed()
        {
            if (this.SpeedVariance == 0.0f)
            {
                return this.Speed;
            }

            return (1.0f - Random.value * this.SpeedVariance) * this.Speed;
        }

        public float GetRandomisedMaxAge()
        {
            if (this.MaxAgeVariance == 0.0f)
            {
                return this.MaxAge;
            }

            return (1.0f - Random.value * this.MaxAgeVariance) * this.MaxAge;
        }

        public void UpdateWeapon(Target firedBy, bool isPlayer, bool fireSplit, float dt)
        {
            this.DidJustFire = false;
            this.muzzleCooldown = Mathf.Max(0.0f, this.muzzleCooldown - dt);
            this.Cooldown = Mathf.Max(0.0f, this.Cooldown - dt);

            if (this.CurrentlyFiring)
            {
                this.TriggerNextMuzzleFlash();
                this.DoFire(firedBy, isPlayer, fireSplit);
                this.currentContinuousFire--;
                this.DidJustFire = true;
            }
            else if (this.InputFire && this.Cooldown <= 0.0f)
            {
                this.Cooldown = this.MaxCooldown;
                if (this.muzzleCooldown <= 0.0f)
                {
                    this.TriggerNextMuzzleFlash();
                    this.muzzleCooldown = this.MuzzleCooldownTime;
                }

                this.currentContinuousFire = this.ContinuousFire;

                this.DoFire(firedBy, isPlayer, fireSplit);
                this.DidJustFire = true;
            }
        }

        private void DoFire(Target firedBy, bool isPlayer, bool fireSplit)
        {
            var numProjectiles = this.ProjectilePerFire;
            var randomRange = this.RandomFireRange;
            if (fireSplit)
            {
                numProjectiles = this.ProjectilePerFireSplit > 0 ? this.ProjectilePerFireSplit : this.ProjectilePerFire * 2;
                randomRange = this.SplitRandomRange > 0 ? this.SplitRandomRange : 4.0f;
            }

            for (var i = 0; i < numProjectiles; i++)
            {
                var firePoint = this.FireAtPoints[this.fireAtPointIndex];
                var pos = Utils.ToXZ(firePoint.position);

                var forward = firePoint.forward;
                if (!Mathf.Approximately(randomRange, 0.0f))
                {
                    forward = Quaternion.Euler(0, (Random.value - 0.5f) * randomRange, 0) * forward;
                }

                ProjectileManager.Instance.SpawnBullet(firedBy, pos, Utils.ToXZ(forward), this);
            }

            this.fireAtPointIndex++;
            if (this.fireAtPointIndex >= this.FireAtPoints.Count)
            {
                this.fireAtPointIndex = 0;
            }

            if (isPlayer)
            {
                PlayerState.Instance.CurrentLevelStats.PlayerFired(this.WeaponType, this.ProjectilePerFire);
            }
        }

        private void TriggerNextMuzzleFlash()
        {
            if (this.MuzzleFlashPrefab == null)
            {
                return;
            }

            var muzzle = GameObjectPools.Instance.Spawn(this.MuzzleFlashPrefab);
            muzzle.transform.SetParent(this.FireAtPoints[this.fireAtPointIndex], false);
            muzzle.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            if (muzzle.TryGetComponent<ParticleSystem>(out var ps))
            {
                ps.Play(true);
            }
        }
        #endregion
    }
}