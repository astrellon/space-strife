using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using UnityEngine.UI;

#nullable enable

namespace Orbits
{
    public class GameManager : MonoBehaviour
    {
        public enum InputMethodType
        {
            Mouse, Touch, Controller
        }

        #region Fields
        public static GameManager Instance { get; private set; }

        public delegate void WeaponUpgradeHandler(WeaponType weaponType, int level);
        public delegate void LevelLoadHandler(Level level);
        public delegate void AbilityExecutedHandler(Ability ability, Level level);

        public List<StarSystem> StarSystems = new();
        public List<GameCharacter> Characters = new();
        public Canvas MainCanvas;
        public CanvasScaler MainCanvasScaler;
        public CanvasGroup MainCanvasGroup;
        public GameObject MainMenuScene;
        public Level? CurrentLevel;
        public LevelContainer? CurrentLevelContainer;
        public LevelBoundary? CurrentBoundary
        {
            get
            {
                if (this.CurrentLevel != null &&
                    this.CurrentLevel.PartOfStarSystem != null &&
                    this.CurrentLevelContainer != null &&
                    this.CurrentLevelContainer.NeedsBoundary)
                {
                    return this.CurrentLevel.PartOfStarSystem.Boundary;
                }
                return null;
            }
        }
        public List<GameCharacter> CurrentCharacters = new();
        public GameCamera Camera;
        public OpeningScene OpeningScene;

        public bool IsGameOver = false;
        public bool GamePaused = false;
        public bool GameActive => !this.IsGameOver && !this.GamePaused;

        public event WeaponUpgradeHandler? OnPlayerUpgrade;
        public event LevelLoadHandler? OnLevelLoad;
        public event Action? OnLevelStopped;
        public event AbilityExecutedHandler? OnAbilityExecuted;

        public Vector3 MouseTouchPosition;
        public bool MouseTouchDown;
        public bool TakingInputFromMouse = true;
        private Vector3 prevMousePosition;

        public InputMethodType InputMethod = InputMethodType.Mouse;
        public bool WaitingForTouchInput = true;
        public Joystick MoveJoystick;
        public Joystick PointJoystick;
        public Joystick CombinedJoystick;
        public WaveIndicator IndicatorPrefab;
        public bool SlowGame;
        public bool TimeManipulatorActive;
        public float PlayerDeltaTimeScale = 1.0f;

        public DamageIndicator DamageIndicatorPrefab;

        private readonly List<DamageIndicator> damageIndicators = new(200);
        private readonly Dictionary<StarSystemId, StarSystem> starSystemMap = new();

        public bool IsLandscape => this.OverridePortrait == false &&
            (Input.deviceOrientation == DeviceOrientation.Unknown ||
            Input.deviceOrientation == DeviceOrientation.LandscapeLeft ||
            Input.deviceOrientation == DeviceOrientation.LandscapeRight);

        public bool OverridePortrait = false;

        public delegate void OrientationChangeHandler(bool landscape);
        public event OrientationChangeHandler? OnOrientationChange;

        private DeviceOrientation prevOrientation = DeviceOrientation.Unknown;
        private bool prevLandscape = true;
        private bool prevLandscapeSet = false;

        public Vector2 JoystickMoveInput => this.CombinedJoystick.gameObject.activeSelf ?
            (this.CombinedJoystick.Direction.magnitude > 0.6 ? this.CombinedJoystick.Direction : Vector2.zero) :
            this.MoveJoystick.Direction;

        public Vector2 JoystickPointInput => this.CombinedJoystick.gameObject.activeSelf ?
            this.CombinedJoystick.Direction :
            this.PointJoystick.Direction;

        private double globalTimeValue = 0.0;
        private double globalTimeValueUnscaled = 0.0;

        public float TimeScaleRatio = 1.0f;
        public float TimeAccessibleScale = 1.0f;
        public float TimeAccessibleWaveScale = 1.0f;
        #endregion

