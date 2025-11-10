using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using Unity.Mathematics;
using System.Collections.Generic;

#nullable enable

namespace Orbits
{
    public static class Utils
    {
        public static readonly IReadOnlyList<string> EmptyStrings = new string[0];

        #region Methods
        public static float WrapAngle(float input)
        {
            while (input < 0.0f) input += 360.0f;
            while (input > 360.0f) input -= 360.0f;

            return input;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddDistinct<T>(List<T> list, T item)
        {
            if (!list.Contains(item))
            {
                list.Add(item);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 TakeXZ(Vector3 position)
        {
            return new Vector3(position.x, 0.0f, position.z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 ToXZ(Vector3 position)
        {
            return new float2(position.x, position.z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 FromXZ(float2 position)
        {
            return new Vector3(position.x, 0, position.y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 FromXZ(Vector2 position)
        {
            return new Vector3(position.x, 0, position.y);
        }

        public static Vector3 FromAngle(float angle, float length)
        {
            var x = Mathf.Cos(angle) * length;
            var z = Mathf.Sin(angle) * length;
            return new Vector3(x, 0, z);
        }

        public static void RemoveAllChildren(Transform target)
        {
            while (target.childCount > 0)
            {
                GameObject.DestroyImmediate(target.GetChild(0).gameObject);
            }
        }

        public static void RemoveAllChildren(Transform target, System.Func<GameObject, bool> predicate)
        {
            var count = 0;
            while (target.childCount > count)
            {
                var child = target.GetChild(count).gameObject;
                if (!predicate.Invoke(child))
                {
                    count++;
                }
                else
                {
                    GameObject.DestroyImmediate(child);
                }
            }
        }

        public static void Shuffle<T> (T[] array)
        {
            var n = array.Length;
            while (n > 1)
            {
                var k = UnityEngine.Random.Range(0, n--);
                (array[k], array[n]) = (array[n], array[k]);
            }
        }

        public static void Shuffle<T> (List<T> array)
        {
            var n = array.Count;
            while (n > 1)
            {
                var k = UnityEngine.Random.Range(0, n--);
                (array[k], array[n]) = (array[n], array[k]);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Color ColourAlpha(Color input, float alpha)
        {
            return new Color(input.r, input.g, input.b, alpha);
        }

        public static List<string> SplitTrim(string input, string separator, int count)
        {
            return input.Split(separator, count, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList();
        }

        public static List<string> SplitTrim(string input, string separator)
        {
            return input.Split(separator, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList();
        }

        public static string StringColour(Color input)
        {
            return "#" + ColorUtility.ToHtmlStringRGB(input);
        }

        public static string ColouredText(Color colour, string input)
        {
            return $"<color={StringColour(colour)}>{input}</color>";
        }

        public static string ColouredText(Color colour, int input)
        {
            return $"<color={StringColour(colour)}>{input}</color>";
        }

        public static string ColouredText(Color colour, float input)
        {
            return $"<color={StringColour(colour)}>{input}</color>";
        }

        public static string FromToColours(float from, float to, Color fromColour, Color toColour)
        {
            return $"{ColouredText(fromColour, from)} -> <b>{ColouredText(toColour, to)}</b>";
        }

        public static float CalculateBarrelAngleTo(Transform transform, Vector3 worldPosition, Vector3 barrelWorldPosition)
        {
            var localToBarrel = transform.InverseTransformVector(worldPosition - barrelWorldPosition);
            var normalised = localToBarrel.normalized;
            return -Mathf.Atan2(normalised.z, normalised.x) + Mathf.PI * 0.5f;
        }
        #endregion
    }
}