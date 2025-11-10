using System.Collections;
using UnityEngine;

#nullable enable

namespace Orbits
{
    public class FollowingProjectileEffect : MonoBehaviour
    {
        #region Fields
        public GameObject Prefab;
        public float RemoveTimeout = 0.3f;
        public bool FollowRotation = false;
        public TrailRenderer? TrailRenderer;
        public ParticleSystem? ParticleSystem;
        public int Following = -1;
        #endregion

        #region Methods
        public void TriggerSpawned()
        {
            if (this.ParticleSystem != null)
            {
                this.ParticleSystem.Clear(true);
                this.ParticleSystem.Play(true);
            }
        }

        public void DoRender()
        {
            if (this.ParticleSystem != null && !this.ParticleSystem.isPlaying)
            {
                this.ParticleSystem.Play(true);
            }
        }

        public void TriggerRemove()
        {
            if (this.ParticleSystem != null)
            {
                this.ParticleSystem.Stop(true);
            }

            if (!this.gameObject.activeInHierarchy)
            {
                GameObjectPools.Instance.Release(this.Prefab, this.gameObject);
            }
            else
            {
                StartCoroutine(this.DoRemove());
            }
        }

        private IEnumerator DoRemove()
        {
            yield return new WaitForSeconds(this.RemoveTimeout);
            GameObjectPools.Instance.Release(this.Prefab, this.gameObject);

            if (this.TrailRenderer != null)
            {
                this.TrailRenderer.Clear();
            }
        }
        #endregion
    }
}