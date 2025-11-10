using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using UnityEngine.Rendering;

#nullable enable

namespace Orbits
{
    public class PortalManager : MonoBehaviour
    {
        public delegate void PortalChangeHandler(PortalManager manager, Portal? oldPortal1, Portal? oldPortal2);
        public delegate void MovedThroughPortalHandler(PortalableTarget target, Portal atPortal, Vector3 moveDiff);

        #region Fields
        public static PortalManager Instance;

        public Camera MainCamera;
        public Camera PortalMaskCamera;
        public Camera PortalCamera;
        public Portal GraphicPrefab;
        public Portal? Portal1;
        public Portal? Portal2;
        public PortalShadow Shadow;
        public GameObject UIMask;
        public List<Transform> NamedPortals;

        public Portal? PortalClosestOnScreen { get; private set; } = null;
        public Portal? PortalOtherOnScreen { get; private set; } = null;
        public Portal? PortalClosest { get; private set; } = null;
        public Portal? PortalOther { get; private set; } = null;
        private readonly Dictionary<string, Transform> namedPortalMap = new();

        public event PortalChangeHandler? OnChange;
        public event MovedThroughPortalHandler? OnMovedThroughPortal;
        public event Action? OnPortalsClosed;

        public int NumPortals => this.Portal1 != null ? 2 : 0;
        #endregion

        #region Unity Methods
        private void Awake()
        {
            Instance = this;

            RenderPipelineManager.beginCameraRendering += this.OnBeginCameraRendering;
            RenderPipelineManager.endCameraRendering += this.OnEndCameraRendering;

            foreach (var namedPortal in this.NamedPortals)
            {
                this.namedPortalMap[namedPortal.name] = namedPortal;
            }
        }

        private void OnEndCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (camera == this.PortalCamera)
            {
                if (this.PortalClosestOnScreen != null)
                {
                    foreach (var nearby in this.PortalClosestOnScreen.NearbyTargets)
                    {
                        nearby.Target.RevertPosition();
                    }
                }
            }
        }

