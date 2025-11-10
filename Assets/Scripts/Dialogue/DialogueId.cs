using System;

namespace Orbits
{
    public readonly struct DialogueId : IEquatable<string>
    {
        #region Fields
        public static readonly DialogueId Empty = new ("");

        public readonly string Value;
        #endregion

        #region Constructor
        public DialogueId(string value)
        {
            this.Value = string.Intern(value);
        }
        #endregion

        #region Methods
        public bool Equals(DialogueId other)
        {
            return this.Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            if (obj.GetType() != typeof(DialogueId))
            {
                return false;
            }

            return ((DialogueId)obj).Value == this.Value;
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
            return $"DialogueId: {this.Value}";
        }

        public static DialogueId FromValue(string input)
        {
            return new DialogueId(input);
        }
        #endregion
    }
}