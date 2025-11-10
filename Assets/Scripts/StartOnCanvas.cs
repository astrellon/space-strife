using UnityEngine;

#nullable enable

namespace Orbits
{
    public class StartOnCanvas : MonoBehaviour
    {
        #region Unity Methods
        private void OnEnable()
        {
            if (this.transform.parent == null)
            {
                this.transform.SetParent(GameManager.Instance.MainCanvas.transform, false);
            }
        }
        #endregion
    }
}