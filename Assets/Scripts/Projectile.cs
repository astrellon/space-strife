using Unity.Burst;
using Unity.Mathematics;

#nullable enable

namespace Orbits
{
    [BurstCompile(CompileSynchronously = true)]
    public struct Projectile
    {
        public int FiredByTarget;
        public int FiredByTeam;
        public int ProjectileId;
        public float MaxAge;
        public float Age;
        public float2 PrevPosition;
        public float2 NextPosition;
        public float2 Velocity;
        public float2 WentThroughPortal;

        public const float InvalidPortalPosX = -1e10f;
        public const float InvalidPortalPosCheck = -5e9f;
        public static readonly float2 InvalidPortalPos = new(InvalidPortalPosX, 0);
    }
}