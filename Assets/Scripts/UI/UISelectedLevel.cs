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
            var canStartLevel = this.CanShowStartLevel();
            foreach (var obj in this.EnabledForStartLevel)
            {
                obj.SetActive(canStartLevel);
            }
        }
        #endregion

        #region Methods
        public bool CanShowStartLevel()
        {
            var starSystem = UIManager.Instance.CurrentStarSystem;
            var level = UIManager.Instance.CurrentLevelSelected.LevelPrefab;
            if (starSystem == null || level == null)
            {
                return false;
            }

            if (!PlayerState.Instance.TryGetLevelTotal(starSystem.Id, level.Id, out _))
            {
                return true;
            }

#if UNITY_EDITOR
            Debug.LogError($"REMEMBER TO REMOVE");
            return true;
#else
            return PlayerState.Instance.LevelRepeatUnlocked;
#endif
        }
        #endregion
    }
}