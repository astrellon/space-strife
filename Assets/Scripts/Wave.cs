using System;
using UnityEngine;
using UnityEngine.Splines;

#nullable enable

namespace Orbits
{
    public class Wave : MonoBehaviour
    {
        #region Fields
        public delegate void ShipsChangeHandler(Wave source, Target target, WaveShipController waveShipController);

        public event ShipsChangeHandler? OnShipSpawned;
        public event ShipsChangeHandler? OnShipDestroyed;

        public SplineContainer? Spline;
        public float SplineTime;
        public RandomWavePlacement? RandomPlacement;

        public GameObject Prefab;
        public int NumShips;
        public int NumSpawned;
        public int NumDestroyed;
        public int NumShipsLeft => this.NumShips - this.NumDestroyed;
        public float TimeBetweenSpawn;
        public bool DamagesPlanet;
        public bool LoopSpline;
        public float Delay;

        public GameObject? SpawnOnCanvas;

        public float OverrideHealth = -1.0f;
        public int EnemyMoneyDrop = 1;
        public int MaxShipsWithMoney = 0;

        private float spawnTime;
        #endregion

        #region Unity Methods
        private void FixedUpdate()
        {
            if (!GameManager.Instance.GameActive ||
                GameManager.Instance.CurrentLevel == null ||
                !GameManager.Instance.CurrentLevel.enabled)
            {
                return;
            }

            if (this.NumSpawned >= this.NumShips)
            {
                return;
            }

            var dt = Time.fixedDeltaTime * GameManager.Instance.TimeScaleRatio;
            this.Delay -= dt;
            if (this.Delay > 0)
            {
                return;
            }

            this.Delay = 0.0f;
            this.spawnTime -= dt;
            while (this.spawnTime <= 0.0f && this.NumSpawned < this.NumShips)
            {
                this.NumSpawned++;
                this.spawnTime += this.TimeBetweenSpawn;
                var newShip = WaveShipManager.Instance.Spawn(this, this.Prefab);
                newShip.name = $"{this.name}: {this.NumSpawned}";
                var newTarget = newShip.GetComponent<Target>();

                var newShipController = newShip.GetComponent<WaveShipController>();
                this.OnShipSpawned?.Invoke(this, newTarget, newShipController);

                if (this.OverrideHealth > 0.0f && newTarget != null)
                {
                    newTarget.MaxHealth = this.OverrideHealth;
                    newTarget.CurrentHealth = this.OverrideHealth;
                }
                if (newShipController != null)
                {
                    if (this.EnemyMoneyDrop > 0)
                    {
                        newShipController.Money = this.EnemyMoneyDrop;
                    }
                }
            }
        }

        private void OnEnable()
        {
            if (this.SpawnOnCanvas != null)
            {
                Instantiate(this.SpawnOnCanvas);
            }

            if (this.Spline != null)
            {
                for (var i = 0; i < this.Spline.Splines.Count; i++)
                {
                    var prefab = GameManager.Instance.IndicatorPrefab;
                    var newIndicator = GameObjectPools.Instance.Spawn(prefab);
                    newIndicator.Prefab = prefab.gameObject;
                    newIndicator.ForWave = this;
                    newIndicator.SplineIndex = i;
                }
            }

            ProjectileManager.Instance.OnTargetDestroyed += this.OnTargetDestroyed;
        }

        private void OnDisable()
        {
            ProjectileManager.Instance.OnTargetDestroyed -= this.OnTargetDestroyed;
        }
        #endregion

        #region Methods
        public Vector3 CalculatePosition(float time, int splineIndex, out float t)
        {
            if (this.Spline == null)
            {
                t = 0;
                return this.transform.position;
            }

            if (this.LoopSpline)
            {
                t = (time / this.SplineTime) % 1.0f;
            }
            else
            {
                t = Mathf.Clamp01(time / this.SplineTime);
            }
            return this.Spline.EvaluatePosition(splineIndex, t);
        }

        private void OnTargetDestroyed(Target target, WaveShipController? waveShipController)
        {
            if (waveShipController != null && waveShipController.PartOfWave == this)
            {
                this.NumDestroyed++;
                // Debug.Log($"Destroy wave for: {this.name} {this.NumShips}/{this.NumSpawned}/{this.NumDestroyed}");
                if (this.MaxShipsWithMoney > 0 && this.NumDestroyed > this.MaxShipsWithMoney)
                {
                    waveShipController.Money = 0;
                }

                this.OnShipDestroyed?.Invoke(this, target, waveShipController);
            }
        }
        #endregion
    }
}