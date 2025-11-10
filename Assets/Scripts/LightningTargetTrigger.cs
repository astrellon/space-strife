using System;
using UnityEngine;

#nullable enable

namespace Orbits
{
    public class LightningTargetTrigger : MonoBehaviour
    {
        #region Fields
        public Target Target;
        public LightningSpawner Spawner;
        public float DamageNeededToTrigger = 10.0f;
        public ParticleSystem? DamageParticlesIndicator;
        public float currentDamageTaken = 0.0f;
        public Vector2 LightningDamage = new(7, 15);

        private float maxEmissionsRate;
        #endregion

        #region Unity Methods
        private void Start()
        {
            GameManager.Instance.OnLevelLoad += this.OnLevelLoad;
            GameManager.Instance.OnLevelStopped += this.OnLevelStopped;
            GameManager.Instance.OnAbilityExecuted += this.OnAbilityExecuted;
            if (this.DamageParticlesIndicator != null)
            {
                var emissions = this.DamageParticlesIndicator.emission;
                this.maxEmissionsRate = emissions.rateOverTime.constant;
                emissions.rateOverTime = new ParticleSystem.MinMaxCurve(0.0f);
            }
        }

        private void OnDestroy()
        {
            GameManager.Instance.OnLevelLoad -= this.OnLevelLoad;
        }
        #endregion

        #region Methods
        private void OnAbilityExecuted(Ability ability, Level level)
        {
            if (ability is TimeControlAbility)
            {
                this.currentDamageTaken = 0.0f;
                this.UpdateParticles();
            }
        }

        private void OnLevelLoad(Level level)
        {
            ProjectileManager.Instance.RegisterTargetDamagedHandler(this.Target, this.OnTargetDamageTaken);
        }

        private void OnLevelStopped()
        {
            this.currentDamageTaken = 0.0f;
            this.UpdateParticles();
        }

        private void OnTargetDamageTaken(Target target, float damageAmount, ProjectileWeapon? source)
        {
            if (source == null ||
                (source.WeaponType != WeaponType.Laser && source.WeaponType != WeaponType.RapidLaser))
            {
                return;
            }

            this.currentDamageTaken += damageAmount;
            if (this.currentDamageTaken >= this.DamageNeededToTrigger)
            {
                this.currentDamageTaken %= this.DamageNeededToTrigger;
                var spawned = this.Spawner.SpawnLightning();
                if (spawned != null)
                {
                    spawned.DamageRange = this.LightningDamage;
                }
            }

            this.UpdateParticles();
        }

        private void UpdateParticles()
        {
            if (this.DamageParticlesIndicator != null)
            {
                var emissions = this.DamageParticlesIndicator.emission;
                var emissionRate = Mathf.Clamp(this.currentDamageTaken / this.DamageNeededToTrigger, 0.0f, float.MaxValue);
                emissions.rateOverTime = new ParticleSystem.MinMaxCurve(emissionRate * this.maxEmissionsRate);
            }
        }
        #endregion
    }
}