        private void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (camera == this.PortalCamera)
            {
                if (this.PortalClosestOnScreen != null && this.PortalOtherOnScreen != null)
                {
                    var closestPos = this.PortalClosestOnScreen.transform.position;
                    var otherPos = this.PortalOtherOnScreen.transform.position;
                    var delta = closestPos - otherPos;
                    foreach (var nearby in this.PortalClosestOnScreen.NearbyTargets)
                    {
                        nearby.Target.MoveBy(delta, otherPos, this.PortalOtherOnScreen.OpenSize);
                    }
                }
            }
        }

        private void Update()
        {
            this.UpdatePortals();
        }
        #endregion

        #region Methods
        public bool IsVisible(Vector3 worldPosition)
        {
            var viewportPoint = this.MainCamera.WorldToViewportPoint(worldPosition, Camera.MonoOrStereoscopicEye.Mono);
            if (viewportPoint.x > -0.05f && viewportPoint.x < 1.05f &&
                viewportPoint.y > -0.05f && viewportPoint.y < 1.05f)
            {
                return true;
            }

            if (this.PortalCamera.gameObject.activeSelf)
            {
                viewportPoint = this.PortalCamera.WorldToViewportPoint(worldPosition, Camera.MonoOrStereoscopicEye.Mono);
                if (viewportPoint.x > -0.05f && viewportPoint.x < 1.05f &&
                    viewportPoint.y > -0.05f && viewportPoint.y < 1.05f)
                {
                    return true;
                }
            }

            return false;
        }

        private void DisableEffect()
        {
            if (this.UIMask.activeSelf)
            {
                Debug.Log($"Disable portal effect");
                this.UIMask.SetActive(false);
                this.PortalCamera.gameObject.SetActive(false);
                this.PortalMaskCamera.gameObject.SetActive(false);
                this.Shadow.gameObject.SetActive(false);
            }
        }

        private void EnableEffect()
        {
            if (!this.UIMask.activeSelf)
            {
                Debug.Log($"Enabled portal effect");
                this.UIMask.SetActive(true);
                this.PortalCamera.gameObject.SetActive(true);
                this.PortalMaskCamera.gameObject.SetActive(true);
                this.Shadow.gameObject.SetActive(true);
            }
        }

        public void UpdatePortals()
        {
            if (this.Portal1 == null || this.Portal2 == null)
            {
                this.DisableEffect();
                return;
            }

            ProjectileManager.Instance.UpdatePortals();

            var mainCameraPos = this.MainCamera.transform.position;
            var cameraPos = Utils.TakeXZ(mainCameraPos);
            var toPortal1 = this.Portal1.transform.position - cameraPos;
            var toPortal2 = this.Portal2.transform.position - cameraPos;

            var toPortal = toPortal1;
            this.PortalClosest = this.Portal1;
            this.PortalOther = this.Portal2;

            if (toPortal2.magnitude < toPortal1.magnitude)
            {
                toPortal = toPortal2;
                this.PortalClosest = this.Portal2;
                this.PortalOther = this.Portal1;
            }

            this.PortalClosestOnScreen = this.PortalClosest;
            this.PortalOtherOnScreen = this.PortalOther;

            var screenPos = this.MainCamera.WorldToViewportPoint(this.PortalClosest.transform.position);
            if (screenPos.x < -0.1f || screenPos.x > 1.1f || screenPos.y < -0.1 || screenPos.y > 1.1f)
            {
                this.PortalClosestOnScreen = null;
                this.PortalOtherOnScreen = null;
                this.DisableEffect();
                return;
            }

            this.EnableEffect();
            this.Shadow.InitRenderTexture();

            this.PortalClosestOnScreen.PortalShadow = this.Shadow;
            this.PortalOtherOnScreen.PortalShadow = null;

            var prevPosition = this.Shadow.transform.position;
            this.Shadow.transform.position = this.PortalClosestOnScreen.transform.position;
            var afterPosition = this.Shadow.transform.position;

            var diff = Vector3.Distance(prevPosition, afterPosition);
            if (diff > 1)
            {
                Debug.Log($"Moved shadow position {prevPosition} -> {afterPosition}");
            }

            var portalCameraPos = this.PortalOtherOnScreen.transform.position - toPortal;
            this.PortalCamera.transform.position = new Vector3(portalCameraPos.x, mainCameraPos.y, portalCameraPos.z);
        }

        public bool TryGetPortalPosition(string name, [NotNullWhen(true)] out Transform? result)
        {
            if (this.namedPortalMap.TryGetValue(name, out var found))
            {
                result = found.transform;
                return true;
            }

            result = null;
            return false;
        }

        public void MoveTarget(PortalableTarget target, Portal atPortal)
        {
            var targetPos = target.transform.position;
            var pos = atPortal.ConnectedTo.transform.position;
            var offPortalDiff = atPortal.transform.position - targetPos;
            var newPos = pos - offPortalDiff;
            var diff = newPos - targetPos;
            target.Rigidbody.position = newPos;
            target.transform.position = newPos;
            target.InsidePortal = 2;
            Debug.Log($"Moving target {target.name} to {newPos}");

            this.OnMovedThroughPortal?.Invoke(target, atPortal, diff);
        }

        public void ShowPortal(IWorldTarget portal1Position, IWorldTarget portal2Position)
        {
            var oldPortal1 = this.Portal1;
            var oldPortal2 = this.Portal2;

            this.DoClosePortals();

            this.Portal1 = this.Spawn(portal1Position.WorldPosition);
            this.Portal1.PortalWidth = this.Shadow.PortalWidth;
            this.Portal1.name = "Portal1";
            this.Portal1.Show = true;
            this.Portal1.TargetPosition = portal1Position;

            this.Portal2 = this.Spawn(portal2Position.WorldPosition);
            this.Portal2.PortalWidth = this.Shadow.PortalWidth;
            this.Portal2.name = "Portal2";
            this.Portal2.Show = true;
            this.Portal2.TargetPosition = portal2Position;

            this.Portal1.ConnectedTo = this.Portal2;
            this.Portal2.ConnectedTo = this.Portal1;

            this.Shadow.Show = true;

            this.OnChange?.Invoke(this, oldPortal1, oldPortal2);
        }

        public void ClosePortals()
        {
            var oldPortal1 = this.Portal1;
            var oldPortal2 = this.Portal2;

            this.DoClosePortals();

            this.OnChange?.Invoke(this, oldPortal1, oldPortal2);
        }

        public void ReleasePortal(Portal portal)
        {
            if (portal == this.Portal1)
            {
                this.Portal1 = null;
            }
            if (portal == this.Portal2)
            {
                this.Portal2 = null;
            }

            this.OnPortalsClosed?.Invoke();
        }

        private void DoClosePortals()
        {
            if (this.Portal1 != null)
            {
                this.Portal1.Show = false;
            }
            if (this.Portal2 != null)
            {
                this.Portal2.Show = false;
            }

            this.Shadow.Show = false;
        }

        private Portal Spawn(Vector3 position)
        {
            var spawned = GameObjectPools.Instance.Spawn(this.GraphicPrefab);
            spawned.transform.position = position;
            spawned.Init(this.GraphicPrefab.gameObject);
            return spawned;
        }
        #endregion
    }
}