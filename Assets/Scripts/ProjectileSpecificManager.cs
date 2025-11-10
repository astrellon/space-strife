using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

#nullable enable

namespace Orbits
{
    public class ProjectileSpecificManager : IDisposable
    {
        #region Fields
        public readonly ProjectileWeapon Weapon;

        private NativeArray<Projectile> result;
        private NativeArray<Matrix4x4> transforms;
        private NativeList<DamageResult> currentFrameDamage;
        private NativeList<MoveProjectileThroughPortal> moveThroughPortals;
        private readonly RenderParams renderParams;
        private readonly List<Projectile> spawnLater = new();

        public int NumAlive { get; private set; } = 0;

        private JobHandle updateHandle;
        private JobHandle fixedUpdateHandle;
        private int counter;

        public NativeList<DamageResult> CurrentFrameDamage => this.currentFrameDamage;
        private readonly Dictionary<int, FollowingProjectileEffect> followingObjects = new();

        private readonly Dictionary<TargetType, long> targetsPlayerDestroyed = new();
        #endregion

        #region Constructor
        public ProjectileSpecificManager(ProjectileWeapon weapon)
        {
            this.Weapon = weapon;
            this.result = new(weapon.MaxNumOfProjectiles, Allocator.Persistent);
            this.transforms = new(this.result.Length, Allocator.Persistent);
            this.currentFrameDamage = new(16, Allocator.Persistent);
            this.moveThroughPortals = new(16, Allocator.Persistent);
            this.renderParams = new(weapon.Material)
            {
                rendererPriority = 3000,
                shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off
            };
        }
        #endregion

        #region Methods
        public void Dispose()
        {
            this.fixedUpdateHandle.Complete();
            this.updateHandle.Complete();
            this.result.Dispose();
            this.transforms.Dispose();
            this.currentFrameDamage.Dispose();
            this.moveThroughPortals.Dispose();
        }

        public void DrawGizmos()
        {
            if (this.followingObjects.Any())
            {
                for (var i = 0; i < this.NumAlive; i++)
                {
                    var projectile = this.result[i];
                    if (this.followingObjects.TryGetValue(projectile.ProjectileId, out var following))
                    {
                        var pos = this.transforms[i].GetPosition();
                        Gizmos.DrawWireSphere(pos, 0.5f);
                        Gizmos.DrawLine(pos, following.transform.position);
                    }
                }
            }
        }

        public void Clear()
        {
            this.NumAlive = 0;
            foreach (var kvp in this.followingObjects)
            {
                kvp.Value.TriggerRemove();
            }
            this.followingObjects.Clear();
            this.targetsPlayerDestroyed.Clear();
        }

        public bool UpdateLocation(ProjectileManager manager)
        {
            if (this.NumAlive == 0)
            {
                return false;
            }

            this.fixedUpdateHandle.Complete();

            var now = Time.timeAsDouble;
            var fixedNow = Time.fixedTimeAsDouble;
            var ratio = (now - fixedNow) / Time.fixedDeltaTime;

            var jobData = new UpdateProjectileJob
            {
                NumAlive = this.NumAlive,
                InterpolateRatio = (float)ratio,
                Result = this.result,
                Transforms = this.transforms
            };

            this.updateHandle = jobData.Schedule();
            return true;
        }

        public void Render()
        {
            if (this.NumAlive > 0)
            {
                this.updateHandle.Complete();

                if (this.Weapon.FollowingObject != null)
                {
                    for (var i = 0; i < this.NumAlive; i++)
                    {
                        var projectile = this.result[i];
                        if (!this.followingObjects.TryGetValue(projectile.ProjectileId, out var following))
                        {
                            following = GameObjectPools.Instance.Spawn(this.Weapon.FollowingObject);
                            following.Prefab = this.Weapon.FollowingObject.gameObject;
                            following.Following = projectile.ProjectileId;
                            following.TriggerSpawned();

                            this.followingObjects[projectile.ProjectileId] = following;
                        }

                        following.DoRender();
                        if (following.FollowRotation)
                        {
                            following.transform.SetPositionAndRotation(this.transforms[i].GetPosition(), this.transforms[i].rotation);
                        }
                        else
                        {
                            following.transform.position = this.transforms[i].GetPosition();
                        }
                    }
                }

                if (this.Weapon.Mesh != null)
                {
                    Graphics.RenderMeshInstanced(this.renderParams, this.Weapon.Mesh, 0, this.transforms, this.NumAlive);
                }

                this.CleanupArrays();
            }
        }

        public void CompleteMovement()
        {
            if (this.NumAlive > 0)
            {
                this.fixedUpdateHandle.Complete();
            }
        }

        public bool FixedUpdate(ProjectileManager manager)
        {
            this.fixedUpdateHandle.Complete();

            this.HandleSpawnLater();
            this.DealDamage(manager);
            this.MoveThroughPortals();

            if (this.NumAlive == 0)
            {
                return false;
            }

            var jobData = new FixedUpdateProjectileJob
            {
                NumAlive = this.NumAlive,
                CurrentDelta = GameManager.Instance.PlayerDeltaTimeScale * Time.deltaTime,
                Result = this.result,
                GravityScale = this.Weapon.GravityScale,
                GravitySources = manager.GravitySourceStructs,
                NumGravitySources = manager.NumGravitySources,
                Portals = manager.PortalStructs,
                NumPortals = PortalManager.Instance.NumPortals,
                Targets = manager.TargetStructs,
                TargetKnn = manager.TargetKdTree,
                NumTargets = manager.Targets.Count,
                QueryRange = Mathf.Max(this.Weapon.QueryRange, 4.5f),
                HomingRange = this.Weapon.HomingRange,
                HomingAngle = this.Weapon.HomingAngle,
                DamageResults = this.currentFrameDamage,
                MoveThroughPortals = this.moveThroughPortals
            };

            this.fixedUpdateHandle = jobData.Schedule(manager.TargetTreeRebuildJob);
            return true;
        }