        #region Unity Methods
        private void Awake()
        {
            if (Instance != null)
            {
                throw new Exception("Double initialised GameManager");
            }

            foreach (var starSystem in this.StarSystems)
            {
                this.starSystemMap[starSystem.Id] = starSystem;
            }

            Instance = this;

            this.UpdateJoysticks();

            for (var i = 0; i < this.damageIndicators.Capacity; i++)
            {
                this.damageIndicators.Add(GameObjectPools.Instance.Spawn(this.DamageIndicatorPrefab));
            }
            for (var i = 0; i < this.damageIndicators.Capacity; i++)
            {
                GameObjectPools.Instance.Release(this.DamageIndicatorPrefab.gameObject, this.damageIndicators[i].gameObject);
            }
            this.damageIndicators.Clear();

            UIManager.Instance.OnStateChange += this.OnUIStateChange;

            if (!this.MainCanvas.gameObject.activeSelf)
            {
                this.MainCanvas.gameObject.SetActive(true);
            }
        }

        private void Start()
        {
            this.UpdateTankGlow();
            GameOptions.OnChange += this.OnGameOptionsChange;
        }

        private void Update()
        {
            this.globalTimeValue += Time.deltaTime;
            this.globalTimeValueUnscaled += Time.unscaledDeltaTime;

            if (this.globalTimeValue > 5000.0)
            {
                this.globalTimeValue = -5000.0;
            }
            if (this.globalTimeValueUnscaled > 5000.0)
            {
                this.globalTimeValueUnscaled = -5000.0;
            }

            Shader.SetGlobalFloat("_GlobalTime", (float)this.globalTimeValue);
            Shader.SetGlobalFloat("_GlobalTimeUnscaled", (float)this.globalTimeValueUnscaled);

            if (Input.GetKeyDown(KeyCode.F11))
            {
                var date = DateTime.Now;
                var dateStr = date.ToString("yyyyMMdd_HHmmssffff");
                var filename = $"space-strife-{dateStr}";
                ScreenCapture.CaptureScreenshot(filename, 2);
                Debug.Log($"Screenshot taken: {filename}");
            }

            if (Input.touchCount > 0)
            {
                this.InputMethod = InputMethodType.Touch;
                this.Camera.EnableFollowMovement = false;
            }
            if (Input.GetMouseButtonDown(0) || Input.GetAxis("Horizontal") != 0.0f)
            {
                this.InputMethod = InputMethodType.Mouse;
                this.Camera.EnableFollowMovement = true;
            }
            if (Input.GetAxis("Joystick Horizontal") != 0.0f || Input.GetAxis("Point Horizontal") != 0.0f || Input.GetAxis("Point Vertical") != 0.0f)
            {
                this.InputMethod = InputMethodType.Controller;
                this.Camera.EnableFollowMovement = true;
            }

            if (Input.GetKeyDown(KeyCode.P) && Input.GetKeyDown(KeyCode.LeftShift))
            {
                this.ForceFinishCurrentLevel();
            }
            if (Input.GetKeyDown(KeyCode.F11))
            {
                this.MainCanvasGroup.alpha = this.MainCanvasGroup.alpha > 0.5f ? 0.0f : 1.0f;
            }

            this.UpdateJoysticks();

            var newOrientation = Input.deviceOrientation;
            if (newOrientation != this.prevOrientation)
            {
                this.prevOrientation = newOrientation;
            }

            var isLandscape = this.IsLandscape;
            if (!this.prevLandscapeSet || this.prevLandscape != isLandscape)
            {
                this.prevLandscapeSet = true;
                this.prevLandscape = isLandscape;
                this.MainCanvasScaler.matchWidthOrHeight = this.IsLandscape ? 1.0f : 0.5f;
                this.UpdateLevelRotation();
                this.OnOrientationChange?.Invoke(this.IsLandscape);
            }

            var mousePosition = Input.mousePosition;
            if (mousePosition != this.prevMousePosition)
            {
                this.TakingInputFromMouse = true;
            }

            if (this.TakingInputFromMouse)
            {
                this.MouseTouchPosition = mousePosition;
            }
            else
            {
                if (Input.touchCount > 0)
                {
                    this.MouseTouchPosition = Input.GetTouch(0).position;
                }
            }

            UIManager.Instance.ManagedUpdate();
            this.UpdateTimeScale();

            var dt = Time.deltaTime;
            for (var i = 0; i < this.damageIndicators.Count; i++)
            {
                var indicator = this.damageIndicators[i];
                var finished = indicator.ManagedUpdate(dt);
                if (finished)
                {
                    GameObjectPools.Instance.Release(this.DamageIndicatorPrefab.gameObject, indicator.gameObject);
                    this.damageIndicators[i] = this.damageIndicators[this.damageIndicators.Count - 1];
                    this.damageIndicators.RemoveAt(this.damageIndicators.Count - 1);
                    i--;
                }
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && this.CurrentLevel != null)
            {
                UIManager.Instance.State = InterfaceState.InGameMenu;
            }
        }
        #endregion

