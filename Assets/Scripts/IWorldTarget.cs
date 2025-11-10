using UnityEngine;

#nullable enable

namespace Orbits
{
    public interface IWorldTarget
    {
        Vector3 WorldPosition { get; }
    }

    public readonly struct WorldOffsetTarget : IWorldTarget
    {
        public readonly IWorldTarget Target;
        public readonly Vector3 Offset;

        public Vector3 WorldPosition => this.Target.WorldPosition + this.Offset;

        public WorldOffsetTarget(IWorldTarget target, Vector3 offset)
        {
            this.Target = target;
            this.Offset = offset;
        }
    }

    public readonly struct WorldTransformTarget : IWorldTarget
    {
        public readonly Transform Target;
        public readonly Vector3 Offset;

        public Vector3 WorldPosition => this.Target.position;

        public WorldTransformTarget(Transform target)
        {
            this.Target = target;
            this.Offset = Vector3.zero;
        }

        public WorldTransformTarget(Transform target, Vector3 offset)
        {
            this.Target = target;
            this.Offset = offset;
        }
    }

    public readonly struct WorldStaticTarget : IWorldTarget
    {
        public static readonly WorldStaticTarget Zero = new(Vector3.zero);
        public readonly Vector3 Target;

        public Vector3 WorldPosition => this.Target;

        public WorldStaticTarget(Vector3 target)
        {
            this.Target = target;
        }
    }

    public static class IWorldOffsetExtensions
    {
        public static WorldOffsetTarget WithOffset(this IWorldTarget self, Vector3 offset)
        {
            if (self is WorldOffsetTarget selfOffset)
            {
                return new WorldOffsetTarget(selfOffset.Target, offset);
            }

            return new WorldOffsetTarget(self, offset);
        }
    }
}