using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using UnityEngine;

#nullable enable

namespace Orbits
{
    public static class PlayerStateStore
    {
        #region Fields
        public const string UnlockedLevelsKey = "UnlockedLevels";
        public const string UnlockedTanksKey = "UnlockedTanks";
        public const string TankUpgradesKey = "TankUpgrades";
        public const string UnlockedCharactersKey = "UnlockedCharacters";
        public const string CharactersPlayedKey = "CharactersPlayed";
        public const string GameFlagsKey = "GameFlags";
        public const string VersionKey = "Version";

        public const string StatsHitKey = "_hits";
        public const string StatsFiredKey = "_fired";
        public const string StatsDestroyedKey = "_destroyed";
        public const string StatsRemainingHealthKey = "_remainingHealth";
        #endregion

        #region Save Methods
        public static string GetLevelStatsKey(StarSystemId starSystemId, LevelId levelId)
        {
            return $"LevelStats/{starSystemId.Value}/{levelId.Value}";
        }

        public static bool Save(PlayerState state)
        {
            var existingUnlockedLevels = PlayerPrefs.GetString(UnlockedLevelsKey);
            var existingUnlockedTanks = PlayerPrefs.GetString(UnlockedTanksKey);
            var existingTankUpgrades = PlayerPrefs.GetString(TankUpgradesKey);
            var existingUnlockedCharacter = PlayerPrefs.GetString(UnlockedCharactersKey);
            var existingCharactersPlayed = PlayerPrefs.GetString(CharactersPlayedKey);
            var existingGameFlags = PlayerPrefs.GetString(GameFlagsKey);

            try
            {
                var unlockedLevels = MakeUnlockedLevels(state);
                var unlockedTanks = MakeUnlockedTanks(state);
                var tankUpgrades = MakeTankUpgrades(state);
                var unlockedCharacters = MakeUnlockedCharacters(state);
                var charactersPlayed = MakeCharactersPlayed(state);
                var gameFlags = MakeGameFlags(state);

                try
                {
                    PlayerPrefs.SetString(UnlockedLevelsKey, unlockedLevels.GetString());
                    PlayerPrefs.SetString(UnlockedTanksKey, unlockedTanks.GetString());
                    PlayerPrefs.SetString(TankUpgradesKey, tankUpgrades.GetString());
                    PlayerPrefs.SetString(UnlockedCharactersKey, unlockedCharacters.GetString());
                    PlayerPrefs.SetString(CharactersPlayedKey, charactersPlayed.GetString());
                    PlayerPrefs.SetString(GameFlagsKey, gameFlags.GetString());
                }
                catch (Exception exp)
                {
                    Debug.LogError($"Error saving player state to prefs: {exp.Message} {exp.StackTrace}");
                    PlayerPrefs.SetString(UnlockedLevelsKey, existingUnlockedLevels);
                    PlayerPrefs.SetString(UnlockedTanksKey, existingUnlockedTanks);
                    PlayerPrefs.SetString(TankUpgradesKey, existingTankUpgrades);
                    PlayerPrefs.SetString(UnlockedCharactersKey, existingUnlockedCharacter);
                    PlayerPrefs.SetString(CharactersPlayedKey, existingCharactersPlayed);
                    PlayerPrefs.SetString(GameFlagsKey, existingGameFlags);

                    return false;
                }

                var statsWithData = new HashSet<string>();
                foreach (var starSystemKvp in state.TotalLevelStats)
                {
                    foreach (var levelKvp in starSystemKvp.Value)
                    {
                        if (levelKvp.Value.IsEmpty)
                        {
                            continue;
                        }

                        var stats = MakeLevelStats(levelKvp.Value);
                        var key = GetLevelStatsKey(starSystemKvp.Key, levelKvp.Key);
                        var value = stats.GetString();

                        statsWithData.Add(key);
                        Debug.Log($"Level stats: {key} = {value}");
                        PlayerPrefs.SetString(key, value);
                    }
                }

                foreach (var starSystem in GameManager.Instance.StarSystems)
                {
                    var starSystemId = starSystem.Id;
                    foreach (var levelContainer in starSystem.Levels)
                    {
                        var levelId = levelContainer.LevelPrefab.Id;
                        var key = GetLevelStatsKey(starSystemId, levelId);
                        if (statsWithData.Contains(key))
                        {
                            continue;
                        }

                        PlayerPrefs.DeleteKey(key);
                    }
                }
            }
            catch (Exception exp)
            {
                Debug.LogError($"Error creating player save strings: {exp.Message} {exp.StackTrace}");
                return false;
            }

            return true;
        }

        private static StringAppender MakeUnlockedLevels(PlayerState state)
        {
            var result = new StringAppender("|");
            foreach (var kvp in state.UnlockedLevels)
            {
                var starSystemId = kvp.Key;
                foreach (var levelId in kvp.Value)
                {
                    var str = $"{starSystemId.Value}/{levelId.Value}";
                    result.Append(str);
                }
            }

            return result;
        }