        #region Methods
        public bool IsCurrentlyPlayingLevel(StarSystemId starSystemId, LevelId levelId)
        {
            return this.CurrentLevel != null &&
                this.CurrentLevel.Id == levelId &&
                this.CurrentLevel.PartOfStarSystem != null &&
                this.CurrentLevel.PartOfStarSystem.Id == starSystemId;
        }

        public bool TryGetPlayerShip([NotNullWhen(true)] out TankShip? result)
        {
            if (this.CurrentLevelContainer != null)
            {
                result = this.CurrentLevelContainer.Ship;
                return result != null;
            }

            result = null;
            return false;
        }

        public void UpdateLevelRotation()
        {
            var rotation = this.IsLandscape ? Quaternion.identity : Quaternion.Euler(0, -90, 0);
            if (this.CurrentLevel != null)
            {
                this.CurrentLevel.transform.rotation = rotation;
            }
            else if (this.MainMenuScene != null)
            {
                // var position = this.IsLandscape ?
                //     new Vector3(9.77510548f, , 5.19726467f) :
                //     new Vector3(-10, -17.3850002f, 5.19726467f);
                this.MainMenuScene.transform.rotation = rotation;
            }
        }

        public void RestartLevel()
        {
            if (this.CurrentLevel != null)
            {
                this.StartLevel(this.CurrentLevel.PartOfStarSystem.Id, this.CurrentLevel.Id, this.CurrentCharacters, isFirstInit: false);
            }
        }

        public void UpdateTimeScale()
        {
            if (this.GamePaused)
            {
                this.TimeScaleRatio = 0.0f;
                this.PlayerDeltaTimeScale = 0.0f;
            }
            else if (this.SlowGame)
            {
                var multiplier = this.TimeManipulatorActive ? 0.5f : 1.0f;
                this.TimeScaleRatio = 0.2f * multiplier;

                this.PlayerDeltaTimeScale = this.TimeManipulatorActive ? 2.0f : 1.0f;

                if (GameOptions.PauseGameInMenus)
                {
                    this.TimeScaleRatio = 0.0f;
                    // this.PlayerDeltaTimeScale = 0.0f;
                }
            }
            else if (this.TimeManipulatorActive)
            {
                this.TimeScaleRatio = 0.5f;
                this.PlayerDeltaTimeScale = 2.0f;
            }
            else if (this.TimeScaleRatio < 1.0f)
            {
                this.TimeScaleRatio = Mathf.Clamp01(this.TimeScaleRatio + Time.unscaledDeltaTime);
                if (this.PlayerDeltaTimeScale > 1.0f)
                {
                    this.PlayerDeltaTimeScale = Mathf.Clamp01(this.PlayerDeltaTimeScale - Time.unscaledDeltaTime);
                }
                else
                {
                    this.PlayerDeltaTimeScale = 1.0f;
                }
            }

            Time.timeScale = this.TimeScaleRatio * this.TimeAccessibleScale;
        }

        public void Exit()
        {
            Application.Quit(0);
        }

        public bool TryGetStarSystem(StarSystemId starSystemId, [NotNullWhen(true)] out StarSystem? result)
        {
            return this.starSystemMap.TryGetValue(starSystemId, out result);
        }

        public bool TryGetLevel(StarSystemId starSystemId, LevelId levelId, [NotNullWhen(true)] out StarSystem? starSystem, [NotNullWhen(true)] out LevelContainer? level)
        {
            if (this.TryGetStarSystem(starSystemId, out starSystem))
            {
                if (starSystem.TryGetLevel(levelId, out var levelInfo))
                {
                    level = levelInfo;
                    return true;
                }
            }

            level = null;
            return false;
        }

        public bool TryGetCharacter(GameCharacterId characterId, [NotNullWhen(true)] out GameCharacter? gameCharacter)
        {
            foreach (var entry in this.Characters)
            {
                if (entry.CharacterId == characterId.Value)
                {
                    gameCharacter = entry;
                    return true;
                }
            }

            gameCharacter = null;
            return false;
        }

