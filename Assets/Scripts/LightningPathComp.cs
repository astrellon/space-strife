using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

#nullable enable

namespace Orbits
{
    public class LightningPathComp : MonoBehaviour
    {
        #region Fields
        public Transform? From;
        public Transform? To;

        public int Generations = 2;
        public float Chaos = 0.1f;
        public float StartingWidth = 0.4f;
        public int NumForks = 4;
        public Vector3 UpDirection = Vector3.up;
        public Vector2 DamageRange = new(7, 15);

        public float ShowSpeed = 1.0f;
        public float ShowAmount = 0.0f;
        public MeshRenderer? Renderer;
        public MeshFilter? MeshFilter;
        public Material? Material;

        private List<LightningPath.Path> paths = new();
        private Material? copiedMaterial;

        private readonly List<Vector3> dealtDamageAt = new();

        public LightningPathComp? GameObjectPrefab;

        private List<Vector3> meshVertices = new();
        private List<Vector2> meshUvs = new();
        private List<int> meshTriangles = new();
        #endregion

        #region Unity Methods
        private void OnEnable()
        {
            if (this.From == null || this.To == null || this.MeshFilter == null || this.Material == null || this.Renderer == null)
            {
                return;
            }

            if (this.copiedMaterial == null)
            {
                this.copiedMaterial = new Material(this.Material);
                this.Renderer.material = this.copiedMaterial;
            }

            this.dealtDamageAt.Clear();
            this.ShowAmount = this.GameObjectPrefab.ShowAmount;

            var from = this.From.transform.localPosition;
            var to = this.To.transform.localPosition;

            var pathCreator = new LightningPath(from, to, this.Generations, this.Chaos, this.NumForks);
            var sw = System.Diagnostics.Stopwatch.StartNew();
            this.paths = pathCreator.Execute();
            var timeToCreatePaths = sw.Elapsed;
            sw.Restart();

            var combinedMesh = CreateMesh();
            var timeToCreateMesh = sw.Elapsed;
            sw.Stop();
            this.MeshFilter.mesh = combinedMesh;

            Debug.Log($"Time to create lightning, paths: {timeToCreatePaths.Ticks}ticks, mesh: {timeToCreateMesh.Ticks}ticks");
        }

        private void Update()
        {
            if (this.copiedMaterial == null)
            {
                return;
            }

            this.ShowAmount += Time.deltaTime * this.ShowSpeed;
            this.copiedMaterial.SetFloat("_Show", this.ShowAmount);

            var thisPos = this.transform.position;

            if (this.DamageRange.y > 0)
            {
                var fromXY = Utils.ToXZ(this.From.position);
                foreach (var path in this.paths)
                {
                    foreach (var point in path.CalculateChange(this.ShowAmount))
                    {
                        var pos = (Vector3)point + thisPos;
                        this.dealtDamageAt.Add(pos);
                        var position = Utils.ToXZ(pos);
                        var damage = UnityEngine.Random.Range(this.DamageRange.x, this.DamageRange.y);
                        var direction = math.normalize(position - fromXY) * damage * 0.2f;
                        ProjectileManager.Instance.SpawnDamage(-1, Target.PlayerTeam, position, direction, 2.0f, damage);
                    }
                }
            }

            if ((this.ShowAmount > 10.0f && this.ShowSpeed > 0) ||
                (this.ShowAmount < -10.0f && this.ShowSpeed < 0))
            {
                this.enabled = false;
                if (this.GameObjectPrefab != null)
                {
                    GameObjectPools.Instance.Release(this.GameObjectPrefab.gameObject, this.gameObject);
                }
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.white;
            Gizmos.matrix = this.transform.localToWorldMatrix;

            foreach (var path in this.paths)
            {
                for (var i = 0; i < path.Points.Count - 1; i++)
                {
                    var from = path.Points[i];
                    var to = path.Points[i + 1];
                    Gizmos.DrawLine(from, to);
                }
            }

            Gizmos.color = Color.red;
            foreach (var point in this.dealtDamageAt)
            {
                Gizmos.DrawWireSphere(point, 1.0f);
            }
        }
        #endregion

        #region Methods
        private Mesh CreateMesh()
        {
            CalculateCounts(this.paths, out var vertexCount, out var triangleCount);

            this.meshTriangles.Clear();
            this.meshUvs.Clear();
            this.meshVertices.Clear();

            this.meshVertices.Capacity = vertexCount;
            this.meshUvs.Capacity = vertexCount;
            this.meshTriangles.Capacity = triangleCount;

            var vIndex = 0;
            var up = (float3)this.UpDirection;
            foreach (var path in this.paths)
            {
                var from = path.Points[0];
                var to = path.Points[1];
                var dir = math.normalize(to - from);

                var width = (float)path.NumGenerations / (float)this.Generations * this.StartingWidth;

                var cross = math.cross(dir, up) * width;

                var v1 = from + cross;
                var v3 = from - cross;

                var uv = new Vector2(path.StartOffset, 0);
                this.meshUvs.Add(uv);
                this.meshVertices.Add(v1);
                vIndex++;

                this.meshUvs.Add(uv);
                this.meshVertices.Add(v3);
                vIndex++;

                for (var i = 1; i < path.Points.Count - 1; i++)
                {
                    from = to;
                    to = path.Points[i + 1];
                    dir = math.normalize(to - from);

                    var percent = (float)i / path.Points.Count;

                    cross = math.cross(dir, up) * (width * Mathf.Clamp01((1.0f - percent) * 3.0f));
                    var v2 = to + cross;
                    var v4 = to - cross;

                    uv = new Vector2(percent + path.StartOffset, 0);

                    this.meshUvs.Add(uv);
                    this.meshVertices.Add(v2);
                    vIndex++;

                    this.meshUvs.Add(uv);
                    this.meshVertices.Add(v4);
                    vIndex++;

                    this.meshTriangles.Add(vIndex - 3 - 1);
                    this.meshTriangles.Add(vIndex - 1 - 1);
                    this.meshTriangles.Add(vIndex - 2 - 1);

                    this.meshTriangles.Add(vIndex - 2 - 1);
                    this.meshTriangles.Add(vIndex - 1 - 1);
                    this.meshTriangles.Add(vIndex - 1);

                    v1 = v2;
                    v3 = v4;
                }
            }

            var mesh = new Mesh();
            mesh.SetVertices(this.meshVertices);
            mesh.SetUVs(0, this.meshUvs);
            mesh.SetTriangles(this.meshTriangles, 0);
            mesh.Optimize();
            return mesh;
        }

        private static void CalculateCounts(IEnumerable<LightningPath.Path> paths, out int vertexCount, out int triangleCount)
        {
            var totalVertexCount = 0;
            var totalTriangleCount = 0;
            foreach (var path in paths)
            {
                CalculateCounts(path, out var vCount, out var tCount);
                totalVertexCount += vCount;
                totalTriangleCount += tCount;
            }

            vertexCount = totalVertexCount;
            triangleCount = totalTriangleCount;
        }

        private static void CalculateCounts(LightningPath.Path path, out int vertexCount, out int triangleCount)
        {
            vertexCount = (path.Points.Count - 1) * 2 + 1;
            triangleCount = (path.Points.Count - 2) * 6 + 3;
        }
        #endregion
    }
}