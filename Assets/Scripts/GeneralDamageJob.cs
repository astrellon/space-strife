using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using KNN;

#nullable enable

namespace Orbits
{
    [BurstCompile(CompileSynchronously = true)]
    public struct DamagePoint
    {
        public int FiredByTarget;
        public int FiredByTeam;
        public float Damage;
        public float Radius;
        public float2 Position;
        public float2 DamageDirection;
    }

    [BurstCompile(CompileSynchronously = true, FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Low)]
    public struct GeneralDamageJob : IJob
    {
        public NativeList<DamageResult> DamageResults;

        [ReadOnly]
        public NativeList<DamagePoint> DamagePoints;

        [ReadOnly]
        public NativeArray<TargetStruct> Targets;

        [ReadOnly]
        public KnnContainer TargetKnn;

        public void Execute()
        {
            var queryResult = new NativeList<int>(10, Allocator.Temp);
            for (var i = 0; i < this.DamagePoints.Length; i++)
            {
                var damagePoint = this.DamagePoints[i];

                queryResult.Clear();
                var xyzPos = new float3(damagePoint.Position.x, 0.0f, damagePoint.Position.y);
                this.TargetKnn.QueryRange(xyzPos, damagePoint.Radius, queryResult);

                for (var s = 0; s < queryResult.Length; s++)
                {
                    var target = this.Targets[queryResult[s]];
                    if (target.Team != Target.EnemyTeam)
                    {
                        continue;
                    }

                    // var fromDamagePoint = target.Position - damagePoint.Position;
                    // var distance = math.length(fromDamagePoint);
                    // var speed = damagePoint.Radius - distance + 1.0f;
                    // fromDamagePoint *= speed / distance;

                    this.DamageResults.Add(new DamageResult{ TargetId = target.Id, Damage = damagePoint.Damage, Direction = damagePoint.DamageDirection, Position = damagePoint.Position});
                }
            }
        }
    }
}