using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Orbits.Extensions;
using UnityEngine;

#nullable enable

namespace Orbits
{
    public class StarSystem : MonoBehaviour
    {
        public enum ForceShowType
        {
            Unset, Show, Hide
        }
        #region Fields
        public string StarSystemId;
        public string StarSystemName;
        public SelectableGameObject UIPoint;
        public List<AnimatePlanet> MainStars = new();
        public List<LevelContainer> Levels = new();
        public List<AnimatePlanet> PrimaryPlanets = new();
        public List<AnimatePlanet> SecondaryPlanets = new();
        public AnimatePlanet.ShowType DefaultShowStar = AnimatePlanet.ShowType.Secondary;

        public IReadOnlyList<GravitySource> AllGravitySources = Array.Empty<GravitySource>();

        private readonly Dictionary<LevelId, LevelContainer> levelMap = new();
        private GravitySource[] mainStarGravitySources = Array.Empty<GravitySource>();
        public IReadOnlyList<GravitySource> MainStarGravitySources => this.mainStarGravitySources;
        public int firstFrameCounter = 2;
        public StarSystemId Id => new(this.StarSystemId);
        public ForceShowType ForceShown = ForceShowType.Unset;
        public LevelBoundary Boundary;

        private AnimatePlanet.ShowType prevShowPrimary;
        private AnimatePlanet.ShowType prevShowSecondary;
        private Vector3 startingPosition;
        private bool hasOffset;
        #endregion

        #region Unity Methods
        private void Awake()
        {
            this.startingPosition = this.transform.position;
            this.mainStarGravitySources = this.MainStars.SelectMany(s => s.GetComponentsInChildren<GravitySource>()).ToArray();
            this.AllGravitySources = this.transform.GetComponentsInChildren<GravitySource>();

            foreach (var level in this.Levels)
            {
                if (level.LevelPrefab != null)
                {
                    this.levelMap[new LevelId(level.LevelPrefab.LevelId)] = level;
                }

                this.PrimaryPlanets.AddDistinctRange(level.PrimaryPlanets);
                this.SecondaryPlanets.AddDistinctRange(level.SecondaryPlanets);
            }
        }

        private void Start()
        {
            foreach (var level in this.Levels)
            {
                level.Init(this);
            }
        }

        private void Update()
        {
            if (GameManager.Instance.CurrentLevel != null)
            {
                return;
            }

            if (this.firstFrameCounter == 2)
            {
                foreach (var planet in this.AllPlanets())
                {
                    planet.SetShow(AnimatePlanet.ShowType.Regular);
                }
                this.UpdateShipVisibility(AnimatePlanet.ShowType.Regular);
            }

            this.UpdateVisibility();
            if (this.ForceShown == ForceShowType.Unset)
            {
                this.UpdateShipVisibility(this.prevShowPrimary);
            }

            if (this.firstFrameCounter == 1)
            {
                foreach (var planet in this.AllPlanets())
                {
                    planet.Finish();
                }
            }

            if (this.firstFrameCounter > 0)
            {
                this.firstFrameCounter--;
            }
        }
        #endregion

        #region Methods
        public void Reset()
        {
            this.ForceShowStarSystem(ForceShowType.Unset);
            this.SetEnableBoundary(false);

            if (this.hasOffset)
            {
                this.transform.position = this.startingPosition;
            }
        }

        public void OffsetPosition(Vector3 offset)
        {
            this.transform.position = this.startingPosition + offset;
            this.hasOffset = offset.magnitude > Mathf.Epsilon;
        }

        public bool ForceShowStarSystem(ForceShowType forceShow)
        {
            this.ForceShown = forceShow;

            if (forceShow == ForceShowType.Show)
            {
                foreach (var planet in this.PrimaryPlanets)
                {
                    planet.SetShow(AnimatePlanet.ShowType.Regular, atTime: 1.0f);
                }

                foreach (var planet in this.SecondaryPlanets)
                {
                    planet.SetShow(AnimatePlanet.ShowType.Regular, atTime: 1.0f);
                }

                foreach (var star in this.MainStars)
                {
                    star.SetShow(AnimatePlanet.ShowType.Regular, atTime: 1.0f);
                }

                this.SetEnableBoundary(true);
            }
            else if (forceShow == ForceShowType.Hide)
            {
                this.UpdateVisibility();
                this.SetEnableBoundary(false);
            }

            return true;
        }

        private void UpdateVisibility()
        {
            var uiState = UIManager.Instance.State;
            var uiStarSystem = UIManager.Instance.CurrentStarSystem;

            var showPrimary = AnimatePlanet.ShowType.Hide;
            var showSecondary = AnimatePlanet.ShowType.Hide;
            var showStar = AnimatePlanet.ShowType.Hide;

            var otherSelected = uiStarSystem != null && uiStarSystem != this;
            if (!otherSelected && this.ForceShown != ForceShowType.Hide)
            {
                showStar = this.DefaultShowStar;
                var starSystemUnlocked = PlayerState.Instance.IsStarSystemUnlocked(this.Id);

                if (starSystemUnlocked)
                {
                    showStar = AnimatePlanet.ShowType.Primary;
                    var regularSize = uiState == InterfaceState.SelectedLevel;
                    var starSystemSelected = uiStarSystem == this &&
                        uiState == InterfaceState.LevelSelect || regularSize;

                    if (starSystemSelected)
                    {
                        if (regularSize)
                        {
                            showPrimary = AnimatePlanet.ShowType.Regular;
                            showSecondary = AnimatePlanet.ShowType.Regular;
                            showStar = AnimatePlanet.ShowType.Regular;
                        }
                        else
                        {
                            showPrimary = AnimatePlanet.ShowType.Primary;
                            showSecondary = AnimatePlanet.ShowType.Secondary;
                        }
                    }
                }
            }

            foreach (var planet in this.PrimaryPlanets)
            {
                planet.SetShow(showPrimary);
            }

            foreach (var planet in this.SecondaryPlanets)
            {
                planet.SetShow(showSecondary);
            }

            foreach (var star in this.MainStars)
            {
                star.SetShow(showStar);
            }

            this.prevShowPrimary = showPrimary;
            this.prevShowSecondary = showSecondary;
        }

        private void UpdateShipVisibility(AnimatePlanet.ShowType showType)
        {
            foreach (var level in this.Levels)
            {
                if (level.Ship != null)
                {
                    level.Ship.AnimatePlanet.SetShow(showType);
                }
            }
        }

        public bool TryGetLevel(LevelId levelId, [NotNullWhen(true)] out LevelContainer? result)
        {
            return this.levelMap.TryGetValue(levelId, out result);
        }

        private IEnumerable<AnimatePlanet> AllPlanets()
        {
            return this.MainStars.Concat(this.PrimaryPlanets).Concat(this.SecondaryPlanets);
        }

        public void SetEnableBoundary(bool enable)
        {
            if (this.Boundary != null)
            {
                this.Boundary.gameObject.SetActive(enable);
            }
        }
        #endregion
    }
}