        public void StartLevel(StarSystemId starSystemId, LevelId levelId, List<GameCharacter> characters, bool isFirstInit)
        {
            this.StopCurrentLevel();

            this.Camera.EnablePlayerZoom = true;

            if (!this.TryGetLevel(starSystemId, levelId, out var starSystem, out var levelContainer))
            {
                Debug.LogError($"Unable to find level: {starSystemId}/{levelId}");
                return;
            }
            if (levelContainer == null)
            {
                Debug.LogError($"Found level is null: {starSystemId}/{levelId}");
                return;
            }
            if (levelContainer.LevelHolderTarget == null)
            {
                Debug.LogError($"Level holder target is null: {starSystemId}/{levelId}");
                return;
            }
            if (levelContainer.Target == null)
            {
                Debug.LogError($"Level doesn't have a starting target planet: {starSystemId}/{levelId}");
                return;
            }

            Debug.Log($"Target: {levelContainer.LevelHolderTarget.name}");

            this.CurrentCharacters = characters;
            var abilities = this.CurrentCharacters.SelectMany(c => c.GetAbilities()).ToList();

            this.CurrentLevel = Instantiate(levelContainer.LevelPrefab, levelContainer.LevelHolderTarget, false);
            this.CurrentLevel.Container = levelContainer;
            if (levelContainer.Target.TryGetComponent<GravitySource>(out var targetGravity))
            {
                this.CurrentLevel.Player.UpdatePlanetRadius(targetGravity.InnerRange);
            }

            this.CurrentLevelContainer = levelContainer;
            this.CurrentLevel.InitLevel(starSystem, levelContainer.Target.gameObject, abilities, levelContainer.Script, isFirstInit);

            UIManager.Instance.State = InterfaceState.PreLevelStart;
            this.CurrentLevel.OnStarted += this.OnLevelStarted;
            this.CurrentLevel.OnStartWaves += this.OnLevelStartWaves;
        }

        private void OnLevelStarted()
        {
            var levelContainer = this.CurrentLevelContainer;

            if (levelContainer == null || this.CurrentLevel == null)
            {
                Debug.LogError("Level started but nothing is set");
                return;
            }

            var starSystem = this.CurrentLevel.PartOfStarSystem;
            if (starSystem == null)
            {
                Debug.LogError($"Level not part of star system: {this.CurrentLevel.name}");
                return;
            }

            if (levelContainer.NeedsBoundary && starSystem.Boundary != null)
            {
                starSystem.SetEnableBoundary(true);
            }

            levelContainer.OnLevelStarted(this.CurrentLevel);
            if (levelContainer.FocusOn != null)
            {
                this.Camera.TransitionTo(levelContainer.FocusOn, 1.0f, Easing.Quadratic.Out);
                this.Camera.ResetZoom();
            }
            else if (this.CurrentLevel.Player != null)
            {
                this.Camera.Zoom = this.Camera.InGameHeightOffset;
                if (this.CurrentLevel.Player.ValidTanks.Count > 0)
                {
                    this.Camera.SetFocusOn(this.CurrentLevel.Player.ValidTanks[0].gameObject);
                }
                else
                {
                    this.Camera.SetFocusOn(this.CurrentLevel.Player.Planet);
                }
            }

            this.OnLevelLoad?.Invoke(this.CurrentLevel);

            this.UpdateLevelRotation();

            ProjectileManager.Instance.AddGravitySources(levelContainer);
            ProjectileManager.Instance.enabled = true;
            PlayerState.Instance.StartNewLevelStats(starSystem.Id, levelContainer.LevelPrefab.Id);
        }

        private void OnLevelStartWaves()
        {
            UIManager.Instance.State = InterfaceState.InGame;
        }

        public void StopCurrentLevel()
        {
            this.OnLevelStopped?.Invoke();
            PlayerState.Instance.ClearLevelStats();
            OffScreenTargetIndicator.Instance.Clear();
            WaveShipManager.Instance.Clear();
            ProjectileManager.Instance.Clear();
            PortalManager.Instance.ClosePortals();

            this.Camera.FocusOn = null;
            this.Camera.EnablePlayerZoom = false;

            if (this.CurrentLevel != null)
            {
                if (this.CurrentLevelContainer != null)
                {
                    this.CurrentLevelContainer.OnLevelStopped(this.CurrentLevel);
                }

                this.CurrentLevel.OnStarted -= this.OnLevelStarted;
                this.CurrentLevel.OnStartWaves -= this.OnLevelStartWaves;

                Destroy(this.CurrentLevel.gameObject);
                this.CurrentLevel = null;
                this.CurrentLevelContainer = null;
            }

            foreach (var starSystem in this.StarSystems)
            {
                starSystem.Reset();
            }
        }

