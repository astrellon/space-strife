using System;
using System.Collections.Generic;
using System.Linq;

namespace Orbits.Extensions
{
        public static class ListExtensions
    {
        // Adapted from https://stackoverflow.com/a/22801345
        public static void AddSorted<T>(this List<T> list, T item) where T : IComparable<T>
        {
            if (list.Count == 0)
            {
                list.Add(item);
                return;
            }

            if (list[list.Count - 1].CompareTo(item) <= 0)
            {
                list.Add(item);
                return;
            }

            if (list[0].CompareTo(item) >= 0)
            {
                list.Insert(0, item);
                return;
            }

            var index = list.BinarySearch(item);
            if (index < 0)
            {
                index = ~index;
            }

            list.Insert(index, item);
        }

        public static void AddSorted<T>(this List<T> list, T item, IComparer<T> comparer)
        {
            if (list.Count == 0)
            {
                list.Add(item);
                return;
            }

            if (comparer.Compare(list[list.Count - 1], item) <= 0)
            {
                list.Add(item);
                return;
            }

            if (comparer.Compare(list[0], item) >= 0)
            {
                list.Insert(0, item);
                return;
            }

            var index = list.BinarySearch(item, comparer);
            if (index < 0)
            {
                index = ~index;
            }

            list.Insert(index, item);
        }

        public static void AddDistinctRange<T>(this List<T> list, IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                if (!list.Contains(item))
                {
                    list.Add(item);
                }
            }
        }

        public static int AddDistinctRange<T>(this List<T> list, IEnumerable<T> items, int distinctCount)
        {
            var added = 0;
            foreach (var item in items)
            {
                if (!list.Contains(item))
                {
                    list.Add(item);
                    added++;
                    distinctCount--;
                }

                if (distinctCount <= 0)
                {
                    break;
                }
            }

            return added;
        }

        public static bool AddDistinct<T>(this List<T> list, T item)
        {
            if (!list.Contains(item))
            {
                list.Add(item);
                return true;
            }

            return false;
        }

        public static void RemoveAtSwapBack<T>(this List<T> list, T item)
        {
            for (var i = 0; i < list.Count; i++)
            {
                if (list[i].Equals(item))
                {
                    list.RemoveAtSwapBack(i);
                    break;
                }
            }
        }

        public static void RemoveAtSwapBack<T>(this List<T> list, int index)
        {
            list[index] = list[list.Count - 1];
            list.RemoveAt(list.Count - 1);
        }

        public static int FindIndex<T>(this List<T> list, T item)
        {
            for (var i = 0; i < list.Count; i++)
            {
                if (list[i].Equals(item))
                {
                    return i;
                }
            }

            return -1;
        }

        public static void ToggleValue<T>(this List<T> list, T item)
        {
            var index = list.FindIndex(item);
            if (index < 0)
            {
                list.Add(item);
            }
            else
            {
                list.RemoveAt(index);
            }
        }
    }
}