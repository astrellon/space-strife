using System;
using System.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

#nullable enable

namespace Orbits
{
    public class WaveSpawner : MonoBehaviour
    {
        #region Fields
        public Wave Target;
        public Target Prefab;
        public float OverrideHealth = -1.0f;
        public int EnemyMoneyDrop = 0;
        public SplineContainer? SpawnerMovement;
        public int SpawnerMovementIndex = 0;
        public float SpawnerMovementTime = 5.0f;

        private IWaveShipMovement? waveShipMovement;
        private Target? spawned;
        private WaveShipController? spawnedController;
        #endregion

        #region Unity Methods
        private void Start()
        {
            this.spawned = Instantiate(this.Prefab);

            GameManager.Instance.OnOrientationChange += this.OnOrientationChange;

            var pos = this.Target.Spline.EvaluatePosition(0);
            this.spawned.transform.position = pos;

            ProjectileManager.Instance.RegisterTargetDestroyedHandler(this.spawned, this.OnSpawnerDestroyed);

            if (this.OverrideHealth > 0.0f)
            {
                this.spawned.MaxHealth = this.OverrideHealth;
                this.spawned.CurrentHealth = this.OverrideHealth;
            }
            this.Target.NumShips++;
            this.Target.NumSpawned++;

            if (this.spawned.TryGetComponent<WaveShipController>(out spawnedController))
            {
                WaveShipManager.Instance.RegisterWaveSpawner(spawnedController);
                this.spawnedController.Prefab = this.Prefab.gameObject;
                if (this.EnemyMoneyDrop > 0)
                {
                    this.spawnedController.Money = this.EnemyMoneyDrop;
                }

                if (this.SpawnerMovement != null)
                {
                    this.waveShipMovement = new SplineShipMovement(this.SpawnerMovement, this.SpawnerMovementIndex, this.SpawnerMovementTime, float3.zero, loops: true);
                    this.spawnedController.Movement = this.waveShipMovement;
                }
            }

            this.Target.enabled = true;
            this.Target.OnShipSpawned += this.OnTargetShipSpawned;
        }

        private void Update()
        {
            if (this.spawnedController != null)
            {
                this.spawnedController.ManagedUpdate(Time.deltaTime);
            }
        }

        private void OnDisable()
        {
            if (this.spawned != null)
            {
                ProjectileManager.Instance.DeregisterTargetDestroyedHandler(this.spawned, this.OnSpawnerDestroyed);
            }

            GameManager.Instance.OnOrientationChange -= this.OnOrientationChange;
            this.Target.OnShipSpawned -= this.OnTargetShipSpawned;
        }
        #endregion

        #region Methods
        private void OnSpawnerDestroyed(Target target, WaveShipController? waveShipController)
        {
            this.Target.NumDestroyed++;
            // Debug.Log($"Destroyed wave spawner for: {this.Target.name} {this.Target.NumShips}/{this.Target.NumSpawned}/{this.Target.NumDestroyed}");
            this.enabled = false;
        }

        private void OnTargetShipSpawned(Wave source, Target target, WaveShipController waveShipController)
        {
            this.Target.NumShips++;
            if (this.waveShipMovement != null)
            {
                if (waveShipController.Movement is SplineShipMovement currentMovement)
                {
                    var randomOffset = currentMovement.RandomOffset;
                    var newSplineContainer = this.gameObject.AddComponent<SplineContainer>();
                    var onDisable = target.gameObject.AddComponent<RemoveComponentOnDisable>();
                    onDisable.ToRemove = newSplineContainer;

                    var spline = currentMovement.SplineContainer.Splines[currentMovement.SplineIndex];

                    var newSpline = new Spline(spline.Knots, false);
                    var spawnerWorldPos = this.spawned.transform.position;
                    var spawnerLocalPos = this.transform.InverseTransformPoint(spawnerWorldPos);
                    newSpline.SetKnot(0, new BezierKnot(spawnerLocalPos), BezierTangent.Out);
                    newSplineContainer.AddSpline(newSpline);
                    newSplineContainer.RemoveSplineAt(0);

                    var newMovement = new SplineShipMovement(newSplineContainer, 0, this.Target.SplineTime, randomOffset);
                    waveShipController.Movement = newMovement;
                }
            }
        }

        private void OnOrientationChange(bool landscape)
        {
            if (this.spawned != null && this.waveShipMovement != null)
            {
                var pos = this.Target.Spline.EvaluatePosition(0);
                this.spawned.transform.position = pos;
            }
        }
        #endregion
    }
}