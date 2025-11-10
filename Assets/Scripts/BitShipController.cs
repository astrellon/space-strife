using UnityEngine;

#nullable enable

namespace Orbits
{
    public class BitShipController : MonoBehaviour
    {
        #region Fields
        public Target Target;
        public Ship Ship;
        public float Angle;
        public float Distance;
        public Transform Parent;
        public Renderer? HealthBar;
        public GameObject SpawnPrefab;
        public Rotator Rotator;

        public float InitialAngleOffset;
        #endregion

        #region Methods
        public void Init(Transform parent, float angleOffset, float distance)
        {
            this.Parent = parent;
            this.InitialAngleOffset = angleOffset;
            this.Angle = angleOffset;
            this.Distance = distance;

            this.UpdateBit(0.0f);

            this.Rotator.Speed = (Random.value * 4.0f + 8.0f) * (Random.value > 0.5f ? 1.0f : -1.0f);

            var spawned = GameObjectPools.Instance.Spawn(this.SpawnPrefab);
            spawned.transform.position = this.transform.position;
        }

        public void UpdateAngle(float angle)
        {
            this.Angle = angle + this.InitialAngleOffset;
        }

        public void UpdateBit(float dt)
        {
            var xPos = Mathf.Cos(this.Angle) * this.Distance;
            var zPos = Mathf.Sin(this.Angle) * this.Distance;
            this.transform.position = this.Parent.position + new Vector3(xPos, 0, zPos);

            this.Target.ManagedUpdate(dt);
            this.Target.SetHealthBar(this.HealthBar);
        }
        #endregion
    }
}