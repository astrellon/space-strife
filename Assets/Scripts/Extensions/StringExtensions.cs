using System;
using System.Linq;
using System.Collections.Generic;

namespace Orbits.Extensions
{
    public static class StringExtensions
    {
        #region Methods
        public static bool EqualsIgnoreCase(this string input, string other)
        {
            return input.Equals(other, StringComparison.OrdinalIgnoreCase);
        }

        public static bool ContainsIgnoreCase(this string input, string other)
        {
            return input.Contains(other, StringComparison.OrdinalIgnoreCase);
        }

        public static bool StartsWithIgnoreCase(this string input, string other)
        {
            return input.StartsWith(other, StringComparison.OrdinalIgnoreCase);
        }

        public static bool EndsWithIgnoreCase(this string input, string other)
        {
            return input.EndsWith(other, StringComparison.OrdinalIgnoreCase);
        }
        #endregion
    }
}