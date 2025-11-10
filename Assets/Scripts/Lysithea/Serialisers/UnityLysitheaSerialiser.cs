using System;
using System.Linq;
using LysitheaVM;
using UnityEngine;
using Unity.Mathematics;

namespace Orbits.Serialiser
{
    public static partial class LysitheaSerialiser
    {
        #region Methods
        public static IValue Write(double3 input)
        {
            return CreateArray()
                .AddTo(input.x)
                .AddTo(input.y)
                .AddTo(input.z)
                .ToValue();
        }
        public static IValue Write(Vector3 input)
        {
            return CreateArray()
                .AddTo(input.x, input.y, input.z)
                .ToValue();
        }

        public static IValue Write(Quaternion input)
        {
            return CreateArray()
                .AddTo(input.x, input.y, input.z, input.w)
                .ToValue();
        }

        public static IValue Write(Bounds input)
        {
            return CreateObject()
                .Set("center", Write(input.center))
                .Set("size", Write(input.size))
                .ToValue();
        }

        public static IValue Write(BoundingSphere input)
        {
            return CreateObject()
                .Set("center", Write(input.position))
                .Set("radius", input.radius)
                .ToValue();
        }

        public static double3 ReadDouble3(IValue input)
        {
            if (input is IArrayValue arrayValue)
            {
                return new double3(arrayValue.GetIndexDouble(0), arrayValue.GetIndexDouble(1), arrayValue.GetIndexDouble(2));
            }

            throw new Exception("Unable to parse Lysithea value into double3");
        }

        public static Vector3 ReadVector3(IValue input)
        {
            if (input is IArrayValue arrayValue)
            {
                return new Vector3(arrayValue.GetIndexFloat(0), arrayValue.GetIndexFloat(1), arrayValue.GetIndexFloat(2));
            }
            else if (input is IObjectValue objValue)
            {
                objValue.TryGetKey<NumberValue>("x", out var x);
                objValue.TryGetKey<NumberValue>("y", out var y);
                objValue.TryGetKey<NumberValue>("z", out var z);
                return new Vector3(x.FloatValue, y.FloatValue, z.FloatValue);
            }
            else if (input is NumberValue numberValue)
            {
                var num = numberValue.FloatValue;
                return new Vector3(num, num, num);
            }

            throw new Exception("Unable to parse Lysithea value into Vector3");
        }

        public static Quaternion ReadQuaternion(IValue input)
        {
            var x = 0.0f;
            var y = 0.0f;
            var z = 0.0f;
            var w = 0.0f;
            if (input is IArrayValue arrayValue)
            {
                x = arrayValue.GetIndexFloat(0);
                y = arrayValue.GetIndexFloat(1);
                z = arrayValue.GetIndexFloat(2);
                w = arrayValue.GetIndexFloat(3);
            }
            else if (input is IObjectValue objValue)
            {
                objValue.TryGetValue("x", out x);
                objValue.TryGetValue("y", out y);
                objValue.TryGetValue("z", out z);
                objValue.TryGetValue("w", out w);
            }
            else
            {
                throw new Exception("Unable to parse Lysithea value into Quaternion");
            }

            return new Quaternion(x, y, z, w);
        }

        public static Quaternion ReadQuaternionEuler(IValue input)
        {
            var x = 0.0f;
            var y = 0.0f;
            var z = 0.0f;
            if (input is IArrayValue arrayValue)
            {
                x = arrayValue.GetIndexFloat(0);
                y = arrayValue.GetIndexFloat(1);
                z = arrayValue.GetIndexFloat(2);
            }
            else if (input is IObjectValue objValue)
            {
                objValue.TryGetValue("x", out x);
                objValue.TryGetValue("y", out y);
                objValue.TryGetValue("z", out z);
            }
            else
            {
                throw new Exception("Unable to parse Lysithea value into Euler Quaternion");
            }

            return Quaternion.Euler(x, y, z);
        }

        public static Bounds ReadBounds(IValue input)
        {
            if (input is IObjectValue objValue)
            {
                var center = ReadVector3(objValue.Get<IObjectValue>("center"));
                var size = ReadVector3(objValue.Get<IObjectValue>("size"));
                return new Bounds(center, size);
            }

            throw new Exception("Unable to parse Lysithea value into Bounds");
        }

        public static BoundingSphere ReadBoundingSphere(IValue input)
        {
            if (input is IObjectValue objValue)
            {
                var center = ReadVector3(objValue.Get<IObjectValue>("center"));
                var radius = objValue.GetFloat("radius", 0.0f);
                return new BoundingSphere(center, radius);
            }

            throw new Exception("Unable to parse Lysithea value into BoundingSphere");
        }
        #endregion
    }
}