using System;
using KNN;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

#nullable enable

namespace Orbits
{
    [BurstCompile(CompileSynchronously = true)]
    public struct DamageResult
    {
        public int TargetId;
        public float Damage;
        public float2 Position;
        public float2 Direction;
    }

    [BurstCompile(CompileSynchronously = true)]
    public struct MoveProjectileThroughPortal
    {
        public int ProjectileId;
        public float2 MoveDiff;
    }

    [BurstCompile(CompileSynchronously = true, FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Low)]
    public struct FixedUpdateProjectileJob : IJob
    {
        public float CurrentDelta;
        public int NumAlive;
        public int NumTargets;
        public int NumGravitySources;
        public int NumPortals;
        public float HomingRange;
        public float QueryRange;
        public float HomingAngle;
        public float GravityScale;
        public NativeArray<Projectile> Result;
        public NativeList<DamageResult> DamageResults;
        public NativeList<MoveProjectileThroughPortal> MoveThroughPortals;

        [ReadOnly]
        public NativeArray<GravitySourceStruct> GravitySources;

        [ReadOnly]
        public NativeArray<PortalStruct> Portals;

        [ReadOnly]
        public NativeArray<TargetStruct> Targets;

        [ReadOnly]
        public KnnContainer TargetKnn;

        public void Execute()
        {
            var rotateHoming1 = quaternion.Euler(0, this.HomingAngle * this.CurrentDelta, 0);
            var rotateHoming2 = quaternion.Euler(0, -this.HomingAngle * this.CurrentDelta, 0);

            var queryResult = new NativeList<int>(10, Allocator.Temp);
            for (var i = 0; i < this.NumAlive; i++)
            {
                var data = this.Result[i];
                data.Age += this.CurrentDelta;

                var acceleration = float2.zero;
                var hitGravitySource = false;
                for (var g = 0; g < this.NumGravitySources; g++)
                {
                    var source = this.GravitySources[g];
                    acceleration += source.CalculateGravityLinear(data.NextPosition) * this.GravityScale;
                    if (ClosestIntersection(source.Position, source.InnerRange, data.PrevPosition, data.NextPosition, out var hitPoint))
                    {
                        hitGravitySource = true;
                        data.NextPosition = hitPoint;

                        if (source.TargetId > 0)
                        {
                            this.DamageResults.Add(new DamageResult{ TargetId = source.TargetId, Damage = 1.0f });
                        }
                    }
                }

                if (hitGravitySource)
                {
                    data.Age = data.MaxAge;
                    this.Result[i] = data;
                    continue;
                }

                data.Velocity += acceleration * this.CurrentDelta;
                data.PrevPosition = data.NextPosition;
                data.NextPosition += data.Velocity * this.CurrentDelta;

                if (this.NumPortals > 0 && data.WentThroughPortal.x < Projectile.InvalidPortalPosCheck)
                {
                    for (var p = 0; p < this.NumPortals; p++)
                    {
                        var portal = this.Portals[p];
                        var dist = math.distance(portal.Position, data.NextPosition);
                        if (dist < portal.OpenSize)
                        {
                            this.MoveThroughPortals.Add(new MoveProjectileThroughPortal
                            {
                                ProjectileId = data.ProjectileId,
                                MoveDiff = portal.PortalsTo - portal.Position
                            });
                            data.WentThroughPortal = portal.PortalsTo;
                        }
                    }
                }
                else if (data.WentThroughPortal.x >= Projectile.InvalidPortalPosCheck)
                {
                    var dist = math.distance(data.WentThroughPortal, data.NextPosition);
                    if (dist > 5.1f)
                    {
                        data.WentThroughPortal = Projectile.InvalidPortalPos;
                    }
                }

                if (!hitGravitySource)
                {
                    queryResult.Clear();
                    var xyzPos = new float3(data.NextPosition.x, 0.0f, data.NextPosition.y);
                    this.TargetKnn.QueryRange(xyzPos, this.QueryRange, queryResult);

                    var closestId = -1;
                    var closestDistance = float.MaxValue;

                    for (var s = 0; s < queryResult.Length; s++)
                    {
                        var target = this.Targets[queryResult[s]];
                        if (target.Id == data.FiredByTarget || target.Team == data.FiredByTeam)
                        {
                            continue;
                        }

                        if (ClosestIntersection(target.Position, target.Size, data.NextPosition, data.PrevPosition, out var hitPosition))
                        {
                            data.NextPosition = hitPosition;
                            closestId = -1;
                            data.Age = data.MaxAge;
                            this.DamageResults.Add(new DamageResult{ TargetId = target.Id, Damage = 1.0f, Direction = data.Velocity, Position = data.NextPosition});
                            break;
                        }
                        else
                        {
                            var distance = math.distance(data.NextPosition, target.Position);
                            if (this.HomingRange > 0.001f && distance < this.HomingRange)
                            {
                                if (distance < closestDistance)
                                {
                                    closestDistance = distance;
                                    closestId = queryResult[s];
                                }
                            }
                        }
                    }

                    if (closestId >= 0)
                    {
                        var target = this.Targets[closestId];
                        var toTarget = ToXYZ(math.normalize(target.Position - data.NextPosition));
                        var angle = math.cross(toTarget, ToXYZ(math.normalize(data.Velocity)));

                        // Hopefully the fact that we're just rotating the velocity won't be an issue
                        // whilst going through gravity sources.
                        if (angle.y > 0)
                        {
                            var newRotate = math.rotate(rotateHoming2, new float3(data.Velocity.x, 0.0f, data.Velocity.y));
                            data.Velocity = new float2(newRotate.x, newRotate.z);
                        }
                        else if (angle.y < 0)
                        {
                            var newRotate = math.rotate(rotateHoming1, new float3(data.Velocity.x, 0.0f, data.Velocity.y));
                            data.Velocity = new float2(newRotate.x, newRotate.z);
                        }
                    }
                }

                this.Result[i] = data;
            }
        }