        public void LevelEnd(Level level)
        {
            UIManager.Instance.State = InterfaceState.PostLevel;
            if (!level.LevelEnd())
            {
                this.LevelFinished(level);
            }
        }

        public void ForceFinishCurrentLevel()
        {
            if (this.CurrentLevel != null)
            {
                this.LevelEnd(this.CurrentLevel);
                this.CurrentLevel.enabled = false;
            }
        }

        public void LevelFinished(Level level)
        {
            var unlockTextEntries = new List<string>();
            var bonusUnlockTextEntries = new List<string>();

            var hasFinishedGame = false;
            var returnToStarSystem = false;

            HandleUnlocks(level, level.LevelUnlocks, unlockTextEntries, ref hasFinishedGame, ref returnToStarSystem);
            if (level.IsAtMaxHealth)
            {
                HandleUnlocks(level, level.LevelFullHealthUnlocks, bonusUnlockTextEntries, ref hasFinishedGame, ref returnToStarSystem);
            }

            var text = "";
            if (unlockTextEntries.Any())
            {
                text = string.Join("\n", unlockTextEntries);
            }

            if (bonusUnlockTextEntries.Any())
            {
                if (text.Length > 0)
                {
                    text += "\n\nPerfect Run Bonuses:\n";
                }

                text += string.Join("\n", bonusUnlockTextEntries);
            }

            PlayerState.Instance.FinishLevelStats(this.CurrentCharacters.Select(c => c.Id), level);
            UIManager.Instance.NextLevelUnlocks.Init(text, hasFinishedGame, returnToStarSystem);
            UIManager.Instance.State = InterfaceState.NextLevel;
        }

        private static void HandleUnlocks(Level level, IReadOnlyList<LevelUnlock> unlocks, List<string> result, ref bool hasFinishedGame, ref bool returnToStarSystem)
        {
            foreach (var unlock in unlocks)
            {
                var unlockResult = unlock.Execute(level);

                if (unlock.Type == LevelUnlock.UnlockType.FinishGame)
                {
                    hasFinishedGame = true;
                }
                if (unlockResult.CameraToStarSystem)
                {
                    returnToStarSystem = true;
                }

                if (!string.IsNullOrWhiteSpace(unlockResult.Message))
                {
                    result.Add(unlockResult.Message);
                }
            }
        }

        public void TriggerAbility(Ability ability)
        {
            if (this.CurrentLevel == null)
            {
                Debug.LogError($"Cannot trigger ability without a level");
                return;
            }

            ability.Execute(this.CurrentLevel);
            this.OnAbilityExecuted?.Invoke(ability, this.CurrentLevel);
        }

        public void DamagePlanet(int amount)
        {
            if (this.CurrentLevel != null)
            {
                if (this.CurrentLevel.DamagePlanet(amount))
                {
                    UIManager.Instance.State = InterfaceState.GameOver;
                }
            }
        }

        public void ShowDamage(Vector3 at, Vector3 direction, float amount)
        {
            var spawned = GameObjectPools.Instance.Spawn(this.DamageIndicatorPrefab);
            spawned.Show(at, at + direction, amount.ToString());
            this.damageIndicators.Add(spawned);
        }

        public bool IsFirstTimePlayingCurrentLevel([NotNullWhen(true)] out StarSystem? starSystem, [NotNullWhen(true)] out LevelContainer? level) {
            level = UIManager.Instance.CurrentLevelSelected;
            starSystem = UIManager.Instance.CurrentStarSystem;
            if (level != null && level.LevelPrefab != null && starSystem != null &&
                PlayerState.Instance.IsFirstTimePlayingLevel(starSystem.Id, level.LevelPrefab.Id))
            {
                return true;
            }

            return false;
        }

