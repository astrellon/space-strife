using System;
using System.Linq;
using UnityEngine;

#nullable enable

namespace Orbits
{
    public class TimeControlAbility : Ability
    {
        #region Fields
        public override string Name => "Time Slow";
        public override string Id => "time";
        #endregion

        #region Constructor
        public TimeControlAbility() : base(cooldown: 10.0f, duration: 5.0f)
        {

        }
        #endregion

        #region Methods
        public override Ability Clone()
        {
            return new TimeControlAbility();
        }

        protected override bool DoExecute(Level currentLevel)
        {
            SaturationEffect.Instance.SetShow(true, currentLevel.Player.Planet.transform, currentLevel.TimeControlVisualDistance);
            GameManager.Instance.TimeManipulatorActive = true;
            return true;
        }

        protected override void DoFinish(Level currentLevel)
        {
            GameManager.Instance.TimeManipulatorActive = false;
            SaturationEffect.Instance.SetShow(false);
        }
        #endregion
    }
}