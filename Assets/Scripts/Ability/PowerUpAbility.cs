using System.Linq;
using UnityEngine;

#nullable enable

namespace Orbits
{
    public class PowerUpAbility : Ability
    {
        #region Fields
        public override string Name => "Power Up";
        public override string Id => "power";
        #endregion

        #region Constructor
        public PowerUpAbility() : base(cooldown: 10.0f, duration: 5.0f)
        {

        }
        #endregion

        #region Methods
        public override Ability Clone()
        {
            return new PowerUpAbility();
        }

        protected override bool DoExecute(Level currentLevel)
        {
            this.RunningTimer = this.Duration;
            this.Running = true;

            var scale = currentLevel.Player.PlanetRadius * 1.2f;
            PowerUpEffect.Instance.SetShow(true, currentLevel.Player.Planet.transform, scale);

            currentLevel.SetWeaponLevelBoosted(1);
            return true;
        }

        protected override void DoFinish(Level currentLevel)
        {
            currentLevel.SetWeaponLevelBoosted(0);
            PowerUpEffect.Instance.SetShow(false);
        }
        #endregion
    }
}