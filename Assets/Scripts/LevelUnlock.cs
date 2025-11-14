using System;
using LysitheaVM;

#nullable enable

namespace Orbits
{
    public class LevelUnlockResult
    {
        public readonly string Message;
        public readonly bool CameraToStarSystem;

        public LevelUnlockResult(string message, bool cameraToStarSystem = false)
        {
            this.Message = message;
            this.CameraToStarSystem = cameraToStarSystem;
        }
    }

    [Serializable]
    public struct LevelUnlock
    {
        public enum UnlockType
        {
            Unknown, Level, UnlockWeapon, UpgradeWeapon, FinishGame, UnlockCharacter, LevelRepeat
        }

        #region Fields
        public UnlockType Type;
        public string LevelId;
        public string StarSystemId;
        public string WeaponUpgradeId;
        public string CharacterId;
        public WeaponType WeaponType;
        #endregion

        #region Methods
        public readonly LevelUnlockResult Execute(Level currentLevel)
        {
            var cameraToStarSystem = false;
            var message = "";

            if (this.Type == UnlockType.Level)
            {
                var starSystemId = new StarSystemId(this.StarSystemId);
                var levelId = new LevelId(this.LevelId);
                if (GameManager.Instance.TryGetLevel(starSystemId, levelId, out var starSystem, out var level))
                {
                    // Always reset the camera
                    if (currentLevel.PartOfStarSystem != starSystem)
                    {
                        cameraToStarSystem = true;
                    }

                    if (PlayerState.Instance.UnlockLevel(starSystemId, levelId))
                    {
                        if (currentLevel.PartOfStarSystem != starSystem)
                        {
                            if (!LevelVM.Instance.TryGetColouredName(starSystemId.Value, out var name))
                            {
                                name = Utils.ColouredText(UIManager.Instance.StarSystemColour, starSystem.name);
                            }

                            message = $"New star system {name}!";
                        }
                        else
                        {
                            message = $"New level {Utils.ColouredText(UIManager.Instance.LevelColour, level.LevelPrefab.LevelName)}!";
                        }
                    }
                }
            }
            else if (this.Type == UnlockType.UnlockWeapon)
            {
                if (PlayerState.Instance.UnlockTank(this.WeaponType))
                {
                    message = $"New tank {Utils.ColouredText(UIManager.Instance.WeaponColour, this.WeaponType.ToString())}!";
                }
            }
            else if (this.Type == UnlockType.UpgradeWeapon)
            {
                if (PlayerState.Instance.UnlockTankUpgrade(this.WeaponType, this.WeaponUpgradeId))
                {
                    message = $"New {Utils.ColouredText(UIManager.Instance.WeaponColour, this.WeaponType.ToString())} tank upgrade {Utils.ColouredText(UIManager.Instance.UpgradeColour, this.WeaponUpgradeId)}!";
                }
            }
            else if (this.Type == UnlockType.UnlockCharacter)
            {
                var charId = new GameCharacterId(this.CharacterId);
                if (PlayerState.Instance.UnlockCharacter(charId))
                {
                    if (GameManager.Instance.TryGetCharacter(charId, out var character))
                    {
                        message = $"New character {character.NameWithColour}!";
                    }
                }
            }
            else if (this.Type == UnlockType.LevelRepeat)
            {
                PlayerState.Instance.SetGameFlag("levelRepeat", true);
                message = Utils.ColouredText(UIManager.Instance.LevelColour, "You can repeat levels now!");
            }

            return new LevelUnlockResult(message, cameraToStarSystem);
        }

        public static bool TryParse(IObjectValue value, out LevelUnlock result)
        {
            if (value.TryGetValue("type", out string typeValue) && TryConvert(typeValue, out UnlockType unlockType))
            {
                var levelId = value.GetString("level") ?? "";
                var starSystemId = value.GetString("starSystem") ?? "";
                var weaponUpgradeId = value.GetString("weaponUpgrade") ?? "";
                TryConvert(value.GetString("weapon") ?? "", out WeaponType weaponType);

                result = new LevelUnlock()
                {
                    Type = unlockType,
                    LevelId = levelId,
                    StarSystemId = starSystemId,
                    WeaponUpgradeId = weaponUpgradeId,
                    WeaponType = weaponType
                };
                return true;
            }

            result = new LevelUnlock() { Type = UnlockType.Unknown };
            return false;
        }

        public static bool TryConvert(string input, out UnlockType type)
        {
            type = input switch
            {
                "level" => UnlockType.Level,
                "weapon" => UnlockType.UnlockWeapon,
                "upgrade" => UnlockType.UpgradeWeapon,
                "finishGame" => UnlockType.FinishGame,
                "character" => UnlockType.UnlockCharacter,
                _ => UnlockType.Unknown
            };

            return type != UnlockType.Unknown;
        }

        public static bool TryConvert(string input, out WeaponType type)
        {
            Enum.TryParse(input, out type);
            return type != WeaponType.Unknown;
        }
        #endregion
    }
}