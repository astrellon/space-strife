using System.Collections.Generic;
using UnityEngine;

namespace Orbits
{
    public class GameObjectPools : MonoBehaviour
    {
        #region Fields
        public static GameObjectPools Instance;

        public int DefaultCapacity = 100;
        public int MaxCapacity = 3000;

        private readonly Dictionary<int, GameObjectPool> pools = new ();

        public IReadOnlyCollection<GameObjectPool> Pools => this.pools.Values;
        #endregion

        #region Unity Methods
        void Awake()
        {
            Instance = this;
        }
        #endregion

        #region Methods
        public void Release(GameObject prefab, GameObject value)
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

        public GameObject Spawn(GameObject prefab)
        {
            if (!this.pools.TryGetValue(prefab.GetInstanceID(), out var pool))
            {
                pool = new GameObjectPool(prefab, this.DefaultCapacity, this.MaxCapacity);
                this.pools[prefab.GetInstanceID()] = pool;
            }

            return pool.Get();
        }

        public T Spawn<T>(T prefab) where T : Component
        {
            var spawned = this.Spawn(prefab.gameObject);
            return spawned.GetComponent<T>();
        }
        #endregion
    }
}