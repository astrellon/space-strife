using System;
using UnityEngine;

#nullable enable

namespace Orbits
{
    public class PortalMaskCamera : MonoBehaviour
    {
        #region Fields
        public PortalShadow Shadow;
        public Camera Camera;
        #endregion

        #region Unity Methods
        private void Start()
        {
            if (this.Camera.targetTexture != this.Shadow.RenderTextureMask)
            {
                this.Camera.targetTexture = this.Shadow.RenderTextureMask;
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
            this.Camera.targetTexture = source.RenderTextureMask;
        }
        #endregion
    }
}