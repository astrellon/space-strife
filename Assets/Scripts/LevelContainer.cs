using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Orbits.Extensions;
using Unity.AI.Navigation;
using UnityEngine;

#nullable enable

namespace Orbits
{
    [Serializable]
    public class StartLevelScriptPair
    {
        public string VarName;
        public MonoBehaviour Target;
    }

    [Serializable]
    public class NamedSubLevels
    {
        public string VarName;
        public Transform ParentLocation;
        public GameObject Prefab;
    }

    public class LevelContainer : MonoBehaviour
    {
        #region Fields
        public List<AnimatePlanet> PrimaryPlanets = new();
        public List<AnimatePlanet> SecondaryPlanets = new();
        public IReadOnlyList<GravitySource> GravitySourcesForLevel = Array.Empty<GravitySource>();
        public List<Target> StartingLevelTargets = new();
        public Level? LevelPrefab;
        public SelectableGameObject? Target;
        public GameObject? FocusOn;
        public GameObject? FocusOnPreLevel;
        public Transform? LevelHolderTarget;
        public List<Transform> NamedTargets = new();
        public bool NeedsBoundary;

        public TextAsset? ScriptText;
        public LysitheaVM.Script? Script;
        public TankShip? Ship;
        public List<GameObject> ToResetOnStopped = new();
        public List<StartLevelScriptPair> StartLevelScriptPairs = new();
        public bool ShipFocusedLevel;
        public bool ShowTargetIndicators = false;

        public List<GameObject> HideIfNotAvailable = new();

        private readonly List<Target> levelTargets = new();
        private List<ILevelStart> toStartOnLevel = new();

        public NavMeshSurface? NavMeshSurface;

        public List<NamedSubLevels> NamedSubLevels = new();
        private Dictionary<string, GameObject> createdSubLevels = new();
        #endregion

        #region Methods
        public void Init(StarSystem starSystem)
        {
            var gravitySources = new List<GravitySource>();
            var allPlanets = this.PrimaryPlanets.Concat(this.SecondaryPlanets);
            foreach (var planet in allPlanets)
            {
                gravitySources.AddDistinctRange(planet.GetComponentsInChildren<GravitySource>());
            }

            if (this.Ship != null)
            {
                this.GravitySourcesForLevel = starSystem.AllGravitySources;
            }
            else
            {
                this.GravitySourcesForLevel = starSystem.MainStarGravitySources.Concat(gravitySources).ToList();
            }

            if (!this.levelTargets.Any())
            {
                var allTargets = gravitySources.Select(s => s.gameObject)
                    .Concat(this.StartingLevelTargets.Select(s => s.gameObject))
                    .Distinct();

                foreach (var planet in allTargets)
                {
                    this.levelTargets.AddRange(planet.GetComponentsInChildren<Target>());
                }
            }

            this.toStartOnLevel = this.levelTargets.SelectMany(s => s.GetComponents<ILevelStart>()).ToList();

            if (this.ScriptText != null)
            {
                Debug.Log($"Compiling script: {this.ScriptText.name}: {this.ScriptText.text.Length}");
                this.Script = LevelVM.Instance.AssembleScript(this.ScriptText.name, this.ScriptText.text);
            }
        }

        public bool StartSubLevel(string subLevelName, Level currentLevel)
        {
            foreach (var subLevel in this.NamedSubLevels)
            {
                if (subLevel.VarName == subLevelName)
                {
                    var created = Instantiate(subLevel.Prefab, subLevel.ParentLocation, false);
                    this.createdSubLevels[subLevelName] = created;
                    currentLevel.AddSubLevel(created);

                    return true;
                }
            }

            return false;
        }

        public bool RemoveSubLevel(string subLevelName, Level currentLevel)
        {
            if (this.createdSubLevels.TryGetValue(subLevelName, out var subLevel))
            {
                currentLevel.RemoveSubLevel(subLevel);
                this.createdSubLevels.Remove(subLevelName);
                return true;
            }

            return false;
        }

        public bool TryGetNamedTransform(string name, [NotNullWhen(true)] out Transform? result)
        {
            foreach (var target in this.NamedTargets)
            {
                if (target.name == name)
                {
                    result = target;
                    return true;
                }
            }

            result = null;
            return false;
        }

        public string GetDescription()
        {
            LevelVM.Instance.StartScript(this.Script);
            var vmRunner = LevelVM.Instance.VMRunner;
            if (vmRunner.TryGetFunction("levelDescription", out var levelDescription))
            {
                vmRunner.VM.CallFunctionToReturn(levelDescription, 0);
                if (vmRunner.VM.Stack.TryPeek(out var top))
                {
                    return top.ToString();
                }
            }
            return "";
        }

        public void OnLevelStarted(Level levelInstance)
        {
            if (this.Ship != null)
            {
                this.Ship.Unlock();
            }

            if (this.NavMeshSurface != null)
            {
                this.NavMeshSurface.gameObject.SetActive(true);
            }

            foreach (var target in this.levelTargets)
            {
                target.Reset();
                target.UpdateId();

                Debug.Log($"Registering level target: {target.name}");
                if (!ProjectileManager.Instance.Targets.Contains(target))
                {
                    ProjectileManager.Instance.RegisterTarget(target);
                }
            }

            foreach (var toStart in this.toStartOnLevel)
            {
                toStart.LevelStart(this);
            }

            if (this.Target != null)
            {
                foreach (var rotator in this.Target.GetComponentsInChildren<Rotator>())
                {
                    rotator.enabled = false;
                }
            }
        }

        public void OnLevelStopped(Level levelInstance)
        {
            if (this.Ship != null)
            {
                this.Ship.Reset();
            }

            if (this.NavMeshSurface != null)
            {
                this.NavMeshSurface.gameObject.SetActive(false);
            }

            foreach (var created in this.createdSubLevels)
            {
                Destroy(created.Value);
            }
            this.createdSubLevels.Clear();

            foreach (var toReset in this.ToResetOnStopped)
            {
                var resets = toReset.GetComponents<IReset>();
                resets.ForEach(ResetObject);
            }

            if (this.Target != null)
            {
                foreach (var rotator in this.Target.GetComponentsInChildren<Rotator>())
                {
                    rotator.enabled = true;
                }
            }
        }

        private static void ResetObject(IReset r)
        {
            r.Reset();
        }
        #endregion
    }
}