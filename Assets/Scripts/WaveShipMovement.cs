using System;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

#nullable enable

namespace Orbits
{
    public interface IWaveShipMovement
    {
        float CurrentTimePercent { get; }
        void UpdateFor(Transform transform, float dt);
    }

    public class EmptyShipMovement : IWaveShipMovement
    {
        public static readonly EmptyShipMovement Instance = new();

        public float CurrentTimePercent => 0.0f;
        public void UpdateFor(Transform transform, float dt) { }
    }

    public class SplineShipMovement : IWaveShipMovement
    {
        #region Fields
        public readonly SplineContainer SplineContainer;
        public readonly int SplineIndex;
        public readonly float SplineTime;
        public readonly float3 RandomOffset;
        public readonly bool Loops;

        public float CurrentTime { get; private set; }
        public float CurrentTimePercent { get; private set; }
        #endregion

        #region Constructor
        public SplineShipMovement(SplineContainer splineContainer, int splineIndex, float splineTime, float3 randomOffset, bool loops = false)
        {
            this.SplineContainer = splineContainer;
            this.SplineIndex = splineIndex;
            this.SplineTime = splineTime;
            this.RandomOffset = randomOffset;
            this.Loops = loops;
        }
        #endregion

        #region Methods
        public void UpdateFor(Transform transform, float dt)
        {
            this.CurrentTime += dt;
            if (this.Loops)
            {
                this.CurrentTimePercent = (this.CurrentTime / this.SplineTime) % 1.0f;
            }
            else
            {
                this.CurrentTimePercent = Mathf.Clamp01(this.CurrentTime / this.SplineTime);
            }

            var position = this.SplineContainer.EvaluatePosition(this.SplineIndex, this.CurrentTimePercent);
            transform.position = position + this.RandomOffset;
        }
        #endregion
    }
}