using System;

#nullable enable

namespace Orbits
{
    public readonly struct LevelId : IEquatable<string>
    {
        #region Fields
        public static readonly LevelId Empty = new("");
        public readonly string Value;

        public bool IsEmpty => string.IsNullOrWhiteSpace(this.Value);
        #endregion

        #region Constructor
        public LevelId(string value)
        {
            this.Value = string.Intern(value);
        }
        #endregion

        #region Methods
        public bool Equals(LevelId other)
        {
            return this.Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            if (obj.GetType() != typeof(LevelId))
            {
                return false;
            }

            return ((LevelId)obj).Value == this.Value;
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
            return $"LevelId: {this.Value}";
        }

        public static LevelId FromValue(string input)
        {
            return new LevelId(input);
        }

        public static bool operator==(LevelId input1, LevelId input2)
        {
            return input1.Value == input2.Value;
        }

        public static bool operator!=(LevelId input1, LevelId input2)
        {
            return input1.Value != input2.Value;
        }
        #endregion
    }
}