using UnityEngine;

#nullable enable

namespace Orbits
{
    public class RemoveComponentOnDisable : MonoBehaviour
    {
        #region Fields
        public Component? ToRemove;
        #endregion

        #region Unity Methods
        private void OnDisable()
        {
            if (this.ToRemove != null)
            {
                // Debug.Log($"Removing component: {this.ToRemove.name} ({this.ToRemove.GetType().FullName})");
                Destroy(this.ToRemove);
                this.ToRemove = null;
            }

            Destroy(this);
        }
        #endregion
    }
}