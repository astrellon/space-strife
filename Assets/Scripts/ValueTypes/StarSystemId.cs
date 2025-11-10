using System;

#nullable enable

namespace Orbits
{
    public readonly struct StarSystemId : IEquatable<string>
    {
        #region Fields
        public static readonly StarSystemId Empty = new("");

        public readonly string Value;
        public bool IsEmpty => string.IsNullOrWhiteSpace(this.Value);
        #endregion

        #region Constructor
        public StarSystemId(string value)
        {
            this.Value = string.Intern(value);
        }
        #endregion

        #region Methods
        public bool Equals(StarSystemId other)
        {
            return this.Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            if (obj.GetType() != typeof(StarSystemId))
            {
                return false;
            }

            return ((StarSystemId)obj).Value == this.Value;
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
            return $"StarSystemId: {this.Value}";
        }

        public static StarSystemId FromValue(string input)
        {
            return new StarSystemId(input);
        }

        public static bool operator==(StarSystemId input1, StarSystemId input2)
        {
            return input1.Value == input2.Value;
        }

        public static bool operator!=(StarSystemId input1, StarSystemId input2)
        {
            return input1.Value != input2.Value;
        }
        #endregion
    }
}