        private static StringAppender MakeUnlockedTanks(PlayerState state)
        {
            var result = new StringAppender("|");
            foreach (var type in state.UnlockedTanks)
            {
                result.Append(type.ToString());
            }
            return result;
        }

        private static StringAppender MakeTankUpgrades(PlayerState state)
        {
            var result = new StringAppender("|");
            foreach (var kvp in state.TankUpgrades)
            {
                var type = kvp.Key;
                foreach (var upgrade in kvp.Value)
                {
                    var str = $"{type}/{upgrade}";
                    result.Append(str);
                }
            }
            return result;
        }

        private static StringAppender MakeUnlockedCharacters(PlayerState state)
        {
            var result = new StringAppender("|");
            foreach (var characterId in state.UnlockedCharacters)
            {
                result.Append(characterId.Value);
            }
            return result;
        }

        private static StringAppender MakeCharactersPlayed(PlayerState state)
        {
            var result = new StringAppender("|");

            foreach (var starKvp in state.CharactersPlayedLevels)
            foreach (var levelKvp in starKvp.Value)
            foreach (var charKvp in levelKvp.Value)
            {
                var entry = $"{starKvp.Key.Value}/{levelKvp.Key.Value}/{charKvp.Key.Value}/{charKvp.Value}";
                result.Append(entry);
            }
            return result;
        }

        private static StringAppender MakeGameFlags(PlayerState state)
        {
            var result = new StringAppender("|");
            foreach (var flag in state.GameFlags)
            {
                result.Append(flag);
            }
            return result;
        }

        private static StringAppender MakeLevelStats(IReadOnlyLevelStats stats)
        {
            var result = new StringAppender("|");
            result.Append(StatsFiredKey);
            foreach (var kvp in stats.ShotsFired)
            {
                result.Append($"{kvp.Key} {kvp.Value}");
            }

            result.Append(StatsHitKey);
            foreach (var kvp in stats.ShotsHit)
            {
                result.Append($"{kvp.Key} {kvp.Value}");
            }

            result.Append(StatsDestroyedKey);
            foreach (var kvp in stats.TargetsDestroyed)
            {
                result.Append($"{kvp.Key} {kvp.Value}");
            }

            result.Append(StatsRemainingHealthKey);
            result.Append(stats.PlanetHealthRemaining.ToString());
            return result;
        }
        #endregion

        #region Read Methods
        public static bool Load(PlayerState state)
        {
            try
            {
                var unlockedLevels = LoadUnlockedLevels();
                var unlockedTanks = LoadUnlockedTanks();
                var tankUpgrades = LoadTankUpgrades();
                var unlockedCharacters = LoadUnlockedCharacters();
                var charactersPlayed = LoadCharactersPlayed();
                var gameFlags = LoadGameFlags();

                foreach (var kvp in unlockedLevels)
                {
                    state.UnlockLevel(kvp.Key, kvp.Value, false);
                }
                foreach (var type in unlockedTanks)
                {
                    state.UnlockTank(type, calculateWeaponLevels: false);
                }
                foreach (var kvp in tankUpgrades)
                {
                    state.UnlockTankUpgrade(kvp.Key, kvp.Value, calculateWeaponLevels: false);
                }
                foreach (var characterId in unlockedCharacters)
                {
                    state.UnlockCharacter(characterId);
                }
                foreach (var entry in charactersPlayed)
                {
                    state.SetCharacterPlayed(entry.StarSystemId, entry.LevelId, entry.GameCharId, entry.Count);
                }

                state.SetGameFlags(gameFlags);

                state.CalculateCurrentWeaponLevels(false);

                foreach (var starSystem in GameManager.Instance.StarSystems)
                {
                    var starSystemId = starSystem.Id;
                    foreach (var levelContainer in starSystem.Levels)
                    {
                        var levelId = levelContainer.LevelPrefab.Id;
                        if (TryLoadLevelStats(starSystemId, levelId, out var stats))
                        {
                            state.SetLevelStats(starSystemId, levelId, stats);
                        }
                    }
                }

                return true;
            }
            catch (Exception exp)
            {
                Debug.LogError($"Error loading player state: {exp.Message} {exp.StackTrace}");
                return false;
            }
        }

        public static bool TryLoadLevelStats(StarSystemId starSystemId, LevelId levelId, [NotNullWhen(true)] out LevelStats? stats)
        {
            var key = GetLevelStatsKey(starSystemId, levelId);
            var str = PlayerPrefs.GetString(key, "");
            if (string.IsNullOrWhiteSpace(str))
            {
                stats = null;
                return false;
            }

            stats = LoadLevelStats(str);
            return true;
        }

        public static LevelStats LoadLevelStats(string input)
        {
            var result = new LevelStats();
            var split = Utils.SplitTrim(input, "|");
            var index = 0;
            while (index < split.Count)
            {
                var token = split[index++];
                if (token == StatsFiredKey)
                {
                    LoadStatsInto(result.ShotsFired, split, ref index);
                }
                else if (token == StatsHitKey)
                {
                    LoadStatsInto(result.ShotsHit, split, ref index);
                }
                else if (token == StatsDestroyedKey)
                {
                    LoadStatsInto(result.TargetsDestroyed, split, ref index);
                }
                else if (token == StatsRemainingHealthKey)
                {
                    LoadStatsInto(out int remainingHealth, split, ref index);
                    result.PlanetHealthRemaining = remainingHealth;
                }
            }

            return result;
        }

