using UnityEngine;

#nullable enable

namespace Orbits
{
    public class ResetRigidbody : MonoBehaviour, IReset
    {
        #region Fields
        public Rigidbody Rigidbody;

        private Vector3 startingPosition;
        private Quaternion startingRotation;
        private float startingDrag;
        #endregion

        #region Unity Methods
        private void Start()
        {
            this.startingPosition = this.Rigidbody.position;
            this.startingRotation = this.Rigidbody.rotation;
            this.startingDrag = this.Rigidbody.drag;
        }
        #endregion

        #region Methods
        public void Reset()
        {
            this.Rigidbody.position = this.startingPosition;
            this.Rigidbody.rotation = this.startingRotation;
            this.Rigidbody.velocity = Vector3.zero;
            this.Rigidbody.angularVelocity = Vector3.zero;
            this.Rigidbody.drag = this.startingDrag;
        }
        #endregion
    }
}