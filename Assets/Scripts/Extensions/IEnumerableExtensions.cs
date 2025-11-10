using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System;

namespace Orbits.Extensions
{
    public static class IEnumerableExtensions
    {
        #region Methods
        public static IReadOnlyList<T> ToReadOnlyList<T>(this IEnumerable<T> input)
        {
            return input.ToList();
        }

        public static IEnumerable<TCast> TryCast<TCast>(this IEnumerable input)
        {
            foreach (var item in input)
            {
                if (item is TCast casted)
                {
                    yield return casted;
                }
            }
        }

        public static void ForEach<T>(this IEnumerable<T> input, Action<T> callback)
        {
            foreach (var item in input)
            {
                callback(item);
            }
        }
        #endregion
    }
}