        private void OnUIStateChange(InterfaceState newState, InterfaceState prevState)
        {
            this.GamePaused = false;
            this.SlowGame = false;
            this.IsGameOver = false;
            this.Camera.ZoomOverride = 0.0f;
            this.Camera.FocusOnOverride = null;
            var tankSelectAlpha = 0.0f;

            this.Camera.EnableFollowMovement = true;

            if (newState == InterfaceState.MainMenuOpening)
            {
                AudioManager.Instance.SetBGM("Intro", 0.5f);
                this.Camera.EnableFollowMovement = false;
            }

            if (newState == InterfaceState.InGameMenu ||
                newState == InterfaceState.InGameOptions ||
                newState == InterfaceState.GameOver)
            {
                this.GamePaused = true;
            }
            else if (newState == InterfaceState.Equipment)
            {
                this.SlowGame = true;
                if (this.CurrentLevel != null)
                {
                    this.Camera.ZoomOverride = this.CurrentLevel.ZoomForEquipment;
                }
                tankSelectAlpha = 1.0f;
            }
            else if (newState == InterfaceState.MainMenu ||
                newState == InterfaceState.MainMenuOpening ||
                newState == InterfaceState.MainMenuOptions ||
                newState == InterfaceState.Credits ||
                newState == InterfaceState.LevelSelect ||
                newState == InterfaceState.StarSystemSelect ||
                newState == InterfaceState.SelectedLevel)
            {
                AudioManager.Instance.SetBGM("Intro", 0.5f);
                this.StopCurrentLevel();
                this.UpdateLevelRotation();
            }
            else if (newState == InterfaceState.GameOver)
            {
                this.IsGameOver = true;
                ProjectileManager.Instance.enabled = false;
            }

            if (newState == InterfaceState.LevelSelect)
            {
                this.Camera.TransitionTo(UIManager.Instance.CurrentStarSystem.UIPoint.GetFocusOffset(), 1.0f, Easing.Quadratic.Out);
                this.Camera.ResetZoom();
                UIManager.Instance.ClearSelectedCharacters();
            }
            else if (newState == InterfaceState.SelectedLevel)
            {
                if (UIManager.Instance.CurrentLevelSelected != null && UIManager.Instance.CurrentLevelSelected.FocusOnPreLevel != null)
                {
                    this.Camera.TransitionTo(UIManager.Instance.CurrentLevelSelected.FocusOnPreLevel, 1.0f, Easing.Quadratic.Out);
                }
                else
                {
                    this.Camera.TransitionTo(UIManager.Instance.CurrentLevelSelected.Target.GetFocusOffset(), 1.0f, Easing.Quadratic.Out);
                }
                this.Camera.ResetZoom();
            }
            else if (newState == InterfaceState.MainMenu || newState == InterfaceState.StarSystemSelect)
            {
                this.Camera.TransitionTo(this.OpeningScene.TargetPosition, 1.0f, Easing.Quadratic.Out);
                this.Camera.ResetZoom();
            }

            UIManager.Instance.TankSelectGroup.SetAlpha(tankSelectAlpha);

            this.UpdateTimeScale();
        }

        private void UpdateJoysticks()
        {
            var moveJoystickEnable = false;
            var pointJoystickEnable = false;
            var combinedJoystickEnable = false;
            if (this.InputMethod == InputMethodType.Touch)
            {
                if (this.IsLandscape)
                {
                    moveJoystickEnable = true;
                    pointJoystickEnable = true;
                }
                else
                {
                    combinedJoystickEnable = UIManager.Instance.State == InterfaceState.InGame;
                }
            }

            if (moveJoystickEnable != this.MoveJoystick.gameObject.activeSelf)
            {
                this.MoveJoystick.gameObject.SetActive(moveJoystickEnable);
            }
            if (pointJoystickEnable != this.PointJoystick.gameObject.activeSelf)
            {
                this.PointJoystick.gameObject.SetActive(pointJoystickEnable);
            }
            if (combinedJoystickEnable != this.CombinedJoystick.gameObject.activeSelf)
            {
                this.CombinedJoystick.gameObject.SetActive(combinedJoystickEnable);
            }
        }

        private void OnGameOptionsChange(SettingType type)
        {
            if (type == SettingType.TankGlow)
            {
                this.UpdateTankGlow();
            }
            else if (type == SettingType.GameSpeedScale)
            {
                this.TimeAccessibleScale = GameOptions.GameSpeedScale;
            }
            else if (type == SettingType.WaveSpeedScale)
            {
                this.TimeAccessibleWaveScale = GameOptions.WaveSpeedScale;
            }
        }

        private void UpdateTankGlow()
        {
            Shader.SetGlobalFloat("_TankGlowAlpha", GameOptions.TankGlow);
        }
        #endregion

        #region Debug
        #endregion
    }
}
