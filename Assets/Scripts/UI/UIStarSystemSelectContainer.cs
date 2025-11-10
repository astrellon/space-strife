using UnityEngine;

#nullable enable

namespace Orbits
{
    public class UIStarSystemSelectContainer : MonoBehaviour
    {
        #region Fields
        public UIStarSystemSelect SelectPrefab;
        public UIStarSystemName NamePrefab;
        #endregion

        #region Unity Methods
        private void Start()
        {
            foreach (var starSystem in GameManager.Instance.StarSystems)
            {
                var select = Instantiate(this.SelectPrefab, this.transform);
                select.StarSystem = starSystem;

                var name = Instantiate(this.NamePrefab, this.transform);
                name.StarSystem = starSystem;
            }
        }
        #endregion
    }
}