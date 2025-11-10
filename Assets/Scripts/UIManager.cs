using System.Linq;
using System.Collections.Generic;
using Orbits.Extensions;
using UnityEngine;

#nullable enable

namespace Orbits
{
    public enum InterfaceState
    {
        Unknown, MainMenu, MainMenuOptions, InGame, InGameMenu, InGameOptions, NextLevel, Equipment,
        GameOver, GameWon, MainMenuOpening, Credits, LevelSelect, StarSystemSelect, SelectedLevel,
        PreLevelStart, PostLevel
    }

    [DefaultExecutionOrder(-1)]
    public class UIManager : MonoBehaviour
    {
        #region Fields
        public delegate void ChangeStateHandler(InterfaceState newState, InterfaceState prevState);

        public static UIManager Instance { get; private set; }

        public InterfaceState State = InterfaceState.MainMenuOpening;

        public GameObject MainMenu;
        public GameObject InGameMenu;
        public GameObject InGameUI;
        public GameObject TankEquipmentUI;
        public GameObject ShowTankEquipmentUI;
        public UIOptionsMenu OptionsMenu;
        public GameObject NextLevelUI;
        public GameObject GameWonUI;
        public GameObject GameOverUI;
        public GameObject CreditsUI;
        public GameObject VersionUI;
        public GameObject LevelSelect;
        public GameObject StarSystemSelect;
        public GameObject SelectedLevel;
        public DialogueUI DialogueUI;
        public LevelVM LevelVM;
        public bool InDialogue;
        public UIFadeCanvasGroup TankSelectGroup;
        public UINextLevel NextLevelUnlocks;
        public float EquipmentZoomSmooth = 1.0f;

        public event ChangeStateHandler? OnStateChange;

        public StarSystem? CurrentStarSystem = null;
        public LevelContainer? CurrentLevelSelected;
        public List<GameCharacter> CurrentCharactersSelected = new();

        private InterfaceState prevState = InterfaceState.Unknown;
        private readonly List<GameObject> uiElements = new();
        private readonly Dictionary<GameObject, bool> uiElementValues = new();

        public bool IsInGame { get; private set; }

        public Color StarSystemColour;
        public Color LevelColour;
        public Color WeaponColour;
        public Color UpgradeColour;

        public event System.Action? OnCharacterSelected;
        #endregion

        #region Unity Methods
        private void Awake()
        {
            Instance = this;

            this.AddElements(this.MainMenu, this.InGameUI, this.InGameMenu,
                this.TankEquipmentUI, this.ShowTankEquipmentUI, this.OptionsMenu.gameObject, this.GameOverUI,
                this.GameWonUI, this.NextLevelUI, this.CreditsUI, this.VersionUI,
                this.LevelSelect, this.StarSystemSelect, this.SelectedLevel);

            this.LevelVM.OnSectionChange += this.OnDialogueSectionChange;
            this.LevelVM.OnShowChoice += this.OnDialogueShowChoice;
            this.LevelVM.OnTextSegment += this.OnDialogTextSegment;
            this.LevelVM.OnEmotion += this.OnDialogEmotion;

            this.CheckForStateChange();
        }

        private void OnDialogueSectionChange(LevelVM.SectionType sectionType)
        {
            if (this.DialogueUI != null) this.DialogueUI.OnSectionChange(sectionType);

            if (sectionType == LevelVM.SectionType.NewLine)
            {
                this.InDialogue = true;
            }
            else if (sectionType == LevelVM.SectionType.DialogueEnded)
            {
                this.InDialogue = false;
            }
        }

        private void OnDialogueShowChoice(string text, int index)
        {
            if (this.DialogueUI != null) this.DialogueUI.OnShowChoice(text, index);
        }

        private void OnDialogTextSegment(string text)
        {
            if (this.DialogueUI != null) this.DialogueUI.OnTextSegment(text);
        }

        private void OnDialogEmotion(string emotion)
        {
            if (this.DialogueUI != null) this.DialogueUI.OnEmotion(emotion);
        }

        private void AddElements(params GameObject[] elements)
        {
            foreach (var element in elements)
            {
                this.uiElements.Add(element);
                element.SetActive(false);
                this.uiElementValues[element] = false;
            }
        }
        #endregion

        #region Methods
        public void ClearSelectedCharacters()
        {
            if (this.CurrentCharactersSelected.Any())
            {
                this.CurrentCharactersSelected.Clear();
                this.OnCharacterSelected?.Invoke();
            }
        }

        public void ToggleSelectedCharacter(GameCharacter gameCharacter)
        {
            this.CurrentCharactersSelected.ToggleValue(gameCharacter);
            this.OnCharacterSelected?.Invoke();
        }

