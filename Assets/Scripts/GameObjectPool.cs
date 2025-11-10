using UnityEngine;
using UnityEngine.Pool;

namespace Orbits
{
    public class GameObjectPool
    {
        #region Fields
        public readonly GameObject Prefab;
        private readonly IObjectPool<GameObject> pool;
        private readonly bool shouldReturn;

        public int CountActive { get; private set; }
        public int Count { get; private set; }
        #endregion

        #region Constructor
        public GameObjectPool(GameObject prefab, int defaultCapacity = 100, int maxCapacity = 3000)
        {
            this.Prefab = prefab;
            if (prefab.TryGetComponent<ParticleSystem>(out _) || prefab.TryGetComponent<ReturnToPool>(out _))
            {
                if (!prefab.TryGetComponent<FollowingProjectileEffect>(out _))
                {
                    this.shouldReturn = true;
                }
            }
            this.pool = new ObjectPool<GameObject>(this.CreatePooledItem, this.OnTakeFromPool, this.OnReturnedToPool, this.OnDestroyPoolObject, false, defaultCapacity, maxSize: maxCapacity);
        }
        #endregion

        #region Methods
        public GameObject Get()
        {
            return this.pool.Get();
        }

        // Do we need a way of checking for if a pooled object is destroyed?
        public void Release(GameObject obj)
        {
            obj.transform.SetParent(null);
            this.pool.Release(obj);
        }

        private GameObject CreatePooledItem()
        {
            var result = GameObject.Instantiate(this.Prefab);

            this.Count++;

            if (this.shouldReturn)
            {
                var returnToPool = result.GetOrAddComponent<ReturnToPool>();
                returnToPool.Init(this);
            }

            return result;
        }

        private void OnTakeFromPool(GameObject obj)
        {
            this.CountActive++;
            obj.SetActive(true);
        }

        private void OnReturnedToPool(GameObject obj)
        {
            this.CountActive--;
            obj.SetActive(false);
        }

        private void OnDestroyPoolObject(GameObject obj)
        {
            GameObject.Destroy(obj);
        }
        #endregion
    }
}