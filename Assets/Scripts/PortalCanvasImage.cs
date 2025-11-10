using System;
using UnityEngine;
using UnityEngine.UI;

#nullable enable

namespace Orbits
{
    public class PortalCanvasImage : MonoBehaviour
    {
        #region Fields
        public PortalShadow Shadow;
        public RawImage Image;
        #endregion

        #region Unity Methods
        private void Start()
        {
            if (this.Image.texture != this.Shadow.RenderTexture)
            {
                this.Image.texture = this.Shadow.RenderTexture;
                this.Image.material.SetTexture("_ShadowMask", this.Shadow.RenderTextureMask);
            }
            this.Shadow.OnRenderTextureChange += this.OnRenderTextureChange;
        }

        private void OnDestroy()
        {
            this.Shadow.OnRenderTextureChange -= this.OnRenderTextureChange;
        }
        #endregion

        #region Methods
        private void OnRenderTextureChange(PortalShadow source)
        {
            this.Image.texture = source.RenderTexture;
            this.Image.material.SetTexture("_ShadowMask", source.RenderTextureMask);
        }
        #endregion
    }
}