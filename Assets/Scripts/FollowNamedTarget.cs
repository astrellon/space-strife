using UnityEngine;

#nullable enable

namespace Orbits
{
    public class FollowNamedTarget : MonoBehaviour
    {
        #region Fields
        public string AttachToName;
        private Transform? target;
        // private Quaternion originalTargetRotation;
        // private Quaternion originalTargetRotationInv;
        #endregion

        #region Unity Methods
        private void Start()
        {
            var inLevel = this.GetComponentInParent<LevelContainer>();
            if (inLevel == null)
            {
                Debug.LogWarning("FollowNamedTarget not inside of a level container");
            }
            else
            {
                if (inLevel.TryGetNamedTransform(this.AttachToName, out var target))
                {
                    this.target = target;
                }
                else
                {
                    this.enabled = false;
                }
            }
        }

        private void LateUpdate()
        {
            if (this.target != null)
            {
                this.transform.SetPositionAndRotation(this.target.position, this.target.rotation);
            }
        }
        #endregion

        #region Methods
        #endregion
    }
}