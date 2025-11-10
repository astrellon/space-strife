using System;
using System.Collections.Generic;
using System.Data;
using Unity.Mathematics;
using UnityEngine;

#nullable enable

namespace Orbits
{
    [DefaultExecutionOrder(1)]
    public class WaveShipManager : MonoBehaviour
    {
        #region Fields
        public static WaveShipManager Instance { get; private set; }

        private readonly List<WaveShipController> waveShips = new();
        private readonly List<WaveShipController> toDestroy = new();
        private readonly List<WaveShipController> waveSpawners = new();

        public IReadOnlyList<WaveShipController> WaveShips => this.waveShips;

        public bool Clearing { get; private set; }
        #endregion

        #region Unity Methods
        private void Awake()
        {
            Instance = this;
        }

        private void Update()
        {
            if (!GameManager.Instance.GameActive ||
                GameManager.Instance.CurrentLevel == null ||
                !GameManager.Instance.CurrentLevel.enabled)
            {
                return;
            }

            var dt = Time.deltaTime;
            dt *= GameManager.Instance.TimeAccessibleWaveScale;

            foreach (var controller in this.waveShips)
            {
                controller.ManagedUpdate(dt);
                if (controller.PrevTimePercent < 1.0f && controller.CurrentTimePercent >= 1.0f)
                {
                    if (controller.PartOfWave != null && controller.PartOfWave.DamagesPlanet)
                    {
                        controller.HitPlanet = true;
                        this.toDestroy.Add(controller);
                        GameManager.Instance.DamagePlanet(1);
                    }
                }
            }

            foreach (var controller in this.waveSpawners)
            {
                controller.ManagedUpdate(dt);
            }

            foreach (var controller in this.toDestroy)
            {
                controller.Target.DestroyTarget(triggerExplode: true, immediate: false);
            }
            this.toDestroy.Clear();
        }
        #endregion

        #region Methods
        public void Clear()
        {
            this.Clearing = true;
            for (var i = 0; i < this.waveShips.Count; i++)
            {
                this.waveShips[i].Target.DestroyTarget(triggerExplode: false, immediate: true);
            }
            this.waveShips.Clear();

            for (var i = 0; i < this.waveSpawners.Count; i++)
            {
                this.waveSpawners[i].Target.DestroyTarget(triggerExplode: false, immediate: true);
            }
            this.waveSpawners.Clear();

            this.Clearing = false;
        }

        public void RegisterWaveSpawner(WaveShipController controller)
        {
            this.waveSpawners.Add(controller);
        }

        public void Deregister(WaveShipController controller)
        {
            if (!this.Clearing)
            {
                this.waveShips.Remove(controller);
            }
        }

        public GameObject Spawn(Wave forWave, GameObject prefab)
        {
            var newObj = GameObjectPools.Instance.Spawn(prefab);
            if (!newObj.TryGetComponent<WaveShipController>(out var controller))
            {
                return newObj;
            }

            // TODO: Check how best to prevent the same ship from being added multiple times.
            this.waveShips.Add(controller);

            if (forWave.Spline != null)
            {
                var splineCount = forWave.Spline.Splines.Count;
                var splineIndex = splineCount == 1 ? 0 : UnityEngine.Random.Range(0, splineCount);
                var offsetX = UnityEngine.Random.value - 0.5f;
                var offsetZ = UnityEngine.Random.value - 0.5f;
                var pathOffset = new float3(offsetX, 0, offsetZ);
                var movement = new SplineShipMovement(forWave.Spline, splineIndex, forWave.SplineTime, pathOffset, loops: forWave.LoopSpline);
                controller.Init(movement, forWave, prefab);
            }
            else if (forWave.RandomPlacement != null)
            {
                var pos = forWave.RandomPlacement.CalculatePos((uint)forWave.NumSpawned);

                controller.Init(EmptyShipMovement.Instance, forWave, prefab);
                newObj.transform.position = pos;
            }
            else
            {
                controller.Init(EmptyShipMovement.Instance, forWave, prefab);
                newObj.transform.position = forWave.transform.position;
            }
            return newObj;
        }
        #endregion
    }
}