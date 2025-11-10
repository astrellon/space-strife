using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

#nullable enable

namespace Orbits
{
    public class UIStartLevelButton : MonoBehaviour
    {
        #region Fields
        public Button Button;
        #endregion

        #region Methods
        private void OnEnable()
        {
            this.OnCharacterSelected();
            UIManager.Instance.OnCharacterSelected += this.OnCharacterSelected;
        }
        private void OnDisable()
        {
            UIManager.Instance.OnCharacterSelected -= this.OnCharacterSelected;
        }

        private void OnCharacterSelected()
        {
            var enabled = false;
            var level = UIManager.Instance.CurrentLevelSelected;
            var starSystem = UIManager.Instance.CurrentStarSystem;
            var countOffset = 0;
            if (starSystem != null && level != null && level.LevelPrefab != null)
            {
                if (PlayerState.Instance.IsFirstTimePlayingLevel(starSystem.Id, level.LevelPrefab.Id))
                {
                    countOffset = level.LevelPrefab.FirstTimeCharacterIds.Count;
                }

                var numChars = level.LevelPrefab.NumCharacters;
                if (numChars == 0)
                {
                    enabled = UIManager.Instance.CurrentCharactersSelected.Count > 0;
                }
                else if (UIManager.Instance.CurrentCharactersSelected.Count == numChars - countOffset)
                {
                    enabled = true;
                }
            }

            this.Button.interactable = enabled;
        }

        public void StartLevel()
        {
            var starSystem = UIManager.Instance.CurrentStarSystem;
            var level = UIManager.Instance.CurrentLevelSelected.LevelPrefab;
            if (starSystem == null)
            {
                Debug.LogWarning("Cannot start level, current star system is null");
                return;
            }
            if (level == null)
            {
                Debug.LogWarning("Cannot start level, current level is null");
                return;
            }

            var characters = new List<GameCharacter>(1);
            if (PlayerState.Instance.IsFirstTimePlayingLevel(starSystem.Id, level.Id) &&
                level.TryGetStartingCharacters(out var startingCharacters))
            {
                characters.AddRange(startingCharacters);
            }
            characters.AddRange(UIManager.Instance.CurrentCharactersSelected);

            GameManager.Instance.StartLevel(starSystem.Id, level.Id, characters, isFirstInit: true);
        }
        #endregion
    }
}