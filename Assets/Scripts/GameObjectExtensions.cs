using System;
using System.Linq;
using UnityEngine;

#nullable enable

namespace Orbits
{
    public static class GameObjectExtensions
    {
        #region Methods
        public static T GetOrAddComponent<T>(this GameObject parent) where T : Component
        {
            if (parent.TryGetComponent<T>(out var result))
            {
                return result;
            }

            result = parent.AddComponent<T>();
            return result;
        }
        #endregion
    }
}