        private static float3 ToXYZ(float2 input)
        {
            return new float3(input.x, 0, input.y);
        }

        //cx,cy is center point of the circle
        public static bool ClosestIntersection(float2 circle, float radius, float2 lineStart, float2 lineEnd, out float2 result)
        {
            var intersections = FindLineCircleIntersections(circle, radius, lineStart, lineEnd, out var intersection1, out var intersection2);
            // In this case we only care about an intersection through a circle, if it's an intersection of 1 (a glancing blow) we're not going to count it.
            if (intersections != 2)
            {
                result = float2.zero;
                return false;
            }

            var ray = lineEnd - lineStart;
            var rayDistance = math.length(ray);
            var rayDirection = ray / rayDistance;

            var dist1 = math.distance(intersection1, lineStart);
            var dist2 = math.distance(intersection2, lineStart);

            if (dist1 > rayDistance && dist2 > rayDistance)
            {
                result = float2.zero;
                return false;
            }

            result = dist1 < dist2 ? intersection1 : intersection2;

            var toResult = math.normalize(result - lineStart);
            if (math.dot(toResult, rayDirection) < 0)
            {
                result = float2.zero;
                return false;
            }

            return true;
        }

        // Find the points of intersection.
        private static int FindLineCircleIntersections(float2 circle, float radius,
            float2 point1, float2 point2, out float2 intersection1, out float2 intersection2)
        {
            float dx, dy, A, B, C, det, t;

            dx = point2.x - point1.x;
            dy = point2.y - point1.y;

            A = dx * dx + dy * dy;
            B = 2 * (dx * (point1.x - circle.x) + dy * (point1.y - circle.y));
            C = (point1.x - circle.x) * (point1.x - circle.x) +
                (point1.y - circle.y) * (point1.y - circle.y) -
                radius * radius;

            det = B * B - 4 * A * C;
            if ((A <= 0.0000001f) || (det < 0))
            {
                // No real solutions.
                intersection1 = float2.zero;
                intersection2 = float2.zero;
                return 0;
            }
            else if (det == 0)
            {
                // One solution.
                t = -B / (2 * A);
                intersection1 = new float2(point1.x + t * dx, point1.y + t * dy);
                intersection2 = float2.zero;
                return 1;
            }
            else
            {
                // Two solutions.
                t = (-B + math.sqrt(det)) / (2 * A);
                intersection1 = new float2(point1.x + t * dx, point1.y + t * dy);
                t = (-B - math.sqrt(det)) / (2 * A);
                intersection2 = new float2(point1.x + t * dx, point1.y + t * dy);
                return 2;
            }
        }
    }

    [BurstCompile(CompileSynchronously = true)]
    public struct UpdateProjectileJob : IJob
    {
        public float InterpolateRatio;
        public int NumAlive;

        [ReadOnly]
        public NativeArray<Projectile> Result;
        public NativeArray<Matrix4x4> Transforms;

        public void Execute()
        {
            for (var i = 0; i < this.NumAlive; i++)
            {
                var data = this.Result[i];

                var interpolated = math.lerp(data.PrevPosition, data.NextPosition, this.InterpolateRatio);
                var position = new Vector3(interpolated.x, 0, interpolated.y);
                var angle = Mathf.Atan2(data.Velocity.y, data.Velocity.x);
                var rotation = Quaternion.Euler(0, angle * -180.0f / Mathf.PI, 0);
                this.Transforms[i] = Matrix4x4.TRS(position, rotation, Vector3.one);
            }
        }
    }
}