using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using PixelPlay.OffScreenIndicator;

#nullable enable

namespace Orbits
{
    /// <summary>
    /// Attach the script to the off screen indicator panel.
    /// </summary>
    [DefaultExecutionOrder(-1)]
    public class OffScreenTargetIndicator : MonoBehaviour
    {
        public static OffScreenTargetIndicator Instance;
        [Range(0.5f, 0.9f)]
        [Tooltip("Distance offset of the indicators from the centre of the screen")]
        [SerializeField] private float screenBoundOffset = 0.9f;

        public Indicator? ArrowPrefab;
        public Indicator? BigArrowPrefab;
        public Indicator? PortalPrefab;

        private Camera mainCamera;
        private Vector3 screenCentre;
        private Vector3 screenBounds;

        private readonly Dictionary<Target, Indicator> targetIndicators = new();
        private Indicator? portalIndicator;

        void Awake()
        {
            Instance = this;
        }

        void Start()
        {
            mainCamera = Camera.main;
            ProjectileManager.Instance.OnTargetRegistered += this.OnTargetRegistered;

            this.portalIndicator = Instantiate(this.PortalPrefab);
            this.portalIndicator.transform.SetParent(this.transform);
        }

        void LateUpdate()
        {
            this.screenCentre = new Vector3(Screen.width, Screen.height, 0) / 2;
            this.screenBounds = this.screenCentre * this.screenBoundOffset;
            DrawIndicators();
        }

        private void OnTargetRegistered(Target target, bool removed)
        {
            if (GameManager.Instance.CurrentLevelContainer == null ||
                !GameManager.Instance.CurrentLevelContainer.ShowTargetIndicators)
            {
                return;
            }

            if (target.Team != Target.EnemyTeam)
            {
                return;
            }

            var prefab = this.ArrowPrefab;
            if (target.TryGetComponent<AlienShip>(out _))
            {
                prefab = this.BigArrowPrefab;
            }

            if (prefab == null)
            {
                Debug.LogWarning($"Unset arrow indicator prefab");
                return;
            }

            if (removed)
            {
                if (this.targetIndicators.TryGetValue(target, out var indicator))
                {
                    GameObjectPools.Instance.Release(indicator.Prefab, indicator.gameObject);
                }
                this.targetIndicators.Remove(target);
            }
            else
            {
                var spawned = GameObjectPools.Instance.Spawn(prefab);
                spawned.transform.SetParent(this.transform);
                spawned.Prefab = prefab.gameObject;
                this.targetIndicators[target] = spawned;
            }
        }

        public void Clear()
        {
            if (this.ArrowPrefab == null)
            {
                return;
            }

            foreach (var indicator in this.targetIndicators.Values)
            {
                GameObjectPools.Instance.Release(indicator.Prefab, indicator.gameObject);
            }

            this.targetIndicators.Clear();
        }

        /// <summary>
        /// Draw the indicators on the screen and set their position and rotation and other properties.
        /// </summary>
        private void DrawIndicators()
        {
            foreach (var kvp in this.targetIndicators)
            {
                var target = kvp.Key;
                var indicator = kvp.Value;

                this.PointIndicatorAtTransform(indicator, target.transform.position);
            }

            if (this.portalIndicator != null)
            {
                if (PortalManager.Instance.PortalClosest != null && PortalManager.Instance.PortalClosest.OpenAmount > 0.1f)
                {
                    this.portalIndicator.Activate(true);
                    this.PointIndicatorAtTransform(this.portalIndicator, PortalManager.Instance.PortalClosest.transform.position);
                }
                else
                {
                    this.portalIndicator.Activate(false);
                }
            }
        }

        private void PointIndicatorAtTransform(Indicator indicator, Vector3 position)
        {
            var screenPosition = OffScreenIndicatorCore.GetScreenPosition(mainCamera, position);
            var isTargetVisible = OffScreenIndicatorCore.IsTargetVisible(screenPosition);

            if (!isTargetVisible)
            {
                indicator.Activate(true);

                var angle = float.MinValue;
                OffScreenIndicatorCore.GetArrowIndicatorPositionAndAngle(ref screenPosition, ref angle, screenCentre, screenBounds);
                indicator.transform.rotation = Quaternion.Euler(0, 0, angle * Mathf.Rad2Deg); // Sets the rotation for the arrow indicator.
                indicator.transform.position = screenPosition; //Sets the position of the indicator on the screen.
            }
            else
            {
                indicator.Activate(false);
            }
        }
    }
}