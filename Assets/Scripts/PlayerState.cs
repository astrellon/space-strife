using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using UnityEngine;

#nullable enable

namespace Orbits
{
    [Serializable]
    public struct LevelLocked
    {
        public string StarSystemId;
        public string LevelId;
        public string LockedIfFlag;
    }

    [DefaultExecutionOrder(1)]
    public class PlayerState : MonoBehaviour
    {
        #region Fields
        public static PlayerState Instance;
        public static readonly IReadOnlyDictionary<GameCharacterId, int> EmptyGameCharacterPlays = new Dictionary<GameCharacterId, int>();

        public Dictionary<WeaponType, int> TankLevels = new();
        public long TotalMoney = 0L;

        public delegate void LevelUnlockHandler(StarSystemId starSystem, LevelId level, bool unlocked);

        public event LevelUnlockHandler? OnLevelUnlock;

        public Dictionary<StarSystemId, Dictionary<LevelId, LevelStats>> TotalLevelStats = new();
        public Dictionary<StarSystemId, HashSet<LevelId>> UnlockedLevels = new();
        public Dictionary<StarSystemId, Dictionary<LevelId, Dictionary<GameCharacterId, int>>> CharactersPlayedLevels = new();
        public HashSet<GameCharacterId> UnlockedCharacters = new();
        public HashSet<WeaponType> UnlockedTanks = new();
        public Dictionary<WeaponType, HashSet<string>> TankUpgrades = new();

        public List<LevelLocked> LevelsLocked = new();

        public LevelId CurrentLevelId = LevelId.Empty;
        public StarSystemId CurrentStarSystemId = StarSystemId.Empty;
        public LevelStats CurrentLevelStats = new();
        public List<GameCharacter> OrderedUnlockedCharacters = new();

        public bool LevelRepeatUnlocked = false;

        public IReadOnlyCollection<string> GameFlags => this.gameFlags;
        private HashSet<string> gameFlags = new();
        private HashSet<string> currentLevelGameFlags = new();
        private Dictionary<StarSystemId, Dictionary<LevelId, LevelLocked>> levelsLockedMap = new();
        #endregion

        #region Unity Methods
        private void Awake()
        {
            Instance = this;

            PlayerStateStore.Load(this);
            foreach (var locked in this.LevelsLocked)
            {
                var starSystemId = new StarSystemId(locked.StarSystemId);
                if (!this.levelsLockedMap.TryGetValue(starSystemId, out var levelsMap))
                {
                    levelsMap = new();
                    this.levelsLockedMap[starSystemId] = levelsMap;
                }

                var levelId = new LevelId(locked.LevelId);
                levelsMap[levelId] = locked;
            }

            this.UpdateFinished();
            this.InitPlayer();

            this.CalculateCurrentWeaponLevels(saveResult: false);
        }
        #endregion

        #region Methods
        public void UpdateFinished()
        {
            var unlockedLevels = this.UnlockedLevels.ToList();
            foreach (var starSystemKvp in unlockedLevels)
            {
                var starSystemId = starSystemKvp.Key;
                foreach (var levelId in starSystemKvp.Value)
                {
                    if (!GameManager.Instance.TryGetLevel(starSystemId, levelId, out _, out var levelContainer))
                    {
                        continue;
                    }
                    if (!this.TryGetLevelTotal(starSystemId, levelId, out var levelStats) || levelStats.IsEmpty)
                    {
                        continue;
                    }

                    foreach (var unlock in levelContainer.LevelPrefab.LevelUnlocks)
                    {
                        var result = unlock.Execute(levelContainer.LevelPrefab);
                        if (!string.IsNullOrWhiteSpace(result.Message))
                        {
                            Debug.Log($"New unlock: {result.Message}");
                        }
                    }

                    if (levelStats.PlanetHealthRemaining >= levelContainer.LevelPrefab.PlanetMaxHealth)
                    {
                        foreach (var unlock in levelContainer.LevelPrefab.LevelFullHealthUnlocks)
                        {
                            var result = unlock.Execute(levelContainer.LevelPrefab);
                            if (!string.IsNullOrWhiteSpace(result.Message))
                            {
                                Debug.Log($"New full health unlock: {result.Message}");
                            }
                        }
                    }
                }
            }
        }

