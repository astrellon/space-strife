using UnityEngine;

#nullable enable

namespace Orbits
{
    public class GravitySource : MonoBehaviour
    {
        #region Fields
        public float OuterRange = 10.0f;
        public float InnerRange = 6.0f;
        public float Strength = 1.0f;
        #endregion

        #region Unity Methods
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(this.transform.position, this.OuterRange);

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(this.transform.position, this.InnerRange);
        }
        #endregion

        #region Methods
        public static Vector3 CalculateGravity(Vector3 gravityCenter, Vector3 target, float range)
        {
            var toTarget = gravityCenter - target; // Negative so that it points to the gravity source
            var distance = toTarget.magnitude;
            if (distance > range)
            {
                return Vector3.zero;
            }

            var toTargetNormalised = toTarget.normalized;
            if (distance <= 1.0f)
            {
                return toTargetNormalised;
            }

            var targetDiff = distance;
            return Mathf.Clamp01(1.0f / Mathf.Pow(targetDiff, 2.0f)) * toTargetNormalised;
            // return Mathf.Lerp(1.0f, 0.0f, targetDiff / range) * toTargetNormalised;
        }
        #endregion
    }
}