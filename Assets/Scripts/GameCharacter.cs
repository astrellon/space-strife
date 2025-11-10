using System.Collections.Generic;
using UnityEngine;

#nullable enable

namespace Orbits
{
    public enum CharacterAbility
    {
        None, Power, Split, TimeControl
    }

    public class GameCharacter : MonoBehaviour
    {
        #region Fields
        public string CharacterId = "Unknown";
        public string Name = "Unknown";
        public string Description = "";
        public Sprite? Portrait;
        public int ShowOrder = 100;
        public Color NameColour = Color.white;
        public CharacterAbility Ability = CharacterAbility.None;
        public AudioOneOffs? CharacterTalk;
        public Easing.Type PortraitEasing = Easing.Type.QuadraticInOut;

        public string NameWithColour => Utils.ColouredText(this.NameColour, this.Name);
        public GameCharacterId Id => !this.gameCharId.IsEmpty ? this.gameCharId : this.gameCharId = new(this.CharacterId);

        private GameCharacterId gameCharId = GameCharacterId.Empty;
        #endregion

        #region Methods
        public IEnumerable<Ability> GetAbilities()
        {
            if (this.Ability == CharacterAbility.Power)
            {
                yield return new PowerUpAbility();
            }
            else if (this.Ability == CharacterAbility.Split)
            {
                yield return new SplitAbility();
            }
            else if (this.Ability == CharacterAbility.TimeControl)
            {
                yield return new TimeControlAbility();
            }
        }
        #endregion
    }
}