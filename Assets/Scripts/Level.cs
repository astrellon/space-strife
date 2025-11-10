using System;
using System.Collections.Generic;
using System.Linq;
using LysitheaVM;
using Orbits.Extensions;
using UnityEngine;

#nullable enable

namespace Orbits
{
    public enum LevelState
    {
        Idle, PreLevel, Running, PostLevel
    }

    public class Level : MonoBehaviour
    {
        #region Fields
        public LevelState State = LevelState.Idle;
        public List<Wave> Waves;
        public string LevelName;
        public string LevelId;
        public string BGMAudioId;
        public int TotalShips;
        public int ShipsLeft;
        public int Money;
        public int MoneyTotal;
        public int NumCharacters = 1;
        public int PlanetCurrentHealth = 20;
        public int PlanetMaxHealth = 20;
        public float TimeControlVisualDistance = 10.0f;
        public StarSystem? PartOfStarSystem;
        public PlayerTankController Player;
        public float ZoomForEquipment = 10.0f;
        public LevelContainer Container;

        public List<string> FirstTimeCharacterIds = new();

        public WeaponType StartingTank = WeaponType.Unknown;

        public List<LevelUnlock> LevelUnlocks = new();
        public List<LevelUnlock> LevelFullHealthUnlocks = new();
        public List<Ability> ActiveAbilities = new();

        public bool IsAtMaxHealth => this.PlanetCurrentHealth >= this.PlanetMaxHealth;

        // public IEnumerable<UnlockState> LevelUnlocksBasedOnHealth
        // {
        //     get
        //     {
        //         foreach (var unlock in this.LevelUnlocks)
        //         {
        //             yield return new UnlockState(unlock, IsMaxHealthUnlock: false);
        //         }

        //         if (this.PlanetCurrentHealth >= this.PlanetMaxHealth)
        //         {
        //             foreach (var unlock in this.LevelUnlocks)
        //             {
        //                 yield return new UnlockState(unlock, IsMaxHealthUnlock: true);
        //             }
        //         }
        //     }
        // }

        public event Action? OnStarted;
        public event Action? OnReady;
        public event Action? OnStartWaves;
        private bool ready = false;
        public List<Behaviour> EnableOnStartWaves = new();

        private LysitheaVM.Script? script;
        private bool isFirstInit = false;

        public int WeaponLevelsBoosted = 0;
        public LevelId Id => new(this.LevelId);
        public bool NeedUpdateGravitySources = false;
        public bool ScriptControlsLevelEnd = false;
        #endregion

        #region Unity Methods
        private void Start()
        {
            if (this.Waves.Count == 0)
            {
                this.Waves = this.GetComponentsInChildren<Wave>().ToList();

                var startingEnabled = this.Waves.Where(w => w.enabled).ToList();
                this.EnableOnStartWaves.AddDistinctRange(startingEnabled);
                startingEnabled.ForEach(w => w.enabled = false);
            }

            this.ready = true;
            this.OnReady?.Invoke();

            if (this.script != null)
            {
                this.State = LevelState.PreLevel;
                Debug.Log("Starting level script");
                LevelVM.Instance.StartScript(this.script, (vm) =>
                {
                    foreach (var pair in this.Container.StartLevelScriptPairs)
                    {
                        if (pair.Target is IValue value)
                        {
                            Debug.Log($"Defining level start var: {pair.VarName}");
                            vm.GlobalScope.TryDefine(pair.VarName, value);
                        }
                    }
                });

                LevelVM.Instance.RunRegisteredFunction("start", new BoolValue(this.isFirstInit));
            }
        }

        public void AddSubLevel(GameObject gameObject)
        {
            var childWaves = gameObject.GetComponentsInChildren<Wave>();
            this.Waves.AddRange(childWaves);
        }

        public void RemoveSubLevel(GameObject gameObject)
        {
            var childWaves = gameObject.GetComponentsInChildren<Wave>();
            this.Waves.RemoveAll(w => childWaves.Contains(w));
        }

        private void Update()
        {
            this.TotalShips = this.Waves.Sum(w => w.NumShips);
            this.ShipsLeft = this.Waves.Sum(w => w.NumShipsLeft);
            if (!this.ScriptControlsLevelEnd && this.ShipsLeft == 0)
            {
                GameManager.Instance.LevelEnd(this);
                this.enabled = false;
            }

            var abilityDt = Time.deltaTime * GameManager.Instance.PlayerDeltaTimeScale;
            foreach (var ability in this.ActiveAbilities)
            {
                ability.Update(abilityDt, this);
            }

            if (!UIManager.Instance.InDialogue && Time.deltaTime > 0.0f)
            {
                LevelVM.Instance.RunUpdateFunction();
            }

            if (!GameManager.Instance.GamePaused && this.NeedUpdateGravitySources)
            {
                ProjectileManager.Instance.UpdateGravitySourceLocations();
            }
        }