        private static void LoadStatsInto(out int result, IReadOnlyList<string> split, ref int index)
        {
            var token = split[index++];
            if (!int.TryParse(token, out result))
            {
                Debug.Log($"Unable to parse int from stats: {token}");
            }
        }

        private static void LoadStatsInto<T>(Dictionary<T, long> target, IReadOnlyList<string> split, ref int index) where T : struct
        {
            while (index < split.Count)
            {
                var token = split[index++];
                if (token[0] == '_')
                {
                    index--;
                    break;
                }

                var splitShots = Utils.SplitTrim(token, " ", 2);
                if (!Enum.TryParse<T>(splitShots[0], out var type))
                {
                    Debug.Log($"Unknown weapon type for level stats: {splitShots[0]}");
                    continue;
                }
                if (!long.TryParse(splitShots[1], out var count))
                {
                    Debug.Log($"Unable to parse level stats number value: {splitShots[1]}");
                    continue;
                }

                target[type] = count;
            }
        }

        public static IEnumerable<KeyValuePair<StarSystemId, LevelId>> LoadUnlockedLevels()
        {
            var str = PlayerPrefs.GetString(UnlockedLevelsKey);
            return SplitKeyValues(str)
                .Select(kvp => new KeyValuePair<StarSystemId, LevelId>(new StarSystemId(kvp.Key), new LevelId(kvp.Value)));
        }

        public static IEnumerable<WeaponType> LoadUnlockedTanks()
        {
            var str = PlayerPrefs.GetString(UnlockedTanksKey);
            foreach (var value in SplitValues(str))
            {
                if (Enum.TryParse<WeaponType>(value, true, out var result))
                {
                    yield return result;
                }
                else
                {
                    Debug.LogWarning($"Unknown weapon type: {value}");
                }
            }
        }

        public static IEnumerable<KeyValuePair<WeaponType, string>> LoadTankUpgrades()
        {
            var str = PlayerPrefs.GetString(TankUpgradesKey);
            foreach (var kvp in SplitKeyValues(str))
            {
                if (Enum.TryParse<WeaponType>(kvp.Key, true, out var type))
                {
                    yield return new KeyValuePair<WeaponType, string>(type, kvp.Value);
                }
                else
                {
                    Debug.LogWarning($"Unknown weapon type: {kvp.Key}");
                }
            }
        }

        public static IEnumerable<GameCharacterId> LoadUnlockedCharacters()
        {
            var str = PlayerPrefs.GetString(UnlockedCharactersKey);
            return SplitValues(str).Select(GameCharacterId.FromValue);
        }

        public class GameCharacterPlayedEntry
        {
            public readonly StarSystemId StarSystemId;
            public readonly LevelId LevelId;
            public readonly GameCharacterId GameCharId;
            public readonly int Count;

            public GameCharacterPlayedEntry(StarSystemId starSystemId, LevelId levelId, GameCharacterId gameCharId, int count)
            {
                this.StarSystemId = starSystemId;
                this.LevelId = levelId;
                this.GameCharId = gameCharId;
                this.Count = count;
            }

            public static GameCharacterPlayedEntry From(string input)
            {
                var split = Utils.SplitTrim(input, "/");
                var starSystemId = new StarSystemId(split[0]);
                var levelId = new LevelId(split[1]);
                var gameCharId = new GameCharacterId(split[2]);
                var count = int.Parse(split[3]);

                return new GameCharacterPlayedEntry(starSystemId, levelId, gameCharId, count);
            }
        }
        public static IEnumerable<GameCharacterPlayedEntry> LoadCharactersPlayed()
        {
            var str = PlayerPrefs.GetString(CharactersPlayedKey);
            return SplitValues(str).Select(GameCharacterPlayedEntry.From);
        }

        public static HashSet<string> LoadGameFlags()
        {
            var str = PlayerPrefs.GetString(GameFlagsKey);
            return new HashSet<string>(SplitValues(str));
        }

        public static List<string> SplitValues(string str)
        {
            if (string.IsNullOrWhiteSpace(str))
            {
                return new List<string>();
            }

            return Utils.SplitTrim(str, "|");
        }

        public static List<KeyValuePair<string, string>> SplitKeyValues(string str)
        {
            var result = new List<KeyValuePair<string, string>>();
            if (string.IsNullOrWhiteSpace(str))
            {
                return result;
            }

            var split = Utils.SplitTrim(str, "|");
            foreach (var item in split)
            {
                var keySplit = Utils.SplitTrim(item, "/", 2);
                result.Add(new KeyValuePair<string, string>(keySplit[0], keySplit[1]));
            }
            return result;
        }
        #endregion
    }
}