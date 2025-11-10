using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using LysitheaVM;
using UnityEngine;

#nullable enable

namespace Orbits
{
    public class PortalableTarget : MonoBehaviour, IObjectValue
    {
        #region Fields
        public static readonly IReadOnlyList<string> Keys = new [] { "isPlayer", "name" };

        public Transform Graphic;
        public Rigidbody Rigidbody;
        public float Radius;
        public bool IsPlayer;
        // public int IgnorePortalCount = 0;
        public int InsidePortal = 0;
        public List<Renderer> MirrorRenderers = new();
        private Vector3 originalPosition;
        private bool moved = false;

        public IReadOnlyList<string> ObjectKeys => Keys;
        public string TypeName => "portalableTarget";
        #endregion

        #region Methods
        public void MoveBy(Vector3 delta, Vector3 portalWorldPos, float portalSize)
        {
            if (!this.moved)
            {
                this.moved = true;
                this.originalPosition = this.Graphic.position;
                this.Graphic.position -= delta;

                foreach (var renderer in this.MirrorRenderers)
                {
                    renderer.material.SetVector("_PortalOffset", delta);
                }

                if (!this.IsPlayer)
                {
                    foreach (var renderer in this.MirrorRenderers)
                    {
                        renderer.material.SetVector("_PortalCutout", portalWorldPos);
                        renderer.material.SetFloat("_PortalSize", portalSize);
                    }
                }
            }
        }

        public void RevertPosition()
        {
            if (this.moved)
            {
                this.Graphic.position = this.originalPosition;
                this.moved = false;

                foreach (var renderer in this.MirrorRenderers)
                {
                    renderer.material.SetVector("_PortalOffset", Vector3.zero);

                    if (!this.IsPlayer)
                    {
                        renderer.material.SetFloat("_PortalSize", 0.0f);
                    }
                }
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(this.transform.position, this.Radius);
        }
        #endregion

        #region IObjectValue Methods
        public override string ToString()
        {
            return StandardObjectLibrary.GeneralToString(this, false);
        }

        public string ToStringSerialise()
        {
            return StandardObjectLibrary.GeneralToString(this, true);
        }

        public bool TryGetKey(string key, [NotNullWhen(true)] out IValue? value)
        {
            if (key == "isPlayer")
            {
                value = new BoolValue(this.IsPlayer);
                return true;
            }
            if (key == "name")
            {
                value = new StringValue(this.name);
                return true;
            }

            value = null;
            return false;
        }

        public int CompareTo(IValue other)
        {
            if (other is PortalableTarget otherTarget)
            {
                return otherTarget == this ? 0 : 1;
            }
            return -1;
        }
        #endregion
    }
}