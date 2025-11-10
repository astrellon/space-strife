using System;
using System.Linq;
using System.Collections.Generic;
using LysitheaVM;
using UnityEngine;
using Unity.Mathematics;
using Orbits.Extensions;

namespace Orbits.Serialiser
{
    public static class ExtensionsLysitheaSerialiser
    {
        #region Methods
        public static T Get<T>(this IObjectValue self, string key) where T : IValue
        {
            if (self.TryGetKey<T>(key, out var result))
            {
                return result;
            }

            throw new Exception($"Unable to get value off object: {key}");
        }

        public static bool TryGetValue(this IObjectValue self, string key, out Vector3 result)
        {
            if (self.TryGetKey(key, out var value))
            {
                result = LysitheaSerialiser.ReadVector3(value);
                return true;
            }

            result = Vector3.zero;
            return false;
        }

        public static bool TryGetValue(this IObjectValue self, string key, out double3 result)
        {
            if (self.TryGetKey(key, out var value))
            {
                result = LysitheaSerialiser.ReadDouble3(value);
                return true;
            }

            result = double3.zero;
            return false;
        }

        public static Dictionary<string, IValue> Set(this Dictionary<string, IValue> self, string key, bool value)
        {
            self[key] = new BoolValue(value);
            return self;
        }

        public static Dictionary<string, IValue> Set(this Dictionary<string, IValue> self, string key, int value)
        {
            self[key] = new NumberValue(value);
            return self;
        }

        public static Dictionary<string, IValue> Set(this Dictionary<string, IValue> self, string key, float value)
        {
            self[key] = new NumberValue(value);
            return self;
        }

        public static Dictionary<string, IValue> Set(this Dictionary<string, IValue> self, string key, double value)
        {
            self[key] = new NumberValue(value);
            return self;
        }

        public static Dictionary<string, IValue> Set(this Dictionary<string, IValue> self, string key, string value)
        {
            self[key] = new StringValue(value);
            return self;
        }
        public static Dictionary<string, IValue> Set(this Dictionary<string, IValue> self, string key, Enum value)
        {
            self[key] = new StringValue(value.ToString());
            return self;
        }


        public static Dictionary<string, IValue> Set(this Dictionary<string, IValue> self, string key, IValue value)
        {
            self[key] = value;
            return self;
        }

        public static Dictionary<string, IValue> SetIfNot(this Dictionary<string, IValue> self, string key, bool value, bool ifNotValue)
        {
            if (value != ifNotValue)
            {
                self[key] = new BoolValue(value);
            }
            return self;
        }

        public static Dictionary<string, IValue> SetIfNot(this Dictionary<string, IValue> self, string key, int value, int ifNotValue)
        {
            if (value != ifNotValue)
            {
                self[key] = new NumberValue(value);
            }
            return self;
        }

        public static Dictionary<string, IValue> SetIfNot(this Dictionary<string, IValue> self, string key, float value, float ifNotValue)
        {
            if (value != ifNotValue)
            {
                self[key] = new NumberValue(value);
            }
            return self;
        }

        public static Dictionary<string, IValue> SetIfNot(this Dictionary<string, IValue> self, string key, string value, string ifNotValue)
        {
            if (value != ifNotValue)
            {
                self[key] = new StringValue(value);
            }
            return self;
        }

        public static ObjectValue ToValue(this Dictionary<string, IValue> self)
        {
            return new ObjectValue(self);
        }

        public static List<IValue> AddTo(this List<IValue> self, string value)
        {
            self.Add(new StringValue(value));
            return self;
        }

        public static List<IValue> AddTo(this List<IValue> self, bool value)
        {
            self.Add(new BoolValue(value));
            return self;
        }

        public static List<IValue> AddTo(this List<IValue> self, int value)
        {
            self.Add(new NumberValue(value));
            return self;
        }

        public static List<IValue> AddTo(this List<IValue> self, params int[] values)
        {
            foreach (var value in values)
            {
                self.Add(new NumberValue(value));
            }
            return self;
        }

        public static List<IValue> AddTo(this List<IValue> self, double value)
        {
            self.Add(new NumberValue(value));
            return self;
        }

        public static List<IValue> AddTo(this List<IValue> self, params double[] values)
        {
            foreach (var value in values)
            {
                self.Add(new NumberValue(value));
            }
            return self;
        }

        public static List<IValue> AddTo(this List<IValue> self, float value)
        {
            self.Add(new NumberValue(value));
            return self;
        }

        public static List<IValue> AddTo(this List<IValue> self, params float[] values)
        {
            foreach (var value in values)
            {
                self.Add(new NumberValue(value));
            }
            return self;
        }

        public static List<IValue> AddTo(this List<IValue> self, params string[] values)
        {
            foreach (var value in values)
            {
                self.Add(new StringValue(value));
            }
            return self;
        }

        public static List<IValue> AddTo(this List<IValue> self, IValue value)
        {
            self.Add(value);
            return self;
        }

        public static ArrayValue ToValue(this List<IValue> self)
        {
            return new ArrayValue(self);
        }

        public static IEnumerable<int> ToInts(this IArrayValue self)
        {
            return self.ArrayValues.TryCast<NumberValue>().Select(n => n.IntValue);
        }

        public static IEnumerable<float> ToFloats(this IArrayValue self)
        {
            return self.ArrayValues.TryCast<NumberValue>().Select(n => n.FloatValue);
        }

        public static IEnumerable<double> ToDoubles(this IArrayValue self)
        {
            return self.ArrayValues.TryCast<NumberValue>().Select(n => n.Value);
        }
        #endregion
    }
}