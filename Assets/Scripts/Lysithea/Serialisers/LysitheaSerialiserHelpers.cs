using System;
using System.Collections.Generic;
using System.Linq;
using LysitheaVM;

namespace Orbits.Serialiser
{
    public static partial class LysitheaSerialiser
    {
        #region Methods
        public static Dictionary<string, IValue> CreateObject()
        {
            return new Dictionary<string, IValue>();
        }

        public static List<IValue> CreateArray()
        {
            return new List<IValue>();
        }

        public static bool TryGetListOrStringValue(IObjectValue obj, string key, out IReadOnlyList<string> result)
        {
            if (obj.TryGetKey(key, out var temp))
            {
                return TryGetListOrStringValue(temp, out result);
            }

            result = Utils.EmptyStrings;
            return false;
        }

        public static IValue MakeStringOrList(IReadOnlyList<string> input)
        {
            if (!input.Any())
            {
                return ArrayValue.Empty;
            }

            if (input.Count() == 1)
            {
                return new StringValue(input.First());
            }
            else
            {
                return new ArrayValue(input.Select(s => new StringValue(s) as IValue).ToList());
            }
        }

        public static bool TryGetListOrStringValue(IValue input, out IReadOnlyList<string> result)
        {
            if (input is StringValue strValue)
            {
                result = new string[] { strValue.Value };
                return true;
            }
            else if (input is IArrayValue value)
            {
                var tempResult = new List<string>();
                foreach (var item in value.ArrayValues)
                {
                    if (item is StringValue itemStrValue)
                    {
                        tempResult.Add(itemStrValue.Value);
                    }
                    else
                    {
                        result = Utils.EmptyStrings;
                        return false;
                    }
                }

                result = tempResult;
                return result.Any();
            }

            result = Utils.EmptyStrings;
            return false;
        }
        #endregion
    }
}