        public void UnlockAll()
        {
            foreach (var starSystem in GameManager.Instance.StarSystems)
            {
                foreach (var levelContainer in starSystem.Levels)
                {
                    if (levelContainer.LevelPrefab == null)
                    {
                        continue;
                    }

                    this.UnlockLevel(starSystem.Id, levelContainer.LevelPrefab.Id);
                }
            }

            this.UnlockCharacter(GameCharacterId.Bob);
            this.UnlockCharacter(GameCharacterId.Caesar);

            this.UnlockTank(WeaponType.Bolt, calculateWeaponLevels: false);
            this.UnlockTank(WeaponType.Gauss, calculateWeaponLevels: false);
            this.UnlockTank(WeaponType.Rocket, calculateWeaponLevels: false);
            this.UnlockTank(WeaponType.Laser, calculateWeaponLevels: false);
            this.UnlockTank(WeaponType.RapidLaser, calculateWeaponLevels: false);

            this.UnlockTankUpgrade(WeaponType.Bolt, "Upgrade 1");
            this.UnlockTankUpgrade(WeaponType.Bolt, "Upgrade 2");
            this.UnlockTankUpgrade(WeaponType.Gauss, "Upgrade 1");
            this.UnlockTankUpgrade(WeaponType.Gauss, "Upgrade 2");
            this.UnlockTankUpgrade(WeaponType.Rocket, "Upgrade 1");
            this.UnlockTankUpgrade(WeaponType.Rocket, "Upgrade 2");
            this.UnlockTankUpgrade(WeaponType.Laser, "Upgrade 1");
            this.UnlockTankUpgrade(WeaponType.Laser, "Upgrade 2");
            this.UnlockTankUpgrade(WeaponType.RapidLaser, "Upgrade 1");
            this.UnlockTankUpgrade(WeaponType.RapidLaser, "Upgrade 2");
        }

        public void ResetPlayer()
        {
            this.TotalLevelStats = new();
            foreach (var starKvp in this.UnlockedLevels)
            {
                foreach (var levelId in starKvp.Value)
                {
                    this.OnLevelUnlock?.Invoke(starKvp.Key, levelId, unlocked: false);
                }
            }

            this.UnlockedLevels = new();
            this.UnlockedTanks = new();
            this.TankUpgrades = new();
            this.CurrentLevelStats = new();
            this.UnlockedCharacters = new();
            this.CharactersPlayedLevels = new();
            this.SetGameFlags(new());

            this.InitPlayer();

            this.CalculateCurrentWeaponLevels(saveResult: true);
        }

        public void InitPlayer()
        {
            var startingStarSystem = new StarSystemId("s1");
            var startingLevel = new LevelId("l1");
            this.UnlockLevel(startingStarSystem, startingLevel, saveUpdate: false);

            this.SetGameFlag("needShowUpgradeTutorial", true);

            // startingStarSystem = new StarSystemId("s1");
            // startingLevel = new LevelId("l4");
            // this.UnlockLevel(startingStarSystem, startingLevel, saveUpdate: false);

            // startingStarSystem = new StarSystemId("s4");
            // startingLevel = new LevelId("l1");
            // this.UnlockLevel(startingStarSystem, startingLevel, saveUpdate: false);

            this.UnlockCharacter(GameCharacterId.Alice);
            // this.UnlockCharacter(GameCharacterId.Bob);
            // this.UnlockCharacter(GameCharacterId.Caesar);
            this.UnlockTank(WeaponType.Bolt, calculateWeaponLevels: false);
        }

        public void CalculateCurrentWeaponLevels(bool saveResult)
        {
            this.TankLevels.Clear();

            foreach (var weaponType in this.UnlockedTanks)
            {
                var level = EquipmentManager.Instance.GetStartingWeaponLevel(weaponType);
                if (this.TankUpgrades.TryGetValue(weaponType, out var upgrades))
                {
                    var upgradeLevels = upgrades.Count;
                    level += upgradeLevels;
                }
                this.TankLevels[weaponType] = level;
            }

            if (saveResult)
            {
                PlayerStateStore.Save(this);
            }
        }

        public bool IsLevelUnlocked(StarSystemId starSystemId, LevelId levelId)
        {
            var isUnlocked = this.UnlockedLevels.TryGetValue(starSystemId, out var levels) && levels.Contains(levelId);
            if (isUnlocked)
            {
                if (this.levelsLockedMap.TryGetValue(starSystemId, out var levelMap) &&
                    levelMap.TryGetValue(levelId, out var locked))
                {
                    if (this.gameFlags.Contains(locked.LockedIfFlag))
                    {
                        return false;
                    }
                }
            }

            return isUnlocked;
        }

