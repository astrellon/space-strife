using System.Linq;
using System.Collections.Generic;
using Unity.Jobs;
using Unity.Collections;
using UnityEngine;
using Unity.Mathematics;
using KNN;
using KNN.Jobs;
using Orbits.Extensions;

#nullable enable

namespace Orbits
{
    public delegate void TargetDestroyedHandler(Target target, WaveShipController? shipController);
    public delegate void TargetDamagedHandler(Target target, float damageAmount, ProjectileWeapon? source);

    [DefaultExecutionOrder(-1)]
    public class ProjectileManager : MonoBehaviour
    {
        public delegate void TargetRegisterHandle(Target target, bool removed);

        #region Fields
        public static ProjectileManager Instance { get; private set; }

        private NativeArray<GravitySourceStruct> gravitySourceStructs;
        private NativeArray<PortalStruct> portalStructs;
        private NativeArray<TargetStruct> targetStructs;
        private NativeArray<float3> targetPoints;

        public NativeArray<GravitySourceStruct> GravitySourceStructs => this.gravitySourceStructs;
        public NativeArray<PortalStruct> PortalStructs => this.portalStructs;
        public NativeArray<TargetStruct> TargetStructs => this.targetStructs;
        public int NumGravitySources => this.gravitySources.Count;

        private readonly Dictionary<int, Target> targetMap = new();
        private readonly List<Target> targets = new();
        private readonly List<Target> playerTanks = new();
        public IReadOnlyList<Target> Targets => this.targets;
        public IReadOnlyDictionary<int, Target> TargetMap => this.targetMap;
        public IReadOnlyList<Target> PlayerTanks => this.playerTanks;

        private readonly List<GravitySource> gravitySources = new();
        private readonly Dictionary<ProjectileWeapon, ProjectileSpecificManager> managers = new();
        private GeneralDamageManager? generalManager;
        private readonly List<Target> deregisterTargets = new();
        private readonly List<Target> registerTargets = new();
        private readonly UpdateSafeDictionaryList<Target, TargetDestroyedHandler> targetDestroyedHandlers = new();
        private readonly UpdateSafeDictionaryList<Target, TargetDamagedHandler> targetDamagedHandlers = new();
        private KnnContainer targetKdTree;
        public JobHandle TargetTreeRebuildJob;

        public KnnContainer TargetKdTree => this.targetKdTree;

        public event TargetDestroyedHandler? OnTargetDestroyed;
        public IReadOnlyDictionary<ProjectileWeapon, ProjectileSpecificManager> Managers => this.managers;

        public event TargetRegisterHandle? OnTargetRegistered;
        #endregion

        #region Unity Methods
        private void Awake()
        {
            Instance = this;

            this.generalManager = new();
            this.gravitySourceStructs = new NativeArray<GravitySourceStruct>(64, Allocator.Persistent);
            this.portalStructs = new NativeArray<PortalStruct>(2, Allocator.Persistent);
            this.targetStructs = new NativeArray<TargetStruct>(512, Allocator.Persistent);
            this.targetPoints = new NativeArray<float3>(512, Allocator.Persistent);

            this.targetKdTree = new KnnContainer(this.targetPoints, false, Allocator.Persistent);
        }

        // Update is called once per frame
        private void Update()
        {
            if (!GameManager.Instance.GameActive)
            {
                return;
            }

            foreach (var kvp in this.managers)
            {
                kvp.Value.UpdateLocation(this);
            }
        }

        private void LateUpdate()
        {
            foreach (var kvp in this.managers)
            {
                kvp.Value.Render();
            }
        }

        private void FixedUpdate()
        {
            if (!GameManager.Instance.GameActive)
            {
                return;
            }

            foreach (var kvp in this.managers)
            {
                kvp.Value.CompleteMovement();
            }
            this.generalManager?.Complete();

            this.HandleRegisteringTargets();
            this.UpdateTargets();

            foreach (var kvp in this.managers)
            {
                kvp.Value.FixedUpdate(this);
            }

            this.generalManager?.FixedUpdate(this);
        }

