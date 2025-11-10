using System.Linq;
using UnityEngine;

#nullable enable

namespace Orbits
{
    public class SplitAbility : Ability
    {
        #region Fields
        public override string Name => "Split";
        public override string Id => "split";
        #endregion

        #region Constructor
        public SplitAbility() : base(cooldown: 10.0f, duration: 5.0f)
        {
        }
        #endregion

        #region Methods
        public override Ability Clone()
        {
            return new SplitAbility();
        }

        protected override bool DoExecute(Level currentLevel)
        {
            this.RunningTimer = this.Duration;
            this.Running = true;
            currentLevel.Player.Tanks.ForEach(t =>
            {
                if (t != null) { t.FireSplit = true; }
            });

            var scale = currentLevel.Player.PlanetRadius * 1.2f;
            SplitEffect.Instance.SetShow(true, currentLevel.Player.Planet.transform, scale);

            return true;
        }

        protected override void DoFinish(Level currentLevel)
        {
            currentLevel.Player.Tanks.ForEach(t =>
            {
                if (t != null) { t.FireSplit = false; }
            });
            SplitEffect.Instance.SetShow(false);
        }
        #endregion
    }
}