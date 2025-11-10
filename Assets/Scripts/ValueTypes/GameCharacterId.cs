using System;

#nullable enable

namespace Orbits
{
    public readonly struct GameCharacterId : IEquatable<string>
    {
        #region Fields
        public static readonly GameCharacterId Empty = new("");

        public static readonly GameCharacterId Alice = new("A");
        public static readonly GameCharacterId Bob = new("B");
        public static readonly GameCharacterId Caesar = new("C");
        public static readonly GameCharacterId SSTControl = new("S");

        public readonly string Value;

        public bool IsEmpty => this.Value.Length == 0;
        #endregion

        #region Constructor
        public GameCharacterId(string value)
        {
            this.Value = string.Intern(value);
        }
        #endregion

        #region Methods
        public bool Equals(GameCharacterId other)
        {
            return this.Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            if (obj.GetType() != typeof(GameCharacterId))
            {
                return false;
            }

            return ((GameCharacterId)obj).Value == this.Value;
        }

        public bool Equals(string other)
        {
            return this.Value == other;
        }

        public override int GetHashCode()
        {
            return this.Value.GetHashCode();
        }

        public override string ToString()
        {
            return $"GameCharacterId: {this.Value}";
        }

        public static GameCharacterId FromValue(string input)
        {
            return new GameCharacterId(input);
        }

        public static bool operator==(GameCharacterId input1, GameCharacterId input2)
        {
            return input1.Value == input2.Value;
        }

        public static bool operator!=(GameCharacterId input1, GameCharacterId input2)
        {
            return input1.Value != input2.Value;
        }
        #endregion
    }
}