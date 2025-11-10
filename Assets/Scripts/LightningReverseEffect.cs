using UnityEngine;

#nullable enable

namespace Orbits
{
    public class LightningReverseEffect : MonoBehaviour
    {
        #region Fields
        public static LightningReverseEffect Instance;

        public LightningSpawner Spawner;
        public float SpawnCountdown = 3.0f;
        public int SpawnCount = 6;

        public bool Show;
        private int spawnCount = 0;
        private float spawnCountdown = 0.0f;
        #endregion

        #region Unity Methods
        private void Awake()
        {
            Instance = this;
        }

        private void Update()
        {
            if (!this.Show)
            {
                this.gameObject.SetActive(false);
                return;
            }

            var dt = Time.deltaTime * GameManager.Instance.PlayerDeltaTimeScale;
            this.spawnCountdown -= dt;
            if (this.spawnCountdown < 0.0f)
            {
                this.spawnCount++;
                this.SpawnLightning();
                this.spawnCountdown = this.SpawnCountdown;
            }

            if (this.spawnCount >= this.SpawnCount)
            {
                this.Show = false;
            }
        }
        #endregion

        #region Methods
        public void SetShow(bool value)
        {
            if (value == this.Show)
            {
                return;
            }

            this.Show = value;
            this.gameObject.SetActive(true);

            this.spawnCount = 0;
            this.spawnCountdown = 0.0f;
        }

        private void SpawnLightning()
        {
            var spawned = this.Spawner.SpawnLightning();
            if (spawned != null)
            {
                spawned.DamageRange = Vector2.zero;
            }
        }
        #endregion
    }
}