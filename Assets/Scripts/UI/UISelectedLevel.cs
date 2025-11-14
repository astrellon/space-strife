using System.Collections.Generic;
using UnityEngine;

#nullable enable

namespace Orbits
{
    public class UISelectedLevel : MonoBehaviour
    {
        #region Fields
        public List<GameObject> EnabledForStartLevel = new();
        #endregion

        #region Unity Methods
        private void OnEnable()
        {
            var levelInfo = this.GetPlayerStateLevelInfo();
            foreach (var obj in this.EnabledForStartLevel)
            {
                obj.SetActive(levelInfo.CanStartLevel);
            }
        }
        #endregion

        #region Methods
        private PlayerStateLevelInfo GetPlayerStateLevelInfo()
        {
            var starSystem = UIManager.Instance.CurrentStarSystem;
            var level = UIManager.Instance.CurrentLevelSelected.LevelPrefab;

            return PlayerStateLevelInfo.CreateFrom(starSystem, level, PlayerState.Instance);
        }
        #endregion
    }
}