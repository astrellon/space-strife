using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#nullable enable

namespace Orbits
{
    [DefaultExecutionOrder(10)]
    public class LevelBoundary : MonoBehaviour
    {
        #region Fields
        public float OuterRadius;
        public float InnerRadius;
        public int Segments;
        public LevelBoundarySegment Prefab;
        public Vector3 ShowNear;
        public float ShowNearRadius = 10.0f;
        public int ShowNearbySegments = 4;
        public Material? BoundaryMaterial;

        private Mesh? mesh;
        private readonly Vector3[] vertices = new Vector3[4];
        private static readonly int[] Triangles = new int[]
        {
            0, 2, 3,
            0, 3, 1,
        };
        private static readonly Vector2[] Uvs = new Vector2[]
        {
            Vector2.zero,
            Vector2.zero,
            Vector2.one,
            Vector2.one,
        };

        private readonly Dictionary<int, LevelBoundarySegment> activeSegments = new();
        private readonly List<int> removedAny = new();
        #endregion

        #region Unity Methods
        private void OnEnable()
        {
            if (GameManager.Instance == null || GameManager.Instance.CurrentLevelContainer == null)
            {
                this.gameObject.SetActive(false);
            }

            this.Clear();
            this.UpdateVertices();

            if (this.mesh == null)
            {
                this.mesh = new Mesh();
            }

            this.mesh.vertices = this.vertices;
            this.mesh.triangles = Triangles;
            this.mesh.uv = Uvs;
            this.mesh.MarkModified();

            this.UpdateSegments();
        }

        private void OnDisable()
        {
            this.Clear();
        }

        private void Update()
        {
            if (GameManager.Instance.CurrentLevelContainer != null &&
                GameManager.Instance.CurrentLevelContainer.Ship != null)
            {
                this.ShowNear = GameManager.Instance.CurrentLevelContainer.Ship.transform.position;
            }
            this.UpdateSegments();
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(this.ShowNear, this.ShowNearRadius);

            Gizmos.color = Color.gray;
            Gizmos.matrix = this.transform.localToWorldMatrix;

            var angleStep = Mathf.PI / (float)this.Segments;

            for (var i = 0; i < this.Segments; i++)
            {
                var angle = i * angleStep * 2.0f;

                var pos1 = Utils.FromAngle(angle - angleStep, this.InnerRadius);
                var pos2 = Utils.FromAngle(angle + angleStep, this.InnerRadius);
                var pos3 = Utils.FromAngle(angle - angleStep, this.OuterRadius);
                var pos4 = Utils.FromAngle(angle + angleStep, this.OuterRadius);

                Gizmos.DrawLine(pos1, pos2);
                Gizmos.DrawLine(pos3, pos4);
            }
        }
        #endregion

        #region Methods
        private void Clear()
        {
            if (this.activeSegments.Count > 0)
            {
                Utils.RemoveAllChildren(this.transform);
                this.activeSegments.Clear();
            }
        }

        private void UpdateVertices()
        {
            var angleStep = Mathf.PI / (float)this.Segments;
            var posZ = Mathf.Sin(angleStep);
            var posX = Mathf.Cos(angleStep);
            var posX1 = posX * this.InnerRadius;
            var posX2 = posX * this.OuterRadius;

            this.vertices[0] = new Vector3(posX1, 0, posZ * this.InnerRadius);
            this.vertices[1] = new Vector3(posX1, 0, -posZ * this.InnerRadius);
            this.vertices[2] = new Vector3(posX2, 0, posZ * this.OuterRadius);
            this.vertices[3] = new Vector3(posX2, 0, -posZ * this.OuterRadius);
        }

        private void UpdateSegments()
        {
            var toShowNear = this.ShowNear - this.transform.position;
            var toShowNearLength = toShowNear.magnitude;
            if (toShowNearLength + this.ShowNearRadius < this.InnerRadius ||
                toShowNearLength - this.ShowNearRadius > this.OuterRadius)
            {
                this.Clear();
                return;
            }

            var toShowNearNorm = toShowNear.normalized;
            var angleToShowNear = Mathf.Atan2(-toShowNearNorm.z, toShowNearNorm.x);

            var segmentCount = Mathf.RoundToInt(angleToShowNear / (2.0f * Mathf.PI) * (float)this.Segments);

            var angleStep = 360.0f / (float)this.Segments;
            var boxPos = new Vector3(this.vertices[0].x + 0.5f, 0, 0);
            var boxSize = new Vector3(1, 1, this.vertices[0].z * 2.0f);

            foreach (var segment in this.activeSegments.Values)
            {
                segment.Needed = false;
            }

            for (var i = segmentCount - this.ShowNearbySegments; i <= segmentCount + this.ShowNearbySegments; i++)
            {
                var segmentId = this.CalculateSegmentId(i);
                if (this.activeSegments.TryGetValue(segmentId, out var current))
                {
                    current.Needed = true;
                    continue;
                }

                var instance = GameObjectPools.Instance.Spawn(this.Prefab);
                instance.name = "Segment: " + segmentId;
                instance.Collider.center = boxPos;
                instance.Collider.size = boxSize;
                instance.MeshFilter.mesh = this.mesh;
                instance.transform.SetParent(this.transform, false);

                var rotation = Quaternion.Euler(0, angleStep * i, 0);
                instance.transform.SetLocalPositionAndRotation(Vector3.zero, rotation);
                instance.Needed = true;

                this.activeSegments[segmentId] = instance;
            }

            this.removedAny.Clear();
            foreach (var kvp in this.activeSegments)
            {
                if (!kvp.Value.Needed)
                {
                    this.removedAny.Add(kvp.Key);
                    GameObjectPools.Instance.Release(this.Prefab.gameObject, kvp.Value.gameObject);
                }
            }

            foreach (var id in this.removedAny)
            {
                this.activeSegments.Remove(id);
            }
        }

        private int CalculateSegmentId(int segment)
        {
            if (segment < 0)
            {
                return this.Segments + segment % this.Segments;
            }
            return segment % this.Segments;
        }
        #endregion
    }
}