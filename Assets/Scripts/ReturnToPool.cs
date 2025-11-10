using System.Collections;
using UnityEngine;

#nullable enable

namespace Orbits
{
    public class ReturnToPool : MonoBehaviour
    {
        #region Fields
        public ParticleSystem? TakeDurationFrom;
        public GameObjectPool? Pool;
        public float Duration;
        private int counter;
        #endregion

        #region Unity Methods
        IEnumerator Wait(int count)
        {
            yield return new WaitForSeconds(this.Duration);
            if (count == this.counter)
            {
                this.Pool.Release(this.gameObject);
            }
            else
            {
                Debug.Log($"Count didn't match");
            }
        }

        void OnEnable()
        {
            this.HandleStart();
        }
        #endregion

        #region Methods
        public void Init(GameObjectPool pool)
        {
            this.Pool = pool;
            this.HandleStart();
        }

        private void HandleStart()
        {
            if (this.Pool == null)
            {
                return;
            }

            if (this.Duration <= 0.0f && this.TakeDurationFrom != null)
            {
                this.Duration = this.TakeDurationFrom.main.duration;
            }

            this.counter++;
            StartCoroutine(this.Wait(this.counter));
        }
        #endregion
    }
}