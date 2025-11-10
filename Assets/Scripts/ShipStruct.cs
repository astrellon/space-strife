using System;
using System.Linq;
using Unity.Mathematics;

#nullable enable

namespace Orbits
{
    public struct TargetStruct
    {
        #region Fields
        public int Id;
        public int Team;
        public float2 Position;
        public float Size;
        #endregion

        #region Methods
        public readonly bool DidHit(float2 target)
        {
            var distance = math.distance(this.Position, target);
            return distance <= this.Size;
        }
        #endregion
    }
}