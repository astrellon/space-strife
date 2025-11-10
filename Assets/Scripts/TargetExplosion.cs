using UnityEngine;

#nullable enable

namespace Orbits
{
    public class TargetExplosion : MonoBehaviour
    {
        #region Fields
        public bool FollowTarget;
        public Target? Target;
        #endregion

        #region Unity Methods
        private void Update()
        {
            if (this.FollowTarget && this.Target != null)
            {
                this.transform.position = this.Target.transform.position;
            }
        }
        #endregion
    }
}