using System;
using UnityEngine;

#nullable enable

namespace Orbits
{
    public class AbilityList : MonoBehaviour
    {
        #region Fields
        public AbilityButton ButtonPrefab;
        #endregion

        #region Unity Methods
        private void Start()
        {
            GameManager.Instance.OnLevelLoad += this.OnLevelLoad;
            var level = GameManager.Instance.CurrentLevel;
            if (level != null)
            {
                this.OnLevelLoad(level);
            }
        }

        private void OnLevelLoad(Level level)
        {
            Utils.RemoveAllChildren(this.transform);
            foreach (var ability in level.ActiveAbilities)
            {
                var button = Instantiate(this.ButtonPrefab, this.transform);
                button.Ability = ability;
            }
        }
        #endregion

        #region Methods
        #endregion
    }
}