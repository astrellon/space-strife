using System;
using System.Collections.Generic;
using UnityEngine;

#nullable enable

namespace Orbits
{
    public class PreLevelCharacterSelector : MonoBehaviour
    {
        #region Fields
        public CharacterSelect SelectorPrefab;
        private readonly Dictionary<string, CharacterSelect> selectors = new();
        #endregion

        #region Unity Methods
        private void Start()
        {
            UIManager.Instance.OnStateChange += this.OnStateChange;
        }
        #endregion

        #region Methods
        private void OnStateChange(InterfaceState newState, InterfaceState prevState)
        {
            if (newState == InterfaceState.SelectedLevel)
            {
                this.gameObject.SetActive(true);
                this.ShowCharacters();
            }
            else
            {
                this.HideCharacters();
            }
        }

        private void ShowCharacters()
        {
            this.HideCharacters();

            var levelInfo = PlayerStateLevelInfo.CreateFromCurrent(out var level);
            if (!levelInfo.CanStartLevel || (levelInfo.FirstTimePlaying && level != null && level.FirstTimeCharacterIds.Count >= level.NumCharacters))
            {
                return;
            }

            var showOffset = 0.0f;
            foreach (var gameChar in PlayerState.Instance.OrderedUnlockedCharacters)
            {
                if (!this.selectors.TryGetValue(gameChar.CharacterId, out var selector))
                {
                    selector = Instantiate(this.SelectorPrefab, this.transform);
                    selector.Character = gameChar;
                    this.selectors[gameChar.CharacterId] = selector;
                }

                var rect = selector.PortraitRect;
                UICharacterPortraitManager.Instance.ShowCharacter(gameChar.Id, rect, rect.position);

                selector.ShowOffset = showOffset;
                selector.SetShow();

                showOffset += 0.1f;
            }
        }

        private void HideCharacters()
        {
            foreach (var selector in this.selectors.Values)
            {
                selector.Show = false;
                UICharacterPortraitManager.Instance.MarkOffScreenIfFollowingTarget(selector.Character, selector.PortraitRect);
            }
        }
        #endregion
    }
}