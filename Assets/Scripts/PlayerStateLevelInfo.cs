using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

#nullable enable

namespace Orbits
{
    public readonly struct PlayerStateLevelInfo
    {
        #region Fields
        public static readonly PlayerStateLevelInfo Empty = new(false, false, false, false);
        public readonly bool CanStartLevel;
        public readonly bool FirstTimePlaying;
        public readonly bool HasFinishedLevelBefore;
        public readonly bool HasFinishedLevelMaxHealth;
        #endregion

        #region Constructor
        public PlayerStateLevelInfo(bool canStartLevel, bool firstTimePlaying, bool hasFinishedLevelBefore, bool HasFinishedLevelMaxHealth)
        {
            this.CanStartLevel = canStartLevel;
            this.FirstTimePlaying = firstTimePlaying;
            this.HasFinishedLevelBefore = hasFinishedLevelBefore;
            this.HasFinishedLevelMaxHealth = HasFinishedLevelMaxHealth;
        }
        #endregion

        #region Methods
        public static PlayerStateLevelInfo CreateFromCurrent(out Level? level)
        {
            var levelContainer = UIManager.Instance.CurrentLevelSelected;
            if (levelContainer == null)
            {
                level = null;
                return Empty;
            }

            var starSystem = UIManager.Instance.CurrentStarSystem;
            level = levelContainer.LevelPrefab;
            return CreateFrom(starSystem, levelContainer.LevelPrefab, PlayerState.Instance);
        }

        public static PlayerStateLevelInfo CreateFrom(StarSystem? starSystem, Level? level, PlayerState playerState)
        {
            if (starSystem == null || level == null)
            {
                return Empty;
            }

            var canShowStartLevel = false;
            var firstTimePlaying = playerState.IsFirstTimePlayingLevel(starSystem.Id, level.Id);
            var hasFinishedLevelBefore = false;
            var HasFinishedLevelMaxHealth = false;

            if (!playerState.TryGetLevelTotal(starSystem.Id, level.Id, out var stats))
            {
                canShowStartLevel = true;
            }
            else
            {
                canShowStartLevel = playerState.LevelRepeatUnlocked;
                hasFinishedLevelBefore = true;
                HasFinishedLevelMaxHealth = stats.PlanetHealthRemaining >= level.PlanetMaxHealth;
            }

            return new(canShowStartLevel, firstTimePlaying, hasFinishedLevelBefore, HasFinishedLevelMaxHealth);
        }
        #endregion
    }
}