        public void SpawnBullet(Target firedBy, float2 position, float2 forward)
        {
            var speed = this.Weapon.GetRandomisedSpeed();
            var maxAge = this.Weapon.GetRandomisedMaxAge();
            var projectile = this.CreateProjectile(firedBy, position, forward * speed, maxAge);
            if (!this.fixedUpdateHandle.IsCompleted)
            {
                this.spawnLater.Add(projectile);
                return;
            }

            this.fixedUpdateHandle.Complete();
            this.result[this.NumAlive] = projectile;
            this.NumAlive++;
        }

        private void DealDamage(ProjectileManager manager)
        {
            var playerHits = 0;
            this.targetsPlayerDestroyed.Clear();

            for (var i = 0; i < this.currentFrameDamage.Length; i++)
            {
                var damage = this.currentFrameDamage[i];
                if (manager.TargetMap.TryGetValue(damage.TargetId, out var target))
                {
                    var amount = damage.Damage * this.Weapon.Damage;
                    var destroyed = target.DealDamage(amount, this.Weapon);

                    if (!manager.PlayerTanks.Contains(target))
                    {
                        playerHits++;

                        if (destroyed)
                        {
                            if (!this.targetsPlayerDestroyed.TryGetValue(target.TargetType, out var current))
                            {
                                current = 0L;
                            }

                            this.targetsPlayerDestroyed[target.TargetType] = current + 1L;
                        }
                    }

                    if (target.Flags.HasFlag(TargetFlags.ShowDamageIndicator))
                    {
                        var directionLength = math.length(damage.Direction);
                        var direction = damage.Direction;
                        if (directionLength > 0.001f)
                        {
                            direction = damage.Direction / directionLength;
                            direction *= math.min(6.0f, directionLength * 0.1f);
                        }
                        GameManager.Instance.ShowDamage(Utils.FromXZ(damage.Position), Utils.FromXZ(direction), amount);
                    }
                }
            }

            if (playerHits > 0)
            {
                PlayerState.Instance.CurrentLevelStats.PlayerHit(this.Weapon.WeaponType, playerHits);
            }
            if (this.targetsPlayerDestroyed.Any())
            {
                PlayerState.Instance.CurrentLevelStats.AddTargetsDestroyed(this.targetsPlayerDestroyed);
            }

            this.currentFrameDamage.Clear();
        }

        private void MoveThroughPortals()
        {
            if (this.moveThroughPortals.Length == 0)
            {
                return;
            }

            for (var i = 0; i < this.NumAlive; i++)
            {
                var obj = this.result[i];
                for (var p = 0; p < this.moveThroughPortals.Length; p++)
                {
                    var move = this.moveThroughPortals[p];
                    if (move.ProjectileId == obj.ProjectileId)
                    {
                        obj.PrevPosition += move.MoveDiff;
                        obj.NextPosition += move.MoveDiff;

                        this.result[i] = obj;
                    }
                }
            }

            this.moveThroughPortals.Clear();
        }

        private void CleanupArrays()
        {
            for (var i = 0; i < this.NumAlive; i++)
            {
                var obj = this.result[i];
                if (obj.Age < obj.MaxAge)
                {
                    continue;
                }

                var xyzPos = new Vector3(obj.NextPosition.x, 0, obj.NextPosition.y);
                if (PortalManager.Instance.IsVisible(xyzPos))
                {
                    var bulletPrefab = GameObjectPools.Instance.Spawn(this.Weapon.HitPrefab);
                    var pos = Utils.FromXZ(obj.NextPosition);
                    bulletPrefab.transform.position = pos;
                }

                if (this.followingObjects.Count > 0)
                {
                    var id = this.result[i].ProjectileId;
                    if (this.followingObjects.TryGetValue(id, out var following))
                    {
                        following.TriggerRemove();
                        this.followingObjects.Remove(id);
                    }
                }

                this.result[i] = this.result[this.NumAlive - 1];
                this.NumAlive--;
                i--;
            }
        }

        private void HandleSpawnLater()
        {
            if (this.spawnLater.Count == 0)
            {
                return;
            }

            foreach (var toSpawn in this.spawnLater)
            {
                this.result[this.NumAlive] = toSpawn;
                this.NumAlive++;
            }

            this.spawnLater.Clear();
        }

        private Projectile CreateProjectile(Target firedBy, float2 position, float2 velocity, float maxAge)
        {
            int id;
            unchecked
            {
                id = this.counter++;
            }

            return new Projectile
            {
                FiredByTarget = firedBy.Id,
                FiredByTeam = firedBy.Team,
                ProjectileId = id,
                MaxAge = maxAge,
                Age = 0.0f,
                PrevPosition = position,
                NextPosition = position,
                Velocity = velocity,
                WentThroughPortal = Projectile.InvalidPortalPos
            };
        }
        #endregion
    }
}