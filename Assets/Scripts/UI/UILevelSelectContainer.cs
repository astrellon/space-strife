using System;
using System.Collections.Generic;
using UnityEngine;

#nullable enable

namespace Orbits
{
    public class UILevelSelectContainer : MonoBehaviour
    {
        #region Fields
        public UILevelSelect2 SelectPrefab;
        public UILevelSelectName NamePrefab;
        public RectTransform LevelNamePosition;
        private Dictionary<LevelContainer, UILevelSelect2> currentSelects = new();
        private Dictionary<LevelContainer, UILevelSelectName> currentNames = new();
        #endregion

        #region Unity Methods
        private void Start()
        {
            UIManager.Instance.OnStateChange += this.OnStateChange;
        }
        #endregion

        #region Methods
        public void Remove(UILevelSelect2 item)
        {
            this.currentSelects.Remove(item.LevelContainer);
        }

        public void Remove(UILevelSelectName item)
        {
            this.currentNames.Remove(item.LevelContainer);
        }

        private void OnStateChange(InterfaceState newState, InterfaceState prevState)
        {
            var starSystem = UIManager.Instance.CurrentStarSystem;
            if (starSystem == null)
            {
                return;
            }

            foreach (var levelContainer in starSystem.Levels)
            {
                if (!this.currentSelects.ContainsKey(levelContainer))
                {
                    var select = GameObjectPools.Instance.Spawn(this.SelectPrefab);
                    select.transform.SetParent(this.transform);
                    select.Init(this, levelContainer, starSystem);
                    this.currentSelects[levelContainer] = select;
                }

                if (!this.currentNames.ContainsKey(levelContainer))
                {
                    var name = GameObjectPools.Instance.Spawn(this.NamePrefab);
                    name.transform.SetParent(this.transform);
                    name.ForWorldTarget.UIPosition = this.LevelNamePosition;
                    name.Init(this, levelContainer, starSystem);
                    this.currentNames[levelContainer] = name;
                }
            }
        }
        #endregion
    }
}