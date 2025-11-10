using UnityEngine;

#nullable enable

namespace Orbits
{
    public class SelectableGameObject : MonoBehaviour
    {
        #region Fields
        public Vector3 ButtonWorldOffset = new(0, 1, 5);
        public Vector3 FocusWorldOffset = new(0, 50, 0);
        #endregion

        #region Unity Methods
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(this.transform.position + this.ButtonWorldOffset, 1.0f);

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(this.transform.position + this.FocusWorldOffset, 1.0f);
        }
        #endregion

        #region Methods
        public void ApplyButtonOffset(RectTransform? rect)
        {
            if (rect != null)
            {
                var screenPosition = this.CalculateButtonOffset();
                rect.anchorMax = screenPosition;
                rect.anchorMin = screenPosition;
            }
        }

        public Vector3 CalculateButtonOffset()
        {
            var position = this.transform.position + this.ButtonWorldOffset;
            return Camera.main.WorldToViewportPoint(position);
        }

        public Vector3 GetFocusOffset()
        {
            return this.transform.position + this.FocusWorldOffset;
        }
        #endregion
    }
}