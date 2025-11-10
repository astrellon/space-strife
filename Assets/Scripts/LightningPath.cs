using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using System.Net;

#nullable enable

namespace Orbits
{
    public class LightningPath
    {
        #region Fields
        public readonly float3 From;
        public readonly float3 To;
        public readonly int Generations;
        public readonly float Chaos;
        public readonly int NumForks;
        public readonly Unity.Mathematics.Random Random;
        #endregion

        public class Path
        {
            #region Fields
            public readonly float StartOffset;
            public readonly int NumGenerations;
            public readonly List<float3> Points;

            private int lastIndex = -1;
            #endregion

            #region Constructor
            public Path(float3 from, float3 to, float startOffset, int numGenerations)
            {
                this.StartOffset = startOffset;
                this.NumGenerations = numGenerations;
                var capacity = CalculateCapacity(numGenerations);

                this.Points = new List<float3>(capacity) { from, to };
            }
            #endregion

            #region Methods
            public void Subdivide(LightningPath parent)
            {
                for (var g = 0; g < this.NumGenerations; g++)
                {
                    for (var i = this.Points.Count - 2; i >= 0; i--)
                    {
                        var from = this.Points[i];
                        var to = this.Points[i + 1];
                        var dir = math.normalize(to - from);

                        var length = math.distance(from, to);

                        var offset = (float3)UnityEngine.Random.onUnitSphere * (length * parent.Chaos);
                        var midPoint = from + dir * (length * 0.5f) + offset;
                        this.Points.Insert(i + 1, midPoint);
                    }
                }
            }

            public Path CreateForkedPathPoints(LightningPath parent)
            {
                var positionToForkAt = parent.Random.NextFloat(0.25f, 0.75f);
                var start = this.CalculatePositionAt(positionToForkAt, out var from, out var to);
                var length = math.distance(this.Points[0], this.Points[^1]);
                var dir = math.normalize(to - from);
                var offset = (float3)UnityEngine.Random.onUnitSphere * (length * 0.75f * parent.Chaos);
                var end = from + dir * (length * 0.5f) + offset;

                return new Path(start, end, positionToForkAt + this.StartOffset, Mathf.Max(1, this.NumGenerations - 1));
            }

            public float3 CalculatePositionAt(float t, out float3 from, out float3 to)
            {
                if (t <= 0.0f)
                {
                    from = this.Points[0];
                    to = this.Points[1];
                    return from;
                }

                if (t >= 1.0f)
                {
                    from = this.Points[^2];
                    to = this.Points[^1];
                    return to;
                }

                var position = Mathf.Floor(t * (this.Points.Count - 1));
                var index = Mathf.FloorToInt(position);
                var frac = math.frac(position);

                from = this.Points[index];
                to = this.Points[index + 1];
                return math.lerp(from, to, frac);
            }

            public IEnumerable<float3> CalculateChange(float t)
            {
                t -= this.StartOffset;

                if (t <= 0.0f)
                {
                    yield break;
                }

                var endIndex = this.Points.Count - 1;
                var index = Mathf.FloorToInt(t * endIndex);

                if (index > this.lastIndex)
                {
                    var start = Mathf.Max(0, this.lastIndex);
                    var end = Mathf.Min(endIndex, index);
                    for (var i = start; i <= end; i += 4)
                    {
                        yield return this.Points[i];
                    }

                    this.lastIndex = index;
                }
            }
            #endregion
        }

        #region Constructor
        public LightningPath(float3 from, float3 to, int generations, float chaos, int numForks)
        {
            this.From = from;
            this.To = to;
            this.Generations = generations;
            this.Chaos = chaos;
            this.NumForks = numForks;

            this.Random = CreateRandomFromNow();
        }
        #endregion

        #region Methods
        public List<Path> Execute()
        {
            var result = new List<Path>();
            var path = new Path(this.From, this.To, 0.0f, this.Generations);
            result.Add(path);
            path.Subdivide(this);

            for (var f = 0; f < this.NumForks; f++)
            {
                var randIndex = this.Random.NextInt(result.Count);
                var pathToFork = result[randIndex];
                var forkedPath = pathToFork.CreateForkedPathPoints(this);
                result.Add(forkedPath);
                forkedPath.Subdivide(this);
            }
            return result;
        }

        private static Unity.Mathematics.Random CreateRandomFromNow()
        {
            var now = (uint)(DateTime.UtcNow.Ticks & 0xFFFFFFFF);
            return new Unity.Mathematics.Random(now);
        }

        private static int CalculateCapacity(int generations)
        {
            return (generations - 1) * 2 + 1;
        }
        #endregion
    }
}