using UnityEngine;

#nullable enable

namespace Orbits
{
    public class ResetAnimation : MonoBehaviour
    {
        #region Fields
        public Animator Animator;
        public string StateName;
        #endregion

        #region Unity Methods
        private void OnDisable()
        {
            this.Animator.Play(this.StateName, 0, 0.0f);
        }
        #endregion

        #region Methods
        #endregion
    }
}