        private void OnDestroy()
        {
            foreach (var kvp in this.managers)
            {
                kvp.Value.Dispose();
            }
            this.generalManager?.Dispose();

            this.gravitySourceStructs.Dispose();
            this.portalStructs.Dispose();
            this.targetStructs.Dispose();
            this.targetPoints.Dispose();

            this.managers.Clear();

            this.TargetTreeRebuildJob.Complete();
            this.targetKdTree.Dispose();
            this.targetDamagedHandlers.Clear();
            this.targetDestroyedHandlers.Clear();
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            foreach (var target in this.Targets)
            {
                Gizmos.DrawWireCube(target.transform.position, Vector3.one * target.Size);
            }

            Gizmos.color = Color.cyan;
            foreach (var gravitySource in this.gravitySources)
            {
                Gizmos.DrawWireCube(gravitySource.transform.position, Vector3.one * gravitySource.OuterRange);
            }

            Gizmos.color = Color.magenta;
            foreach (var manager in this.managers.Values)
            {
                manager.DrawGizmos();
            }
        }
        #endregion

        #region Methods
        public void Clear()
        {
            Debug.Log("Clearing projectile manager");
            Target.ResetCounter();

            foreach (var kvp in this.managers)
            {
                kvp.Value.CompleteMovement();
            }
            this.generalManager?.Complete();

            foreach (var kvp in this.managers)
            {
                kvp.Value.Clear();
            }

            this.targetMap.Clear();
            this.targets.Clear();
            this.playerTanks.Clear();
            this.targetDamagedHandlers.Clear();
            this.targetDestroyedHandlers.Clear();

            this.gravitySources.Clear();
        }

        public void RegisterPlayerTank(Target target)
        {
            this.playerTanks.Add(target);
        }

        private void UpdateTargets()
        {
            for (var i = 0; i < this.Targets.Count; i++)
            {
                var ship = this.Targets[i];
                var shipStruct = this.targetStructs[i];

                var pos = ship.transform.position;
                shipStruct.Position = Utils.ToXZ(pos);
                this.targetStructs[i] = shipStruct;

                this.targetPoints[i] = pos;
            }

            this.TargetTreeRebuildJob.Complete();

            var rebuildJob = new KnnRebuildJob(this.targetKdTree, this.Targets.Count);
            this.TargetTreeRebuildJob = rebuildJob.Schedule();
        }

        public void SpawnBullet(Target firedBy, float2 position, float2 forward, ProjectileWeapon weapon)
        {
            if (!this.managers.TryGetValue(weapon, out var specificManager))
            {
                this.managers[weapon] = specificManager = new ProjectileSpecificManager(weapon);
            }

            specificManager.SpawnBullet(firedBy, position, forward);
        }

        public void SpawnDamage(int byTarget, int byTeam, float2 position, float2 damageDirection, float radius, float damage)
        {
            this.generalManager?.AddDamagePoint(position, damageDirection, damage, radius, byTarget, byTeam);
        }

        public void TriggerTargetDestroyed(Target target)
        {
            if (this.targetDestroyedHandlers.Count == 0)
            {
                return;
            }

            var shipController = target.GetComponent<WaveShipController>();
            this.targetDestroyedHandlers.AddLock();
            try
            {
                if (this.targetDestroyedHandlers.UnsafeItems.TryGetValue(target, out var list))
                {
                    foreach (var handler in list)
                    {
                        handler.Invoke(target, shipController);
                    }
                }
            }
            finally
            {
                this.targetDestroyedHandlers.RemoveLock();
            }

            this.OnTargetDestroyed?.Invoke(target, shipController);
        }

        public void RegisterTargetDestroyedHandler(Target target, TargetDestroyedHandler handler)
        {
            this.targetDestroyedHandlers.Add(target, handler);
        }

        public void DeregisterTargetDestroyedHandler(Target target, TargetDestroyedHandler handler)
        {
            this.targetDestroyedHandlers.Remove(target, handler);
        }

        public void TriggerTargetDamaged(Target target, float damageAmount, ProjectileWeapon? source)
        {
            if (this.targetDamagedHandlers.Count == 0)
            {
                return;
            }

            this.targetDamagedHandlers.AddLock();
            try
            {
                if (this.targetDamagedHandlers.UnsafeItems.TryGetValue(target, out var list))
                {
                    foreach (var handler in list)
                    {
                        handler.Invoke(target, damageAmount, source);
                    }
                }
            }
            finally
            {
                this.targetDamagedHandlers.RemoveLock();
            }
        }

        public void RegisterTargetDamagedHandler(Target target, TargetDamagedHandler handler)
        {
            this.targetDamagedHandlers.Add(target, handler);
        }

