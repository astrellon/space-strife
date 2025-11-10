using System;
using UnityEngine;

#nullable enable

namespace Orbits
{
    public class HideIfNotAvailable : MonoBehaviour
    {
        #region Fields
        public string StarSystemId = "";
        public string LevelId = "";

        private StarSystemId starSystemId;
        private LevelId levelId;

        public bool ShouldHide
        {
            get
            {
                var state = UIManager.Instance.State;
                if (state == InterfaceState.LevelSelect ||
                    state == InterfaceState.PreLevelStart ||
                    state == InterfaceState.SelectedLevel)
                {
                    return !PlayerState.Instance.IsLevelUnlocked(this.starSystemId, this.levelId);
                }
                return !GameManager.Instance.IsCurrentlyPlayingLevel(this.starSystemId, this.levelId);
            }
        }
        #endregion

        #region Unity Methods
        private void OnEnable()
        {
            this.starSystemId = new(this.StarSystemId);
            this.levelId = new(this.LevelId);
        }
        #endregion
    }
}