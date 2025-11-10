using System;
using System.Linq;
using LysitheaVM;

namespace Orbits.Serialiser
{
    public static partial class LysitheaSerialiser
    {
        #region Methods
        public static TEnum ReadEnum<TEnum>(IValue input) where TEnum : struct
        {
            if (input is StringValue strValue)
            {
                return ReadEnum<TEnum>(strValue.Value);
            }

            throw new Exception("Unable to parse non string enum");
        }

        public static TEnum ReadEnum<TEnum>(string input) where TEnum : struct
        {
            if (Enum.TryParse<TEnum>(input, true, out var result))
            {
                return result;
            }

            throw new Exception($"Unknown enum value: {input}");
        }
        #endregion
    }

    public static class LysitheaSerialiserExtensions
    {
        #region Methods
        public static bool TryGetIndexEnum<TEnum>(this IArrayValue self, int index, out TEnum result) where TEnum : struct
        {
            if (self.TryGetIndex<StringValue>(index, out var str) && Enum.TryParse<TEnum>(str.Value, true, out result))
            {
                return true;
            }

            result = default(TEnum);
            return false;
        }

        public static TEnum GetIndexEnum<TEnum>(this IArrayValue self, int index, TEnum defaultValue) where TEnum : struct
        {
            if (self.TryGetIndex<StringValue>(index, out var str) && Enum.TryParse<TEnum>(str.Value, true, out var result))
            {
                return result;
            }

            return defaultValue;
        }

        public static bool TryGetEnum<TEnum>(this IObjectValue self, string key, out TEnum result) where TEnum : struct
        {
            if (self.TryGetKey<StringValue>(key, out var str) && Enum.TryParse<TEnum>(str.Value, true, out result))
            {
                return true;
            }

            result = default(TEnum);
            return false;
        }

        public static TEnum GetEnum<TEnum>(this IObjectValue self, string key, TEnum defaultValue) where TEnum : struct
        {
            if (self.TryGetKey<StringValue>(key, out var str) && Enum.TryParse<TEnum>(str.Value, true, out var result))
            {
                return result;
            }

            return defaultValue;
        }
        #endregion
    }
}