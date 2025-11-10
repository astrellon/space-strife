using Unity.Collections;
using UnityEngine;

#nullable enable

namespace Orbits
{
    [ExecuteInEditMode]
    public class StarBackground : MonoBehaviour
    {
        #region Fields
        public Material Material;
        public Mesh Mesh;
        public float RandomOffset;
        public int NumInstances;
        public Vector4 OffsetScale = Vector4.one;

        private RenderParams renderParams;
        private MaterialPropertyBlock? materialPropertyBlock;
        #endregion

        #region Unity Methods
        private void Start()
        {
            this.renderParams = new(this.Material)
            {
                matProps = this.materialPropertyBlock = new MaterialPropertyBlock(),
                worldBounds = new Bounds(Vector3.zero, Vector3.one * 1000)
            };

            this.UpdateMaterial(this.materialPropertyBlock);
        }

        private void Update()
        {
            if (this.materialPropertyBlock == null)
            {
                return;
            }

            #if UNITY_EDITOR
            this.UpdateMaterial(this.materialPropertyBlock);
            #endif
            Graphics.RenderMeshPrimitives(in this.renderParams, this.Mesh, 0, this.NumInstances);
        }
        #endregion

        #region Methods
        private void UpdateMaterial(MaterialPropertyBlock block)
        {
            block.SetMatrix("_Matrix", this.transform.localToWorldMatrix);
            block.SetVector("_ScaleOffset", this.OffsetScale);
            block.SetFloat("_NumInstances", this.NumInstances);
            block.SetFloat("_RandomOffset", this.RandomOffset);
        }
        #endregion
    }
}