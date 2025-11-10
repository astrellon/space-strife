
using System.Collections.Generic;
using Orbits.Extensions;
using UnityEngine;

#nullable enable

namespace Orbits
{
    [DefaultExecutionOrder(-10)]
    public class Portal : MonoBehaviour
    {
        public struct NearbyTarget
        {
            public readonly PortalableTarget Target;

            public NearbyTarget(PortalableTarget target)
            {
                this.Target = target;
            }
        }

        #region Fields
        public GameObject Prefab;
        public bool Show;
        public float Countdown = 0;
        public float MaxCountdown = 6.0f;
        public float PortalWidth = 1.0f;
        public Light Light;
        public float OriginalIntensity;
        // public float LightAmount;
        public ParticleSystem ParticleSystem;
        public Portal ConnectedTo;
        public PortalShadow? PortalShadow;
        public List<Transform> ScaleWithShadow = new();

        public IWorldTarget TargetPosition = WorldStaticTarget.Zero;

        public float OpenAmount = 0.0f;
        public float OpenAmountPercent = 0.0f;
        public float OpenSize => this.OpenAmount * this.PortalWidth;

        private readonly List<NearbyTarget> nearbyTargets = new();
        public IReadOnlyList<NearbyTarget> NearbyTargets => this.nearbyTargets;

        private readonly List<PortalableTarget> moveTargets = new();
        #endregion

        #region Unity Methods
        private void Awake()
        {
            this.OriginalIntensity = this.Light.intensity;
        }

        private void Update()
        {
            this.transform.position = this.TargetPosition.WorldPosition;

            if (this.moveTargets.Count > 0)
            {
                foreach (var target in this.moveTargets)
                {
                    this.DoMoveTarget(target);
                }
                this.moveTargets.Clear();
            }

            if (this.ParticleSystem.isPlaying && !this.Show)
            {
                this.ParticleSystem.Stop(withChildren: true);
            }
            else if (!this.ParticleSystem.isPlaying && this.Show)
            {
                this.ParticleSystem.Play(withChildren: true);
            }

            var change = this.Show ? Time.deltaTime : -Time.deltaTime;
            this.OpenAmountPercent = Mathf.Clamp01(this.OpenAmountPercent + change);
            this.OpenAmount = PortalShadow.PortalShadowLerp(this.OpenAmountPercent);

            var noise = 0.9f + 0.1f * Mathf.PerlinNoise1D(Time.timeSinceLevelLoad);
            this.Light.intensity = this.OpenAmount * this.OriginalIntensity * noise;

            var toScaleSize = Vector3.one * this.OpenSize;
            foreach (var toScale in this.ScaleWithShadow)
            {
                toScale.localScale = toScaleSize;
            }

            if (this.OpenAmount > 0.1f)
            {
                var size = this.OpenSize;
                var pos = this.transform.position;
                for (var i = 0; i < this.nearbyTargets.Count; i++)
                {
                    var nearby = this.nearbyTargets[i];
                    if (nearby.Target.InsidePortal > 0 || nearby.Target.Radius > size)
                    {
                        continue;
                    }

                    var toPortal = nearby.Target.transform.position - pos;
                    if (toPortal.magnitude + nearby.Target.Radius < size)
                    {
                        this.MoveTarget(nearby.Target);
                    }
                }
            }

            if (!this.ParticleSystem.isPlaying && !this.Show && this.OpenAmountPercent <= 0.0f)
            {
                this.Countdown += Time.deltaTime;
                if (this.Countdown > this.MaxCountdown)
                {
                    PortalManager.Instance.ReleasePortal(this);
                    GameObjectPools.Instance.Release(this.Prefab, this.gameObject);
                }
            }
            else
            {
                this.Countdown = 0.0f;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.TryGetComponent<PortalableTarget>(out var target))
            {
               Debug.Log($"{target.name} entered portal {this.name}");
                this.nearbyTargets.Add(new NearbyTarget(target));
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.gameObject.TryGetComponent<PortalableTarget>(out var target))
            {
                Debug.Log($"{target.name} exited portal {this.name}");
                this.nearbyTargets.RemoveAll(t => t.Target == target);
                target.InsidePortal--;
            }
        }
        #endregion

        #region Methods
        public void Init(GameObject prefab)
        {
            this.Countdown = 0.0f;
            this.Prefab = prefab;
            this.ParticleSystem.Clear(withChildren: true);

            foreach (var toScale in this.ScaleWithShadow)
            {
                toScale.localScale = Vector3.zero;
            }
        }

        private void MoveTarget(PortalableTarget target)
        {
            this.moveTargets.AddDistinct(target);
        }

        private void DoMoveTarget(PortalableTarget target)
        {
            PortalManager.Instance.MoveTarget(target, this);

            if (target.IsPlayer && this.PortalShadow != null)
            {
                GameCamera.Instance.AfterCameraUpdate = this.AfterCameraUpdateMove;
            }
        }

        public void AfterCameraUpdateMove()
        {
            if (this.PortalShadow != null)
            {
                var shadow = this.PortalShadow;
                shadow.Flip();

                Debug.Log("Updating portals");
                PortalManager.Instance.UpdatePortals();

                shadow.UpdateMesh();
            }
        }
        #endregion
    }
}
