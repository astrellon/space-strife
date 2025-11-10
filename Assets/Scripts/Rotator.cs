using UnityEngine;

#nullable enable

namespace Orbits
{
    public class Rotator : MonoBehaviour
    {
        #region Fields
        public float Speed = 10.0f;
        public Vector3 UpAxis = Vector3.up;
        public Space RelativeTo = Space.World;
        #endregion

        #region Unity Methods
        private void Update()
        {
            if (Time.deltaTime == 0.0f)
            {
                return;
            }
            this.transform.Rotate(this.UpAxis, this.Speed * Time.deltaTime, this.RelativeTo);
        }
        #endregion
    }
}