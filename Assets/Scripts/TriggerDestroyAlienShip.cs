using UnityEngine;

#nullable enable

namespace Orbits
{
    public class TriggerDestroyAlienShip : MonoBehaviour
    {
        #region Fields
        public float ExplosionRadius;
        public float ExplosionTimer;
        public bool DisableAlienShip;
        public bool EnableSmallExplosions;
        public GameObject SmallExplosionPrefab;
        public float SmallExplosionScale = 2.0f;

        private float explosionCountdown;
        #endregion

        #region Unity Methods
        private void Update()
        {
            if (AlienShip.Instance != null && this.DisableAlienShip)
            {
                AlienShip.Instance.gameObject.SetActive(false);
                return;
            }

            if (this.EnableSmallExplosions)
            {
                this.explosionCountdown -= Time.deltaTime;
                if (this.explosionCountdown < 0)
                {
                    this.explosionCountdown += this.ExplosionTimer + Random.value * 0.15f;
                    this.SpawnSmallExplosion();
                }
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(this.transform.position, this.ExplosionRadius);
        }
        #endregion

        #region Methods
        private void SpawnSmallExplosion()
        {
            var angle = Random.value * Mathf.PI * 2.0f;
            var ypos = Random.value * 0.5f + 0.5f;

            var xpos = Mathf.Cos(angle) * this.ExplosionRadius;
            var zpos = Mathf.Sin(angle) * this.ExplosionRadius;

            var spawned = GameObjectPools.Instance.Spawn(this.SmallExplosionPrefab);
            spawned.transform.position = this.transform.position + new Vector3(xpos, ypos, zpos);

            var scale = Random.value * this.SmallExplosionScale;
            spawned.transform.localScale = Vector3.one * scale;
        }
        #endregion
    }
}