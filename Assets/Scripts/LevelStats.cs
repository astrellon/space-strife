using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using NUnit.Framework.Interfaces;

#nullable enable

namespace Orbits
{
    public interface IReadOnlyLevelStats
    {
        IReadOnlyDictionary<WeaponType, long> ShotsFired { get; }
        IReadOnlyDictionary<WeaponType, long> ShotsHit { get; }
        IReadOnlyDictionary<TargetType, long> TargetsDestroyed { get; }
        int PlanetHealthRemaining { get; }

        bool IsEmpty { get; }
    }

    public class LevelStats : IReadOnlyLevelStats
    {
        #region Fields
        public static readonly IReadOnlyLevelStats Empty = new LevelStats();

        public Dictionary<WeaponType, long> ShotsFired;
        public Dictionary<WeaponType, long> ShotsHit;
        public Dictionary<TargetType, long> TargetsDestroyed;
        public int PlanetHealthRemaining { get; set; } = -1;

        IReadOnlyDictionary<WeaponType, long> IReadOnlyLevelStats.ShotsFired => this.ShotsFired;
        IReadOnlyDictionary<WeaponType, long> IReadOnlyLevelStats.ShotsHit => this.ShotsHit;
        IReadOnlyDictionary<TargetType, long> IReadOnlyLevelStats.TargetsDestroyed => this.TargetsDestroyed;

        public bool IsEmpty => !this.ShotsFired.Any(kvp => kvp.Value > 0u);
        #endregion

        #region Constructor
        public LevelStats()
        {
            this.ShotsFired = new();
            this.ShotsHit = new();
            this.TargetsDestroyed = new();
        }

        public LevelStats(Dictionary<WeaponType, long> shotsFired, Dictionary<WeaponType, long> shotsHit, Dictionary<TargetType, long> targetsDestroyed)
        {
            this.ShotsFired = shotsFired;
            this.ShotsHit= shotsHit;
            this.TargetsDestroyed = targetsDestroyed;
        }
        #endregion

        #region Methods
        public void PlayerFired(WeaponType weaponType, long numFired)
        {
            if (!this.ShotsFired.TryGetValue(weaponType, out var current))
            {
                current = 0L;
            }

            current += numFired;
            this.ShotsFired[weaponType] = current;
        }

        public void PlayerHit(WeaponType weaponType, long numHit)
        {
            if (!this.ShotsHit.TryGetValue(weaponType, out var current))
            {
                current = 0L;
            }

            current += numHit;
            this.ShotsHit[weaponType] = current;
        }

        public void AddTargetsDestroyed(IReadOnlyDictionary<TargetType, long> destroyed)
        {
            foreach (var kvp in destroyed)
            {
                if (!this.TargetsDestroyed.TryGetValue(kvp.Key, out var value))
                {
                    value = 0L;
                }

                value += kvp.Value;
                this.TargetsDestroyed[kvp.Key] = value;
            }
        }

        public static LevelStats Combine(IReadOnlyLevelStats stats1, IReadOnlyLevelStats stats2)
        {
            var shotsFired = new Dictionary<WeaponType, long>(stats1.ShotsFired);
            var shotsHit = new Dictionary<WeaponType, long>(stats1.ShotsHit);
            var targetsDestroyed = new Dictionary<TargetType, long>(stats1.TargetsDestroyed);

            Combine(shotsFired, stats2.ShotsFired);
            Combine(shotsHit, stats2.ShotsHit);
            Combine(targetsDestroyed, stats2.TargetsDestroyed);

            var result = new LevelStats(shotsFired, shotsHit, targetsDestroyed)
            {
                PlanetHealthRemaining = Mathf.Max(stats1.PlanetHealthRemaining, stats2.PlanetHealthRemaining)
            };
            return result;
        }

        private static void Combine<T>(Dictionary<T, long> target, IReadOnlyDictionary<T, long> source)
        {
            foreach (var kvp in source)
            {
                if (!target.TryGetValue(kvp.Key, out var current))
                {
                    current = 0L;
                }
                target[kvp.Key] = current + kvp.Value;
            }
        }
        #endregion
    }
}