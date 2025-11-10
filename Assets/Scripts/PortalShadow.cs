using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

#nullable enable

namespace Orbits
{
    public class PortalShadow : MonoBehaviour
    {
        public delegate void RenderTextureChangeHandler(PortalShadow source);

        #region Fields
        public Camera Camera;
        public MeshFilter MeshFilter;
        public MeshRenderer MeshRenderer;
        public float PortalWidth = 1.0f;
        public float PortalLength = 100.0f;
        public Vector3 PortalLeft = Vector3.left;
        public Vector3 PortalForward = Vector3.zero;
        public CustomRenderTexture RenderTexture;
        public CustomRenderTexture RenderTextureMask;
        public Transform? CenterMask;

        public event RenderTextureChangeHandler? OnRenderTextureChange;

        public bool Show;

        public float OpenAmount = 0.0f;

        [Range(0.0f, 1.0f)]
        public float OpenAmountPercent = 0.0f;
        public float OpenSize => this.OpenAmount * this.PortalWidth;
        private Mesh? mesh;
        private readonly Vector3[] vertices = new Vector3[9];
        private static readonly int[] Triangles = new int[]
        {
            0, 2, 3,
            0, 3, 7,
            7, 3, 4,
            7, 4, 8,
            8, 4, 5,
            8, 5, 1,
            1, 5, 6
        };
        private static readonly Vector2[] Uvs = new Vector2[]
        {
            Vector2.zero,
            Vector2.zero,
            Vector2.zero,
            Vector2.one,
            Vector2.one,
            Vector2.one,
            Vector2.zero,
            Vector2.one,
            Vector2.one
        };

        /*
                2
        0        3
        7
                  4
        8
        1        5
                6

        |----A-----B
        A = 0, 5
        B = 0, 10
        AB = B - A, 0,5
        BA = A - B, 0,-5
        */
        private bool initedRenderTexture = false;
        private int lockForwardCounter = 0;
        #endregion

        #region Unity Methods
        private void Start()
        {
            this.UpdateVertices();

            this.mesh = new Mesh();
            this.mesh.MarkDynamic();
            this.mesh.vertices = this.vertices;
            this.mesh.triangles = Triangles;
            this.mesh.uv = Uvs;

            this.MeshFilter.mesh = this.mesh;
        }

        private void Update()
        {
            if (this.mesh != null)
            {
                var change = Time.deltaTime;
                if (!this.Show)
                {
                    change = -change;
                }

                this.OpenAmountPercent = Mathf.Clamp01(this.OpenAmountPercent + change);
                this.OpenAmount = PortalShadowLerp(this.OpenAmountPercent);

                this.mesh.Clear();
                if (this.OpenAmount > 0.0f)
                {
                    this.MeshRenderer.enabled = true;
                    this.UpdateMesh();

                    if (this.CenterMask != null)
                    {
                        this.CenterMask.gameObject.SetActive(true);
                        this.CenterMask.localScale = Vector3.one * this.OpenSize;
                    }
                }
                else
                {
                    this.MeshRenderer.enabled = false;

                    if (this.CenterMask != null)
                    {
                        this.CenterMask.gameObject.SetActive(false);
                    }
                }
            }

            var renderSize = this.GetRenderTextureSize();
            if (renderSize.x != this.RenderTexture.width ||
                renderSize.y != this.RenderTexture.height)
            {
                this.UpdateRenderTexture(renderSize);
            }
        }
        #endregion

        #region Methods
        public void Clear()
        {
            this.PortalForward = Vector3.zero;
        }

        public void Flip(int lockForwardCounter = 6)
        {
            this.PortalForward = -this.PortalForward;
            this.PortalLeft = -this.PortalLeft;
            this.lockForwardCounter = lockForwardCounter;
        }

        public void UpdateMesh()
        {
            if (this.mesh == null)
            {
                return;
            }

            this.UpdateVertices();
            this.mesh.vertices = this.vertices;
            this.mesh.triangles = Triangles;
            this.mesh.uv = Uvs;
            this.mesh.MarkModified();
        }

        public void InitRenderTexture()
        {
            if (this.initedRenderTexture)
            {
                return;
            }

            this.UpdateRenderTexture(this.GetRenderTextureSize());
            this.initedRenderTexture = true;
        }

        private Vector2Int GetRenderTextureSize()
        {
            var renderSize = new Vector2Int(Screen.width, Screen.height);
            if (QualitySettings.GetQualityLevel() == 0)
            {
                renderSize *= 2;
                renderSize /= 3;
            }

            return renderSize;
        }

        public void UpdateRenderTexture(Vector2Int renderSize)
        {
            Debug.Log($"Resized portal shadow to: {renderSize.x}x{renderSize.y}");

            if (this.RenderTexture.width != Screen.width &&
                this.RenderTexture.height != Screen.height)
            {
                this.RenderTexture.Release();
                this.RenderTexture = new(Screen.width, Screen.height, this.RenderTexture.graphicsFormat);
            }

            this.RenderTextureMask.Release();
            this.RenderTextureMask = new(renderSize.x, renderSize.y, this.RenderTextureMask.graphicsFormat);

            this.OnRenderTextureChange?.Invoke(this);
        }

        private void UpdateVertices()
        {
            var start = this.transform.position;
            var cameraPos = Utils.TakeXZ(this.Camera.transform.position);

            var cameraToStart = start - cameraPos;

            if (this.lockForwardCounter > 0)
            {
                this.lockForwardCounter--;
            }
            else
            {
                var dist = cameraToStart.magnitude;
                if (dist > this.PortalWidth || this.PortalForward.magnitude < 0.5f)
                {
                    this.PortalForward = cameraToStart.normalized;
                    this.PortalLeft = Vector3.Cross(this.PortalForward, Vector3.up) * (this.PortalWidth * this.OpenAmount);
                }
            }

            var cameraToLeft = (start + this.PortalLeft - cameraPos).normalized;
            var cameraToRight = (start - this.PortalLeft - cameraPos).normalized;

            var pos0 = this.PortalLeft;
            var pos1 = -this.PortalLeft;

            var pos7 = pos0 * 0.8f;
            var pos8 = pos1 * 0.8f;

            var pos2 = pos0 + cameraToLeft * this.PortalLength;
            var pos6 = pos1 + cameraToRight * this.PortalLength;

            var between = pos2 - pos6;
            var shortBetween = between * 0.075f;

            var pos3 = pos2 - shortBetween;
            var pos5 = pos6 + shortBetween;

            var pos4 = this.PortalForward * this.PortalLength;

            this.vertices[0] = pos0;
            this.vertices[1] = pos1;
            this.vertices[2] = pos2;
            this.vertices[3] = pos3;
            this.vertices[4] = pos4;
            this.vertices[5] = pos5;
            this.vertices[6] = pos6;
            this.vertices[7] = pos7;
            this.vertices[8] = pos8;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float PortalShadowLerp(float input)
        {
            return Easing.Cubic.Out(input);
        }
        #endregion
    }
}