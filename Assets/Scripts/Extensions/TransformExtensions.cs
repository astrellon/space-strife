using System;
using UnityEngine;

namespace Orbits.Extensions
{
    // Taken from: https://stackoverflow.com/a/68291679
    public static class TransformExtensions
    {
        public static Transform FindRecursive(this Transform self, string exactName) => self.FindRecursive(child => child.name == exactName);

        public static Transform FindRecursive(this Transform self, Func<Transform, bool> selector)
        {
            foreach (Transform child in self)
            {
                if (selector(child))
                {
                    return child;
                }

                var finding = child.FindRecursive(selector);

                if (finding != null)
                {
                    return finding;
                }
            }

            return null;
        }

        public static bool TryGetComponentInParent<T>(this Transform self, out T result)
        {
            result = self.GetComponentInParent<T>();
            return result != null;
        }

        public static bool TryGetComponentInChildren<T>(this Transform self, out T result)
        {
            result = self.GetComponentInChildren<T>();
            return result != null;
        }
    }
}