using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using LysitheaVM;

namespace Orbits
{
    public readonly struct GameCharacterValue : IObjectValue
    {
        #region Fields
        public static readonly IReadOnlyList<string> Keys = new [] { "name" };
        public IReadOnlyList<string> ObjectKeys => Keys;

        public string TypeName => "gameChar";

        public readonly GameCharacter Value;
        #endregion

        #region Constructor
        public GameCharacterValue(GameCharacter actor)
        {
            this.Value = actor;
            if (actor == null)
            {
                throw new System.Exception("Dialogue actor cannot be null");
            }
        }
        #endregion

        #region Methods
        public int CompareTo(IValue other)
        {
            if (other == null || !(other is GameCharacterValue otherActor))
            {
                return -1;
            }

            return this.Value == otherActor.Value ? 0 : 1;
        }

        public override string ToString() => $"Actor: {this.Value.Name}";
        public string ToStringSerialise() => this.ToString();

        public bool TryGetKey(string key, [NotNullWhen(true)] out IValue value)
        {
            if (key == "name")
            {
                value = new StringValue(this.Value.NameWithColour);
                return true;
            }

            value = NullValue.Value;
            return false;
        }
        #endregion
    }
}