        public void ManagedUpdate()
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetButtonDown("Cancel"))
            {
                if (this.State == InterfaceState.MainMenuOptions)
                {
                    if (this.OptionsMenu.GoBack())
                    {
                        this.State = InterfaceState.MainMenu;
                    }
                }
                else if (this.State == InterfaceState.InGameOptions)
                {
                    if (this.OptionsMenu.GoBack())
                    {
                        this.State = InterfaceState.InGameMenu;
                    }
                }
                else if (this.State == InterfaceState.InGameMenu)
                {
                    this.State = InterfaceState.InGame;
                }
                else if (this.State == InterfaceState.InGame)
                {
                    this.State = InterfaceState.InGameMenu;
                }
                else if (this.State == InterfaceState.Equipment)
                {
                    this.State = InterfaceState.InGame;
                }
                else if (this.State == InterfaceState.LevelSelect)
                {
                    this.State = InterfaceState.StarSystemSelect;
                }
                else if (this.State == InterfaceState.StarSystemSelect)
                {
                    this.State = InterfaceState.MainMenu;
                }
                else if (this.State == InterfaceState.SelectedLevel)
                {
                    this.State = InterfaceState.LevelSelect;
                }
            }
            if (Input.GetButtonDown("Show Menu"))
            {
                if (this.State == InterfaceState.InGame || this.State == InterfaceState.Equipment)
                {
                    this.State = InterfaceState.InGameMenu;
                }
                else if (this.State == InterfaceState.InGameMenu)
                {
                    this.State = InterfaceState.InGame;
                }
            }
            if (Input.GetButtonDown("Show Equipment"))
            {
                if (this.State == InterfaceState.InGame)
                {
                    this.State = InterfaceState.Equipment;
                }
                else if (this.State == InterfaceState.Equipment)
                {
                    this.State = InterfaceState.InGame;
                }
            }

            this.CheckForStateChange();
        }

        public void HideOptionsMenu()
        {
            if (this.State == InterfaceState.MainMenuOptions)
            {
                this.State = InterfaceState.MainMenu;
            }
            else if (this.State == InterfaceState.InGameOptions)
            {
                this.State = InterfaceState.InGameMenu;
            }
        }

        public void ToggleInGameMenu()
        {
            if (this.State == InterfaceState.InGame)
            {
                this.State = InterfaceState.InGameMenu;
            }
            else if (this.State == InterfaceState.InGameMenu)
            {
                this.State = InterfaceState.InGame;
            }
            else if (this.State == InterfaceState.InGameOptions)
            {
                this.State = InterfaceState.InGameMenu;
            }
        }

        private void CheckForStateChange()
        {
            if (this.State == this.prevState)
            {
                return;
            }

            Debug.Log($"Changing state: {this.prevState} -> {this.State}");

            this.OnStateChange?.Invoke(this.State, this.prevState);
            this.prevState = this.State;

            foreach (var key in this.uiElements)
            {
                this.uiElementValues[key] = false;
            }

            if (this.State == InterfaceState.MainMenu)
            {
                this.uiElementValues[this.MainMenu] = true;
                this.uiElementValues[this.VersionUI] = true;
                this.CurrentLevelSelected = null;
                this.CurrentStarSystem = null;
            }
            else if (this.State == InterfaceState.MainMenuOptions)
            {
                this.uiElementValues[this.OptionsMenu.gameObject] = true;
                this.uiElementValues[this.VersionUI] = true;
                this.OptionsMenu.GoToSelector();
            }
            else if (this.State == InterfaceState.InGame)
            {
                this.uiElementValues[this.InGameUI] = true;
                this.uiElementValues[this.ShowTankEquipmentUI] = true;
            }
            else if (this.State == InterfaceState.InGameMenu)
            {
                this.uiElementValues[this.InGameUI] = true;
                this.uiElementValues[this.InGameMenu] = true;
                this.uiElementValues[this.VersionUI] = true;
            }
            else if (this.State == InterfaceState.InGameOptions)
            {
                this.uiElementValues[this.InGameUI] = true;
                this.uiElementValues[this.OptionsMenu.gameObject] = true;
                this.OptionsMenu.GoToSelector();
            }
            else if (this.State == InterfaceState.Equipment)
            {
                this.uiElementValues[this.InGameUI] = true;
                this.uiElementValues[this.TankEquipmentUI] = true;
                this.uiElementValues[this.ShowTankEquipmentUI] = false;
            }
            else if (this.State == InterfaceState.GameOver)
            {
                this.uiElementValues[this.InGameUI] = true;
                this.uiElementValues[this.GameOverUI] = true;
            }
            else if (this.State == InterfaceState.GameWon)
            {
                this.uiElementValues[this.InGameUI] = true;
                this.uiElementValues[this.GameWonUI] = true;
            }
            else if (this.State == InterfaceState.NextLevel)
            {
                this.uiElementValues[this.InGameUI] = false;
                this.uiElementValues[this.NextLevelUI] = true;
            }
            else if (this.State == InterfaceState.Credits)
            {
                this.uiElementValues[this.CreditsUI] = true;
                this.uiElementValues[this.VersionUI] = true;
            }
            else if (this.State == InterfaceState.LevelSelect)
            {
                this.uiElementValues[this.LevelSelect] = true;
                this.CurrentLevelSelected = null;
            }
            else if (this.State == InterfaceState.SelectedLevel)
            {
                this.uiElementValues[this.SelectedLevel] = true;
            }
            else if (this.State == InterfaceState.StarSystemSelect)
            {
                this.uiElementValues[this.StarSystemSelect] = true;
                this.CurrentLevelSelected = null;
                this.CurrentStarSystem = null;
            }

            foreach (var kvp in this.uiElementValues)
            {
                if (kvp.Key.activeSelf != kvp.Value)
                {
                    kvp.Key.SetActive(kvp.Value);
                }
            }
        }

        public static float EquipmentSmoothDamp(float current, float target, ref float velocity)
        {
            if (Mathf.Approximately(current, target))
            {
                return target;
            }

            var smoothTime = Time.unscaledDeltaTime * Instance.EquipmentZoomSmooth;
            var result = Mathf.SmoothDamp(current, target, ref velocity, smoothTime, 500, Time.unscaledDeltaTime);
            return result;
        }

        public static bool ContinueButtonsPressed()
        {
            return Input.anyKeyDown || Input.GetButtonDown("Cancel") || Input.GetMouseButtonDown(0);
        }
        #endregion
    }
}