        public void DeregisterTargetDamagedHandler(Target target, TargetDamagedHandler handler)
        {
            this.targetDamagedHandlers.Remove(target, handler);
        }

        private void HandleRegisteringTargets()
        {
            foreach (var target in this.deregisterTargets)
            {
                this.DoDeregisterTarget(target);
            }
            this.deregisterTargets.Clear();

            foreach (var target in this.registerTargets)
            {
                this.DoRegisterTarget(target);
            }
            this.registerTargets.Clear();
        }

        public void RegisterTarget(Target target)
        {
            this.registerTargets.Add(target);
            this.deregisterTargets.Remove(target);
        }

        private void DoRegisterTarget(Target target)
        {
            this.targetMap[target.Id] = target;
            this.targetStructs[this.targets.Count] = new TargetStruct
            {
                Id = target.Id,
                Team = target.Team,
                Position = Utils.ToXZ(target.transform.position),
                Size = target.Size
            };

            if (!this.targets.Contains(target))
            {
                this.targets.Add(target);

                this.OnTargetRegistered?.Invoke(target, removed: false);
            }
            else
            {
                Debug.Log("Target already registered");
            }
        }

        public void DeregisterTarget(Target target)
        {
            this.registerTargets.Remove(target);
            this.deregisterTargets.Add(target);
        }

        private void DoDeregisterTarget(Target target)
        {
            var index = this.targets.FindIndex(s => s == target);
            if (index >= 0)
            {
                for (var i = index; i < this.targets.Count - 1; i++)
                {
                    this.targetStructs[i] = this.targetStructs[i + 1];
                }

                this.OnTargetRegistered?.Invoke(target, removed: true);

                this.targets.RemoveAt(index);
                this.targetMap.Remove(target.Id);
                this.targetDamagedHandlers.Remove(target);
                this.targetDestroyedHandlers.Remove(target);
            }
        }

        public void AddGravitySources(StarSystem starSystem)
        {
            var beforeLength = this.gravitySources.Count;
            this.gravitySources.AddDistinctRange(starSystem.AllGravitySources);
            Debug.Log($"Added gravity sources: {beforeLength} -> {this.gravitySources.Count}");
            this.UpdateGravitySourceLocations();
        }

        public void AddGravitySources(LevelContainer level)
        {
            var beforeLength = this.gravitySources.Count;
            this.gravitySources.AddDistinctRange(level.GravitySourcesForLevel);
            Debug.Log($"Added gravity sources: {beforeLength} -> {this.gravitySources.Count}");
            this.UpdateGravitySourceLocations();
        }

        public void RemoveGravitySources(StarSystem starSystem)
        {
            var beforeLength = this.gravitySources.Count;
            foreach (var source in starSystem.AllGravitySources)
            {
                this.gravitySources.Remove(source);
            }

            Debug.Log($"Removed gravity sources: {beforeLength} -> {this.gravitySources.Count}");
            this.UpdateGravitySourceLocations();
        }

        public void UpdateGravitySourceLocations()
        {
            for (var i = 0; i < this.gravitySources.Count; i++)
            {
                var source = this.gravitySources[i];
                var targetId = -1;
                var teamId = -1;
                if (source.gameObject.TryGetComponent<Target>(out var gravityTarget))
                {
                    targetId = gravityTarget.Id;
                    teamId = gravityTarget.Team;
                }

                this.gravitySourceStructs[i] = new GravitySourceStruct
                {
                    Position = Utils.ToXZ(source.transform.position),
                    TargetId = targetId,
                    TeamId = teamId,
                    InnerRange = source.InnerRange,
                    OuterRange = source.OuterRange,
                    DiffRange = source.OuterRange - source.InnerRange,
                    Strength = source.Strength
                };
            }
        }

        public void UpdatePortals()
        {
            if (PortalManager.Instance.Portal1 != null)
            {
                this.portalStructs[0] = CreateFrom(PortalManager.Instance.Portal1);
            }
            if (PortalManager.Instance.Portal2 != null)
            {
                this.portalStructs[1] = CreateFrom(PortalManager.Instance.Portal2);
            }
        }

        private static PortalStruct CreateFrom(Portal portal)
        {
            return new PortalStruct
            {
                Position = Utils.ToXZ(portal.transform.position),
                OpenSize = portal.OpenSize,
                PortalsTo = Utils.ToXZ(portal.ConnectedTo.transform.position)
            };
        }
        #endregion
    }
}