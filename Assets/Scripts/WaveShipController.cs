using UnityEngine;
using UnityEngine.Splines;

#nullable enable

namespace Orbits
{
    public class WaveShipController : MonoBehaviour
    {
        #region Fields
        public static readonly Quaternion LandscapeRotation = Quaternion.identity;
        public static readonly Quaternion PortraitRotation = Quaternion.Euler(0, 90, 0);

        public GameObject Prefab;
        public float PrevTimePercent;
        public float CurrentTimePercent;
        public int Money = -1;
        public IWaveShipMovement Movement = EmptyShipMovement.Instance;
        public Target Target;
        public Wave? PartOfWave;
        public bool HitPlanet = false;
        public Renderer? HealthBar;
        public GameObject? SpawnEffect;
        #endregion

        #region Unity Methods
        private void OnEnable()
        {
            this.HitPlanet = false;
            if (this.Target == null)
            {
                this.Target = this.GetComponent<Target>();
            }

            ProjectileManager.Instance.RegisterTargetDestroyedHandler(this.Target, this.OnDestroyed);
        }

        private void OnDisable()
        {
            ProjectileManager.Instance.DeregisterTargetDestroyedHandler(this.Target, this.OnDestroyed);
            GameObjectPools.Instance.Release(this.Prefab, this.gameObject);
        }
        #endregion

        #region Methods
        public void Init(IWaveShipMovement movement, Wave? partOfWave, GameObject prefab)
        {
            this.Movement = movement;
            movement.UpdateFor(this.transform, 0.0f);
            if (this.SpawnEffect != null)
            {
                var pos = this.transform.position;
                var viewportPoint = Camera.main.WorldToViewportPoint(pos, Camera.MonoOrStereoscopicEye.Mono);
                if (viewportPoint.x > -0.05f && viewportPoint.x < 1.05f &&
                    viewportPoint.y > -0.05f && viewportPoint.y < 1.05f)
                {
                    var effect = GameObjectPools.Instance.Spawn(this.SpawnEffect);
                    effect.transform.position = pos;
                }
            }

            this.PartOfWave = partOfWave;
            this.Prefab = prefab;
        }

        public void ManagedUpdate(float dt)
        {
            this.Target.ManagedUpdate(dt);
            this.Target.SetHealthBar(this.HealthBar);

            if (this.Movement != null)
            {
                this.PrevTimePercent = this.CurrentTimePercent;
                this.CurrentTimePercent = this.Movement.CurrentTimePercent;
                this.Movement.UpdateFor(this.transform, dt);
            }

            // if (this.PartOfWave != null)
            // {
            //     this.CurrentTimePercent = Mathf.Clamp01(time / this.SplineTime);
            //     this.CurrentTime = Mathf.Clamp(this.CurrentTime + dt, 0.0f, this.PartOfWave.SplineTime);

            //     var position = this.PartOfWave.CalculatePosition(this.CurrentTime, this.SplineIndex, out this.CurrentTimePercent) + this.PathOffset;
            //     this.transform.position = position;
            // }
        }

        private void OnDestroyed(Target target, WaveShipController? waveShipController)
        {
            if (GameManager.Instance.CurrentLevel != null && !this.HitPlanet)
            {
                GameManager.Instance.CurrentLevel.GetMoney(this.Money);
            }

            WaveShipManager.Instance.Deregister(this);
        }
        #endregion
    }
}