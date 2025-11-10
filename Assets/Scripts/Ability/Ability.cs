using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using LysitheaVM;
using UnityEngine;

#nullable enable

namespace Orbits
{
    public abstract class Ability : IObjectValue
    {
        #region Fields
        public static readonly IReadOnlyList<string> Keys = new [] { "id", "name", "cooldownPercent", "running", "timesTriggered" };

        public readonly float Cooldown;
        public readonly float Duration;

        public float CurrentCooldown = 0.0f;
        public bool Running = false;
        public float RunningTimer = 0.0f;
        public int TimesTriggered = 0;

        public float CooldownPercent => Mathf.Clamp01(this.CurrentCooldown / this.Cooldown);

        public abstract string Name { get; }
        public abstract string Id { get; }

        public IReadOnlyList<string> ObjectKeys => Keys;
        public string TypeName => "ability";
        #endregion

        #region Constructor
        public Ability(float cooldown, float duration)
        {
            this.Cooldown = cooldown;
            this.Duration = duration;
        }
        #endregion

        #region Methods
        public virtual void Update(float dt, Level currentLevel)
        {
            this.CurrentCooldown = Mathf.Max(0.0f, this.CurrentCooldown - dt);

            this.RunningTimer -= dt;
            if (this.Running && (this.RunningTimer <= 0.0f || currentLevel.State != LevelState.Running))
            {
                this.DoFinish(currentLevel);
                this.Running = false;
            }
        }
        public abstract Ability Clone();

        public bool Execute(Level currentLevel)
        {
            if (this.CurrentCooldown <= 0.0f)
            {
                if (this.DoExecute(currentLevel))
                {
                    this.TimesTriggered++;
                    this.RunningTimer = this.Duration;
                    this.Running = true;
                    this.CurrentCooldown = this.Cooldown;
                    return true;
                }
            }

            return false;
        }

        protected abstract bool DoExecute(Level currentLevel);
        protected abstract void DoFinish(Level currentLevel);

        public bool TryGetKey(string key, [NotNullWhen(true)] out IValue? value)
        {
            value = key switch
            {
                "id" => new StringValue(this.Id),
                "name" => new StringValue(this.Name),
                "cooldownPercent" => new NumberValue(this.CooldownPercent),
                "running" => new BoolValue(this.Running),
                "timesTriggered" => new NumberValue(this.TimesTriggered),
                _ => NullValue.Value
            };

            return value.CompareTo(NullValue.Value) != 0;
        }

        public override string ToString()
        {
            return StandardObjectLibrary.GeneralToString(this, false);
        }
        public string ToStringSerialise()
        {
            return StandardObjectLibrary.GeneralToString(this, true);
        }

        public int CompareTo(IValue? other)
        {
            if (other == null || other is not Ability otherAbility)
            {
                return 1;
            }

            return this.Name.CompareTo(otherAbility.Name);
        }
        #endregion
    }
}