        public bool IsStarSystemUnlocked(StarSystemId starSystemId)
        {
            return this.UnlockedLevels.TryGetValue(starSystemId, out var levels) && levels.Any();
        }

        public bool UnlockLevel(StarSystemId starSystemId, LevelId levelId, bool saveUpdate = true)
        {
            if (!this.UnlockedLevels.TryGetValue(starSystemId, out var levels))
            {
                this.UnlockedLevels[starSystemId] = levels = new HashSet<LevelId>();
            }

            if (levels.Add(levelId))
            {
                this.OnLevelUnlock?.Invoke(starSystemId, levelId, unlocked: true);

                if (saveUpdate)
                {
                    PlayerStateStore.Save(this);
                }
                return true;
            }
            return false;
        }

        public void MarkUpgradeTutorial()
        {
            this.SetGameFlag("needShowUpgradeTutorial", false);
        }

        public bool UnlockCharacter(GameCharacterId characterId)
        {
            if (this.UnlockedCharacters.Add(characterId))
            {
                this.UpdateCharacterList();
                return true;
            }
            return false;
        }

        public bool RemoveCharacter(GameCharacterId characterId)
        {
            if (this.UnlockedCharacters.Remove(characterId))
            {
                this.UpdateCharacterList();
                return true;
            }
            return false;
        }

        public void ToggleCharacter(GameCharacterId characterId)
        {
            if (this.UnlockedCharacters.Contains(characterId))
            {
                this.RemoveCharacter(characterId);
            }
            else
            {
                this.UnlockCharacter(characterId);
            }
        }

        private void UpdateCharacterList()
        {
            this.OrderedUnlockedCharacters.Clear();
            foreach (var charId in this.UnlockedCharacters)
            {
                if (GameManager.Instance.TryGetCharacter(charId, out var gameCharacter))
                {
                    this.OrderedUnlockedCharacters.Add(gameCharacter);
                }
            }

            this.OrderedUnlockedCharacters.Sort((x, y) => x.ShowOrder.CompareTo(y.ShowOrder));
        }

        public bool UnlockTank(WeaponType weaponType, bool calculateWeaponLevels = true)
        {
            if (this.UnlockedTanks.Add(weaponType))
            {
                if(calculateWeaponLevels)
                {
                    this.CalculateCurrentWeaponLevels(true);
                }
                return true;
            }
            return false;
        }

        public bool UnlockTankUpgrade(WeaponType weaponType, string upgradeId, bool calculateWeaponLevels = true)
        {
            if (!this.TankUpgrades.TryGetValue(weaponType, out var list))
            {
                this.TankUpgrades[weaponType] = list = new();
            }

            if (list.Add(upgradeId))
            {
                if (calculateWeaponLevels)
                {
                    this.CalculateCurrentWeaponLevels(true);
                }
                return true;
            }
            return false;
        }

        public void SetLevelStats(StarSystemId starSystemId, LevelId levelId, LevelStats stats)
        {
            if (!this.TotalLevelStats.TryGetValue(starSystemId, out var starSystemLevels))
            {
                this.TotalLevelStats[starSystemId] = starSystemLevels = new Dictionary<LevelId, LevelStats>();
            }

            starSystemLevels[levelId] = stats;
        }

        public void AddLevelStats(StarSystemId starSystemId, LevelId levelId, LevelStats stats)
        {
            if (!this.TotalLevelStats.TryGetValue(starSystemId, out var starSystemLevels))
            {
                this.TotalLevelStats[starSystemId] = starSystemLevels = new Dictionary<LevelId, LevelStats>();
            }

            if (!starSystemLevels.TryGetValue(levelId, out var totalLevelStats))
            {
                starSystemLevels[levelId] = totalLevelStats = new();
            }

            starSystemLevels[levelId] = LevelStats.Combine(totalLevelStats, stats);
        }

        public bool TryGetLevelTotal(StarSystemId starSystemId, LevelId levelId, out IReadOnlyLevelStats result)
        {
            if (this.TotalLevelStats.TryGetValue(starSystemId, out var starSystemLevels) &&
                starSystemLevels.TryGetValue(levelId, out var stats))
            {
                result = stats;
                return true;
            }

            result = Orbits.LevelStats.Empty;
            return false;
        }

