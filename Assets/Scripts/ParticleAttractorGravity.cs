using System;
using UnityEngine;

#nullable enable

namespace Orbits
{
    public class ParticleAttractorGravity : MonoBehaviour
    {
        #region Fields
        public Transform? RelativeParent;
        public Transform? Target;
        public float GravityRange = 10.0f;
        public float GravityStrength = 5f;
        public AnimatePlanet? ResetOnHide;

        private ParticleSystem? ps;
        private ParticleSystem.Particle[] particles = Array.Empty<ParticleSystem.Particle>();
        #endregion

        #region Methods
        void Start()
        {
            this.ps = this.GetComponent<ParticleSystem>();
            this.particles = new ParticleSystem.Particle[this.ps.main.maxParticles];

            if (this.ResetOnHide != null)
            {
                this.ResetOnHide.OnShowTypeChange += this.OnShowTypeChange;
            }
        }

        void Update()
        {
            if (this.Target == null || this.ps == null || this.RelativeParent == null)
            {
                return;
            }

            var numParticlesAlive = this.ps.GetParticles(this.particles);
            var step = this.GravityStrength * Time.deltaTime;
            var gravityCenter = this.Target.position;

            if (this.ps.main.simulationSpace == ParticleSystemSimulationSpace.Local)
            {
                for (var i = 0; i < numParticlesAlive; i++)
                {
                    var particlePos = this.particles[i].position;
                    var worldParticlePos = this.transform.TransformPoint(particlePos);

                    var gravity = GravitySource.CalculateGravity(worldParticlePos, gravityCenter, this.GravityRange);
                    var relativeGravity = this.Target.InverseTransformDirection(gravity);
                    this.particles[i].velocity -= relativeGravity * step;
                }
            }
            else if (this.ps.main.simulationSpace == ParticleSystemSimulationSpace.World)
            {
                for (var i = 0; i < numParticlesAlive; i++)
                {
                    var particlePos = this.particles[i].position;

                    var gravity = GravitySource.CalculateGravity(particlePos, gravityCenter, this.GravityRange);
                    this.particles[i].velocity -= gravity * step;
                }
            }

            this.ps.SetParticles(this.particles, numParticlesAlive);
        }

        void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            if (this.Target != null)
            {
                Gizmos.DrawWireSphere(this.Target.position, this.GravityRange);
                Gizmos.DrawLine(this.Target.position, this.transform.position);
            }
        }
        #endregion

        #region Methods
        private void OnShowTypeChange(AnimatePlanet.ShowType previous, AnimatePlanet.ShowType current, AnimatePlanet source)
        {
            if (previous == AnimatePlanet.ShowType.Hide && this.ps != null)
            {
                this.ps.Clear();
            }
        }
        #endregion
    }
}
