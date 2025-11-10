using System.Collections.Generic;
using UnityEngine;

#nullable enable

namespace Orbits
{
    [DefaultExecutionOrder(2)]
    public class AlienShipSpawn : MonoBehaviour
    {
        #region Fields
        public List<AlienShipSpawnInfo> SpawnInfo = new();
        #endregion

        #region Unity Methods
        private void OnEnable()
        {
            AlienShip.Instance.SetSpawnInfo(this.SpawnInfo);
        }
        #endregion
    }
}