using Unity.Burst;
using Unity.Mathematics;

#nullable enable

namespace Orbits
{
    [BurstCompile(CompileSynchronously = true)]
    public struct GravitySourceStruct
    {
        #region Fields
        public float2 Position;
        public int TargetId;
        public int TeamId;
        public float InnerRange;
        public float OuterRange;
        public float DiffRange;
        public float Strength;
        #endregion

        #region Methods
        public readonly float2 CalculateGravityParabolic(float2 target)
        {
            var toTarget = this.Position - target; // Negative so that it points to the gravity source
            var distance = math.length(toTarget);
            if (distance > this.OuterRange)
            {
                return float2.zero;
            }

            var toTargetNormalised = math.normalize(toTarget);
            if (distance <= this.InnerRange)
            {
                return this.Strength * toTargetNormalised;
            }

            var targetDiff = distance - this.InnerRange;
            var amount = math.pow((targetDiff - this.DiffRange) / this.DiffRange, 2.0f);
            return amount * this.Strength * toTargetNormalised;
        }

        public readonly float2 CalculateGravityLinear(float2 target)
        {
            var toTarget = this.Position - target; // Negative so that it points to the gravity source
            var distance = math.length(toTarget);
            if (distance > this.OuterRange)
            {
                return float2.zero;
            }

            var toTargetNormalised = math.normalize(toTarget);
            if (distance <= this.InnerRange)
            {
                return this.Strength * toTargetNormalised;
            }

            var targetDiff = distance - this.InnerRange;
            return math.lerp(this.Strength, 0.0f, targetDiff / this.DiffRange) * toTargetNormalised;
        }
        #endregion
    }
}