using Unity.Burst;
using Unity.Mathematics;

#nullable enable

namespace Orbits
{
    [BurstCompile(CompileSynchronously = true)]
    public struct PortalStruct
    {
        #region Fields
        public float2 Position;
        public float OpenSize;
        public float2 PortalsTo;
        #endregion
    }
}