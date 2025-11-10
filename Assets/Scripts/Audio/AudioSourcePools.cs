using System.Collections.Generic;
using UnityEngine;

namespace Orbits
{
    public class AudioSourcePools : MonoBehaviour
    {
        #region Fields
        public static AudioSourcePools Instance;

        public int DefaultCapacity = 10;
        public int MaxCapacity = 32;

        private readonly Dictionary<int, AudioSourcePool> pools = new();
        #endregion

        #region Unity Methods
        void Awake()
        {
            Instance = this;
        }
        #endregion

        #region Methods
        public void Release(AudioSource prefab, AudioSource value)
        {
            if (this.pools.TryGetValue(prefab.GetInstanceID(), out var pool))
            {
                pool.Release(value);
            }
            else
            {
                Debug.LogWarning($"Unable to release pool for prefab: [{prefab.GetInstanceID()}] {prefab.name}");
            }
        }

        public AudioSource Spawn(AudioSource prefab)
        {
            if (!this.pools.TryGetValue(prefab.GetInstanceID(), out var pool))
            {
                pool = new AudioSourcePool(prefab, this.DefaultCapacity, this.MaxCapacity);
                this.pools[prefab.GetInstanceID()] = pool;
            }

            return pool.Get();
        }
        #endregion
    }
}