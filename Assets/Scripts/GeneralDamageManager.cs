using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

#nullable enable

namespace Orbits
{
    public class GeneralDamageManager : IDisposable
    {
        #region Fields
        private NativeList<DamageResult> currentFrameDamage;
        private NativeList<DamagePoint> nextFrameDamagePoints;

        private readonly List<DamagePoint> spawnLater = new();
        private JobHandle fixedUpdateHandle;
        #endregion

        #region Constructor
        public GeneralDamageManager()
        {
            this.currentFrameDamage = new(16, Allocator.Persistent);
            this.nextFrameDamagePoints = new(128, Allocator.Persistent);
        }
        #endregion

        #region Methods
        public void Dispose()
        {
            this.fixedUpdateHandle.Complete();
            this.currentFrameDamage.Dispose();
            this.nextFrameDamagePoints.Dispose();
        }

        public void FixedUpdate(ProjectileManager manager)
        {
            this.fixedUpdateHandle.Complete();

            this.nextFrameDamagePoints.Clear();

            this.DealDamage(manager);

            if (this.spawnLater.Any())
            {
                this.spawnLater.ForEach(s => this.nextFrameDamagePoints.Add(s));
                this.spawnLater.Clear();
            }

            if (this.nextFrameDamagePoints.Length == 0)
            {
                return;
            }

            var jobData = new GeneralDamageJob
            {
                DamageResults = this.currentFrameDamage,
                DamagePoints = this.nextFrameDamagePoints,
                Targets = manager.TargetStructs,
                TargetKnn = manager.TargetKdTree
            };

            this.fixedUpdateHandle = jobData.Schedule(manager.TargetTreeRebuildJob);
        }

        public void Complete()
        {
            this.fixedUpdateHandle.Complete();
        }

        private void DealDamage(ProjectileManager manager)
        {
            for (var i = 0; i < this.currentFrameDamage.Length; i++)
            {
                var damage = this.currentFrameDamage[i];
                if (manager.TargetMap.TryGetValue(damage.TargetId, out var target))
                {
                    var amount = damage.Damage;
                    target.DealDamage(amount, null);

                    if (target.Flags.HasFlag(TargetFlags.ShowDamageIndicator))
                    {
                        var directionLength = math.length(damage.Direction);
                        var direction = damage.Direction;
                        if (directionLength > 0.001f)
                        {
                            direction = damage.Direction / directionLength;
                            direction *= math.min(6.0f, directionLength);
                        }
                        GameManager.Instance.ShowDamage(Utils.FromXZ(damage.Position), Utils.FromXZ(direction), amount);
                    }
                }
            }

            this.currentFrameDamage.Clear();
        }

        public void AddDamagePoint(float2 position, float2 damageDirection, float damage, float radius, int byTarget, int byTeam)
        {
            var damagePoint = new DamagePoint
            {
                FiredByTarget = byTarget,
                FiredByTeam = byTeam,
                Position = position,
                Damage = damage,
                Radius = radius,
                DamageDirection = damageDirection
            };

            this.spawnLater.Add(damagePoint);
        }
        #endregion
    }
}