        private void OnDisable()
        {
            this.State = LevelState.PostLevel;
            foreach (var ability in this.ActiveAbilities)
            {
                ability.Update(0, this);
            }
        }
        #endregion

        #region Methods
        public bool TryGetStartingCharacters(out List<GameCharacter> result)
        {
            result = new List<GameCharacter>();
            foreach (var c in this.FirstTimeCharacterIds)
            {
                var charId = new GameCharacterId(c);
                if (GameManager.Instance.TryGetCharacter(charId, out var gameChar))
                {
                    result.Add(gameChar);
                }
                else
                {
                    Debug.LogError($"Failing to find starting character: {c}");
                    return false;
                }
            }

            return result.Count > 0;
        }

        public void InitLevel(StarSystem starSystem, GameObject planet, List<Ability> abilities, LysitheaVM.Script? script, bool isFirstInit)
        {
            this.PartOfStarSystem = starSystem;
            this.TotalShips = this.Waves.Sum(w => w.NumShips);
            this.ShipsLeft = this.TotalShips;
            this.Player.Planet = planet;
            if (this.Player.TankContainer == null && planet.TryGetComponent<ITankContainer>(out var tankContainer))
            {
                this.Player.TankContainer = tankContainer;
            }

            if (this.Player.TankContainer != null)
            {
                this.Player.TankContainer.InitPlanet(planet);
            }
            this.ActiveAbilities = abilities;
            this.script = script;
            this.isFirstInit = isFirstInit;
        }

        public void StartLevel()
        {
            if (this.ready)
            {
                if (this.StartingTank != WeaponType.Unknown)
                {
                    this.BuyTank(this.StartingTank, 0, checkMoney: false);
                }

                this.OnStarted?.Invoke();
            }
            else
            {
                Debug.LogWarning($"Cannot start level that is not ready: {this.LevelName}");
            }
        }

        public void StartWaves()
        {
            this.State = LevelState.Running;
            this.OnStartWaves?.Invoke();
            foreach (var entry in this.EnableOnStartWaves)
            {
                entry.enabled = true;
            }
        }

        public bool LevelEnd()
        {
            this.State = LevelState.PostLevel;
            return LevelVM.Instance.RunRegisteredFunction("end");
        }

        public void GetMoney(int amount)
        {
            this.Money += amount;
            this.MoneyTotal += amount;
        }

        public bool BuyTank(WeaponType type, int index, bool checkMoney = true)
        {
            if (!EquipmentManager.Instance.TryGetTank(type, out var tankPrefab))
            {
                return false;
            }

            if (checkMoney)
            {
                if (this.Money < tankPrefab.Cost || tankPrefab.Prefab == null)
                {
                    return false;
                }

                this.Money -= tankPrefab.Cost;
            }

            var tank = Instantiate(tankPrefab.Prefab, this.transform);
            if (tank == null)
            {
                return false;
            }

            this.Player.SetTank(tank, index);
            return true;
        }

        public bool SellTank(int index)
        {
            if (this.Player.SellTank(index, out var returnedMoney))
            {
                this.Money += returnedMoney;
                return true;
            }

            return false;
        }

        public bool UpgradeTank(Tank tank)
        {
            if (!EquipmentManager.Instance.TryGetUpgrade(tank.CurrentWeaponType, out var upgradeList))
            {
                return false;
            }

            tank.GetWeaponLevel(tank.CurrentWeaponType, out int baseLevel, out int boostedLevel, out int level);
            var cost = upgradeList.Levels[baseLevel].Cost;
            if (this.Money < cost || baseLevel >= upgradeList.Levels.Count)
            {
                return false;
            }

            if (tank.UpgradeWeapon(tank.CurrentWeaponType))
            {
                this.Money -= cost;
            }

            return true;
        }

        public void SetWeaponLevelBoosted(int level)
        {
            if (this.WeaponLevelsBoosted == level)
            {
                return;
            }

            this.WeaponLevelsBoosted = level;
            this.Player.Tanks.ForEach(t => t?.BoostUpgradeWeapon(this));
        }

        public bool DamagePlanet(int amount)
        {
            var before = this.PlanetCurrentHealth;
            this.PlanetCurrentHealth = Mathf.Clamp(this.PlanetCurrentHealth - amount, 0, this.PlanetMaxHealth);

            if (before > 0 && this.PlanetCurrentHealth <= 0)
            {
                return true;
            }

            return false;
        }
        #endregion
    }
}