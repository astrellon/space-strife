using UnityEngine;
using UnityEngine.Pool;

namespace Orbits
{
    public class AudioSourcePool
    {
        #region Fields
        public readonly AudioSource Prefab;
        private readonly IObjectPool<AudioSource> pool;
        #endregion

        #region Constructor
        public AudioSourcePool(AudioSource prefab, int defaultCapacity = 100, int maxCapacity = 3000)
        {
            this.Prefab = prefab;
            this.pool = new ObjectPool<AudioSource>(this.CreatePooledItem, this.OnTakeFromPool, this.OnReturnedToPool, this.OnDestroyPoolObject, false, defaultCapacity, maxSize: maxCapacity);
        }
        #endregion

        #region Methods
        public AudioSource Get()
        {
            return this.pool.Get();
        }

        public void Release(AudioSource obj)
        {
            this.pool.Release(obj);
        }

        private AudioSource CreatePooledItem()
        {
            var result = GameObject.Instantiate(this.Prefab);

            return result;
        }

        private void OnTakeFromPool(AudioSource obj)
        {
            obj.gameObject.SetActive(true);
        }

        private void OnReturnedToPool(AudioSource obj)
        {
            obj.gameObject.SetActive(false);
        }

        private void OnDestroyPoolObject(AudioSource obj)
        {
            GameObject.Destroy(obj);
        }
        #endregion
    }
}