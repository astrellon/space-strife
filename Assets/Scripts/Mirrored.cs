using UnityEngine;

#nullable enable

namespace Orbits
{
    public class Mirrored : MonoBehaviour
    {
        #region Fields
        public GameObject Left;
        public Renderer LeftMaterial;
        public GameObject Right;
        public Renderer RightMaterial;

        public float RotateAngle;
        public float RotateSpeed = 10.0f;
        #endregion

        #region Unity Methods
        private void Update()
        {
            this.RotateAngle = (this.RotateAngle + this.RotateSpeed * Time.deltaTime) % 360.0f;

            var pos = this.transform.position;
            var leftNormal = this.transform.right;
            var rightNormal = -leftNormal;
            this.LeftMaterial.material.SetVector("_MirrorPosition", pos);
            this.LeftMaterial.material.SetVector("_MirrorNormal", leftNormal);

            this.RightMaterial.material.SetVector("_MirrorPosition", pos);
            this.RightMaterial.material.SetVector("_MirrorNormal", rightNormal);

            this.Left.transform.localRotation = Quaternion.Euler(0, this.RotateAngle, 0);
            this.Right.transform.localRotation = Quaternion.Euler(0, -this.RotateAngle, 0);
        }
        #endregion
    }
}