        public bool IsFirstTimePlayingLevel(StarSystemId starSystemId, LevelId levelId)
        {
            if (!this.TryGetLevelTotal(starSystemId, levelId, out _))
            {
                return true;
            }

            return false;
        }

        public void StartNewLevelStats(StarSystemId starSystemId, LevelId levelId)
        {
            this.CurrentLevelId = levelId;
            this.CurrentStarSystemId = starSystemId;
            this.CurrentLevelStats = new();

            this.currentLevelGameFlags = new HashSet<string>(this.gameFlags);
        }

        public void ClearLevelStats()
        {
            this.CurrentLevelId = LevelId.Empty;
            this.CurrentStarSystemId = StarSystemId.Empty;
            this.CurrentLevelStats = new();
        }

        public void FinishLevelStats(IEnumerable<GameCharacterId> playedWithCharacters, Level level)
        {
            if (this.CurrentLevelId.IsEmpty)
            {
                return;
            }

            this.CurrentLevelStats.PlanetHealthRemaining = level.PlanetCurrentHealth;
            this.AddLevelStats(this.CurrentStarSystemId, this.CurrentLevelId, this.CurrentLevelStats);
            foreach (var gameCharId in playedWithCharacters)
            {
                this.IncCharacterPlayed(this.CurrentStarSystemId, this.CurrentLevelId, gameCharId, 1);
            }

            this.gameFlags = new HashSet<string>(this.currentLevelGameFlags);

            PlayerStateStore.Save(this);
            this.ClearLevelStats();
        }

        public void IncCharacterPlayed(StarSystemId starSystemId, LevelId levelId, GameCharacterId gameCharId, int count)
        {
            if (!this.CharactersPlayedLevels.TryGetValue(starSystemId, out var levelMap))
            {
                this.CharactersPlayedLevels[starSystemId] = levelMap = new();
            }
            if (!levelMap.TryGetValue(levelId, out var characterMap))
            {
                levelMap[levelId] = characterMap = new();
            }
            if (!characterMap.TryGetValue(gameCharId, out var currentCount))
            {
                currentCount = 0;
            }

            currentCount += count;
            characterMap[gameCharId] = currentCount;
        }

        public void SetCharacterPlayed(StarSystemId starSystemId, LevelId levelId, GameCharacterId gameCharId, int value)
        {
            if (!this.CharactersPlayedLevels.TryGetValue(starSystemId, out var levelMap))
            {
                this.CharactersPlayedLevels[starSystemId] = levelMap = new();
            }
            if (!levelMap.TryGetValue(levelId, out var characterMap))
            {
                levelMap[levelId] = characterMap = new();
            }

            characterMap[gameCharId] = value;
        }

        public IReadOnlyDictionary<GameCharacterId, int> GetCharacterPlays(StarSystemId starSystemId, LevelId levelId)
        {
            if (this.CharactersPlayedLevels.TryGetValue(starSystemId, out var levelMap) &&
                levelMap.TryGetValue(levelId, out var gameCharMap))
            {
                return gameCharMap;
            }

            return EmptyGameCharacterPlays;
        }

        public bool TryGetUpgrade(WeaponType weaponType, [NotNullWhen(true)] out WeaponUpgradeList? result)
        {
            var hasUpgradeList = EquipmentManager.Instance.TryGetUpgrade(weaponType, out var list);
            if (hasUpgradeList && list != null)
            {
                if (this.TankLevels.TryGetValue(weaponType, out var maxLevel))
                {
                    result = list.LimitLevels(maxLevel);
                    return true;
                }

                var startingLevel = EquipmentManager.Instance.GetStartingWeaponLevel(weaponType);
                if (startingLevel > 0)
                {
                    result = list.LimitLevels(startingLevel);
                    return true;
                }
            }

            result = null;
            return false;
        }

        public bool HasGameFlag(string key)
        {
            return this.currentLevelGameFlags.Contains(key);
        }

        public void SetGameFlag(string key, bool value)
        {
            if (value)
            {
                this.currentLevelGameFlags.Add(key);
            }
            else
            {
                this.currentLevelGameFlags.Remove(key);
            }

            // if (key == "levelRepeat")
            // {
            //     this.LevelRepeatUnlocked = value;
            // }
        }

        public void SetGameFlags(HashSet<string> flags)
        {
            this.gameFlags = flags;
            this.LevelRepeatUnlocked = flags.Contains("levelRepeat");
        }
        #endregion
    }
}