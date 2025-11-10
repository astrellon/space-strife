using UnityEngine;

#nullable enable

namespace Orbits
{
    public class LightningSpawner : MonoBehaviour
    {
        #region Fields
        public LightningPathComp? Prefab;
        public AudioOneOffs? AudioClips;
        public string PlayerStateFlag = "";

        public float InnerRadius = 0.0f;

        public float Distance = 30.0f;
        #endregion

        #region Methods
        public LightningPathComp? SpawnLightning()
        {
            if (this.Prefab == null)
            {
                return null;
            }

            if (this.AudioClips != null)
            {
                AudioManager.Instance.PlayOneOff(this.AudioClips, AudioManager.Instance.GlobalSFXPrefab, this.transform.position);
            }

            var spawned = GameObjectPools.Instance.Spawn(this.Prefab);
            spawned.transform.position = this.transform.position;
            spawned.GameObjectPrefab = this.Prefab;

            var angle = UnityEngine.Random.Range(0, 360);
            var dirX = Mathf.Cos(angle);
            var dirZ = Mathf.Sin(angle);
            var innerPositionX = dirX * this.InnerRadius;
            var innerPositionZ = dirZ * this.InnerRadius;
            var outerPositionX = dirX * this.Distance;
            var outerPositionZ = dirZ * this.Distance;

            spawned.From.localPosition = new Vector3(innerPositionX, 0, innerPositionZ);
            spawned.To.localPosition = new Vector3(outerPositionX, 0, outerPositionZ);
            spawned.enabled = true;

            if (!string.IsNullOrWhiteSpace(this.PlayerStateFlag))
            {
                PlayerState.Instance.SetGameFlag(this.PlayerStateFlag, true);
            }

            return spawned;
        }

        void OnDrawGizmos()
        {
            // Gizmos.matrix = this.transform.localToWorldMatrix;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(this.transform.position, this.InnerRadius);

            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(this.transform.position, this.Distance);
        }
        #endregion
    }
}