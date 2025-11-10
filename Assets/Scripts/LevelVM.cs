using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using LysitheaVM;
using LysitheaVM.Unity;
using Orbits.Serialiser;

#nullable enable

namespace Orbits
{
    public enum ActorPosition
    {
        Unknown, Left, Right
    }

    public class ActorInfo
    {
        public readonly GameCharacter Actor;

        public string Emotion;

        public ActorInfo(GameCharacter actor, string emotion = "idle")
        {
            this.Actor = actor;
            this.Emotion = emotion;
        }
    }

    [Serializable]
    public class ColouredNames
    {
        public Color Colour = Color.white;
        public string Id = "";
        public string Name = "";
    }

    public class LevelVM : MonoBehaviour
    {
        public enum SectionType
        {
            NewLine, DialogueEnded, ForChoices, ToContinue
        }

        public delegate void TextSegmentHandler(string text);
        public delegate void ShowChoiceHandler(string text, int index);
        public delegate void SectionHandler(SectionType sectionType);
        public delegate void EmotionHandler(string emotion);
        public delegate void PortraitHandler(ActorPosition position, ActorInfo? actorInfo);

        public event TextSegmentHandler? OnTextSegment;
        public event ShowChoiceHandler? OnShowChoice;
        public event SectionHandler? OnSectionChange;
        public event EmotionHandler? OnEmotion;
        public event PortraitHandler? OnActorChange;

        public static LevelVM Instance;

        public ActorInfo? LeftActor { get; private set; }
        public ActorInfo? RightActor { get; private set; }
        public ActorPosition CurrentTalkingActorPosition = ActorPosition.Unknown;
        public ActorInfo? CurrentTalkingActor
        {
            get
            {
                return this.CurrentTalkingActorPosition switch
                {
                    ActorPosition.Left => this.LeftActor,
                    ActorPosition.Right => this.RightActor,
                    _ => null,
                };
            }
        }
        public VMRunner VMRunner;

        [ArrayElementTitle("Name")]
        public List<ColouredNames> ColouredNames;

        private Assembler? assembler;
        private VirtualMachine vm => this.VMRunner.VM;
        private readonly List<IValue> choiceBuffer = new();

        private ObjectValue? colouredNamesObject = null;

        private ArrayValue currentCharacters = new ();

        private readonly Dictionary<string, IFunctionValue> registeredFunctions = new();

        public bool TryGetColouredName(string id, out string result)
        {
            if (this.colouredNamesObject == null)
            {
                this.colouredNamesObject = this.MakeColouredNamesObject();
            }

            return this.colouredNamesObject.TryGetValue(id, out result);
        }

        public bool TryGetActorAtPosition(ActorPosition position, [NotNullWhen(true)] out ActorInfo? result)
        {
            result = position switch
            {
                ActorPosition.Left => this.LeftActor,
                ActorPosition.Right => this.RightActor,
                _ => null,
            };

            return result != null;
        }

        public ActorPosition GetActorPosition(GameCharacter actor)
        {
            if (this.LeftActor != null && this.LeftActor.Actor == actor)
            {
                return ActorPosition.Left;
            }
            if (this.RightActor != null && this.RightActor.Actor == actor)
            {
                return ActorPosition.Right;
            }
            return ActorPosition.Unknown;
        }

        void Awake()
        {
            Instance = this;

            this.assembler = this.CreateAssembler();
            this.VMRunner.Init(32);
            this.VMRunner.OnComplete += (runner) =>
            {
                this.OnSectionChange?.Invoke(SectionType.DialogueEnded);
            };
            this.VMRunner.OnError += (runner, exp) =>
            {
                Debug.LogWarning($"Dialogue Error: {exp.Message}");
                if (exp is VirtualMachineException vmExp)
                {
                    Debug.LogWarning("- " + string.Join("\n- ", vmExp.VirtualMachineStackTrace));
                }
            };
        }

        void Start()
        {
            PortalManager.Instance.OnMovedThroughPortal += this.OnMovedThroughPortal;
            PortalManager.Instance.OnPortalsClosed += this.OnPortalsClosed;
        }

        private void OnPortalsClosed()
        {
            this.RunRegisteredFunction("portalsClosed");
        }

        private void OnMovedThroughPortal(PortalableTarget target, Portal atPortal, Vector3 moveDiff)
        {
            this.RunRegisteredFunction("movedThroughPortal", target);
        }

        public Script AssembleScript(string sourceName, string text)
        {
            if (this.assembler == null)
            {
                throw new InvalidOperationException("Assembler not created yet");
            }
            return this.assembler.ParseFromText(sourceName, text);
        }

        private ObjectValue MakeColouredNamesObject()
        {
            var result = new Dictionary<string, IValue>();
            foreach (var name in this.ColouredNames)
            {
                result[name.Id] = new StringValue(Utils.ColouredText(name.Colour, name.Name));
            }

            return new ObjectValue(result);
        }

        public void StartScript(Script script, Action<VirtualMachine>? beforeStartScript = null)
        {
            this.vm.GlobalScope.Clear();
            this.registeredFunctions.Clear();

            var characters = new List<IValue>();
            var mainCharDefined = false;
            var isFirstTime = GameManager.Instance.IsFirstTimePlayingCurrentLevel(out var starSystem, out var level);
            var gameCharPlays = PlayerState.EmptyGameCharacterPlays;
            if (starSystem != null && level != null && level.LevelPrefab != null)
            {
                gameCharPlays = PlayerState.Instance.GetCharacterPlays(starSystem.Id, level.LevelPrefab.Id);
            }

            this.vm.GlobalScope.TryDefine("FIRSTTIME", isFirstTime ? BoolValue.True : BoolValue.False);

            if (this.colouredNamesObject == null)
            {
                this.colouredNamesObject = this.MakeColouredNamesObject();
            }

            this.vm.GlobalScope.TryDefine("NAMES", this.colouredNamesObject);

            if (GameManager.Instance.CurrentLevel != null)
            {
                var abilitiesMap = GameManager.Instance.CurrentLevel.ActiveAbilities.ToDictionary(a => a.Id, a => a as IValue);
                var abilities = new ObjectValue(abilitiesMap);
                this.vm.GlobalScope.TryDefine("ABILITIES", abilities);
            }

            var playsCount = new Dictionary<string, IValue>();
            foreach (var character in GameManager.Instance.Characters)
            {
                var value = new GameCharacterValue(character);
                this.vm.GlobalScope.TryDefine(character.CharacterId, value);
                if (GameManager.Instance.CurrentCharacters.Contains(character))
                {
                    characters.Add(value);

                    if (!mainCharDefined)
                    {
                        this.vm.GlobalScope.TryDefine("MAINCHAR", value);
                        mainCharDefined = true;
                    }
                }

                if (!gameCharPlays.TryGetValue(character.Id, out var count))
                {
                    count = 0;
                }
                playsCount[character.CharacterId] = new NumberValue(count);
            }

            this.vm.GlobalScope.TryDefine("PLAYCOUNTS", new ObjectValue(playsCount));

            this.currentCharacters = new ArrayValue(characters);
            this.vm.GlobalScope.TryDefine("CHARS", this.currentCharacters);
            this.vm.GlobalScope.TryDefine("DELTATIME", new NumberValue(Time.deltaTime));

            beforeStartScript?.Invoke(this.vm);

            this.VMRunner.StartScript(script);
            this.VMRunner.VM.Execute(script);
        }

        public void Continue()
        {
            if (choiceBuffer.Count > 0)
            {
                // Debug.Log("Cannot continue, needs to make a choice.");
                return;
            }

            this.vm.Paused = false;
            this.VMRunner.Running = true;
        }

        public void SelectChoice(int index)
        {
            if (index < 0 || index >= this.choiceBuffer.Count)
            {
                return;
            }

            var choiceValue = this.choiceBuffer[index];
            Debug.Log($"Selecting choice: {index}, {choiceValue.ToString()}");
            if (choiceValue is IFunctionValue choiceFunc)
            {
                this.vm.CallFunction(choiceFunc, 0, false);
            }
            else
            {
                this.vm.Jump(choiceValue.ToString());
            }
            this.vm.Paused = false;
        }

        public void CreateChoice(string choiceLabel, IValue choiceValue)
        {
            var index = this.choiceBuffer.Count;
            this.choiceBuffer.Add(choiceValue);
            this.OnShowChoice?.Invoke(choiceLabel, index);
        }

        public bool RunUpdateFunction()
        {
            this.vm.GlobalScope.TryDefine("DELTATIME", new NumberValue(Time.deltaTime));
            return this.RunRegisteredFunction("update");
        }

        public bool RunRegisteredFunction(string type, params IValue[] args)
        {
            if (this.registeredFunctions.TryGetValue(type, out var func))
            {
                this.VMRunner.RunFunction(func, args);
                return true;
            }

            return false;
        }

        private Assembler CreateAssembler()
        {
            var assembler = new Assembler();
            StandardLibrary.AddToScope(assembler.BuiltinScope);
            assembler.BuiltinScope.CombineScope(UnityLibrary.Scope);

            assembler.BuiltinScope.TryDefine("startWaves", this.StartWaveFunc, hasReturn: false);
            assembler.BuiltinScope.TryDefine("startLevel", this.StartLevelFunc, hasReturn: false);
            assembler.BuiltinScope.TryDefine("endLevel", this.EndLevelFunc, hasReturn: false);
            assembler.BuiltinScope.TryDefine("finishLevel", this.FinishLevelFunc, hasReturn: false);
            assembler.BuiltinScope.TryDefine("actor", this.ActorFunc, hasReturn: false);
            assembler.BuiltinScope.TryDefine("emotion", this.EmotionFunc, hasReturn: false);
            assembler.BuiltinScope.TryDefine("beginLine", this.BeginLineFunc, hasReturn: false);
            assembler.BuiltinScope.TryDefine("text", this.TextFunc, hasReturn: false);
            assembler.BuiltinScope.TryDefine("endLine", this.EndLineFunc, hasReturn: false);
            assembler.BuiltinScope.TryDefine("line", this.LineFunc, hasReturn: false);
            assembler.BuiltinScope.TryDefine("choice", this.ChoiceFunc, hasReturn: false);
            assembler.BuiltinScope.TryDefine("wait", this.WaitFunc, hasReturn: false);
            assembler.BuiltinScope.TryDefine("moveTo", this.MoveToFunc);
            assembler.BuiltinScope.TryDefine("isChar", this.IsCharFunc);
            assembler.BuiltinScope.TryDefine("getFlag", this.GetFlagFunc);
            assembler.BuiltinScope.TryDefine("setFlag", this.SetFlagFunc, hasReturn: false);
            assembler.BuiltinScope.TryDefine("getTime", this.GetTimeFunc, hasReturn: true);
            assembler.BuiltinScope.TryDefine("playEffect", this.PlayEffectFunc, hasReturn: false);
            assembler.BuiltinScope.TryDefine("getStat", this.GetStatFunc, hasReturn: true);
            assembler.BuiltinScope.TryDefine("pauseGame", this.PauseGameFunc, hasReturn: false);
            assembler.BuiltinScope.TryDefine("register", this.RegisterFunc, hasReturn: false);
            assembler.BuiltinScope.TryDefine("unlock", this.UnlockFunc, hasReturn: false);
            assembler.BuiltinScope.TryDefine("openPortals", this.OpenPortalsFunc, hasReturn: false);
            assembler.BuiltinScope.TryDefine("closePortals", this.ClosePortalsFunc, hasReturn: false);
            assembler.BuiltinScope.TryDefine("showStarSystem", this.ShowStarSystemFunc, hasReturn: false);
            assembler.BuiltinScope.TryDefine("hideStarSystem", this.HideStarSystemFunc, hasReturn: false);
            assembler.BuiltinScope.TryDefine("startSubLevel", this.StartSubLevelFunc, hasReturn: false);
            assembler.BuiltinScope.TryDefine("removeSubLevel", this.RemoveSubLevelFunc, hasReturn: false);
            assembler.BuiltinScope.TryDefine("levelBoundary", this.LevelBoundaryFunc, hasReturn: false);
            assembler.BuiltinScope.TryDefine("offsetStarSystem", this.OffsetStarSystemFunc, hasReturn: false);
            assembler.BuiltinScope.TryDefine("playBGM", this.PlayBGMFunc, hasReturn: false);

            return assembler;
        }

        public void PlayBGMFunc(VirtualMachine vm, ArgumentsValue args)
        {
            var id = args.GetIndexString(0);
            var fadeOutTime = 1.0f;
            if (args.Length > 1)
            {
                fadeOutTime = args.GetIndexFloat(1);
            }
            AudioManager.Instance.SetBGM(id, fadeOutTime);
        }

        public void OffsetStarSystemFunc(VirtualMachine vm, ArgumentsValue args)
        {
            var starSystemId = new StarSystemId(args.GetIndexString(0));
            var x = args.GetIndexFloat(1);
            var z = args.GetIndexFloat(2);

            if (GameManager.Instance.TryGetStarSystem(starSystemId, out var found))
            {
                found.OffsetPosition(new Vector3(x, 0, z));
            }
        }

        public void LevelBoundaryFunc(VirtualMachine vm, ArgumentsValue args)
        {
            var starSystemId = new StarSystemId(args.GetIndexString(0));
            var show = args.GetIndexBoolean(1);

            if (GameManager.Instance.TryGetStarSystem(starSystemId, out var found))
            {
                found.SetEnableBoundary(show);
            }
        }

        public void StartSubLevelFunc(VirtualMachine vm, ArgumentsValue args)
        {
            var subLevelName = args.GetIndexString(0);
            GameManager.Instance.CurrentLevelContainer.StartSubLevel(subLevelName, GameManager.Instance.CurrentLevel);
        }

        public void RemoveSubLevelFunc(VirtualMachine vm, ArgumentsValue args)
        {
            var subLevelName = args.GetIndexString(0);
            GameManager.Instance.CurrentLevelContainer.RemoveSubLevel(subLevelName, GameManager.Instance.CurrentLevel);
        }

        public void OpenPortalsFunc(VirtualMachine vm, ArgumentsValue args)
        {
            var portal1Name = args.GetIndexString(0);
            var portal2Name = args.GetIndexString(1);

            var portalManager = PortalManager.Instance;
            if (!portalManager.TryGetPortalPosition(portal1Name, out var portal1Pos))
            {
                Debug.LogWarning($"Unable to find portal 1 with name: {portal1Name}");
                return;
            }
            if (!portalManager.TryGetPortalPosition(portal2Name, out var portal2Pos))
            {
                Debug.LogWarning($"Unable to find portal 2 with name: {portal2Name}");
                return;
            }

            PortalManager.Instance.ShowPortal(new WorldTransformTarget(portal1Pos), new WorldTransformTarget(portal2Pos));
        }

        public void ClosePortalsFunc(VirtualMachine vm, ArgumentsValue args)
        {
            PortalManager.Instance.ClosePortals();
        }

        public void ShowStarSystemFunc(VirtualMachine vm, ArgumentsValue args)
        {
            var starSystemId = new StarSystemId(args.GetIndexString(0));
            if (GameManager.Instance.TryGetStarSystem(starSystemId, out var foundStarSystem))
            {
                if (foundStarSystem.ForceShowStarSystem(StarSystem.ForceShowType.Show))
                {
                    ProjectileManager.Instance.AddGravitySources(foundStarSystem);
                }
            }
        }

        public void HideStarSystemFunc(VirtualMachine vm, ArgumentsValue args)
        {
            var starSystemId = new StarSystemId(args.GetIndexString(0));
            if (GameManager.Instance.TryGetStarSystem(starSystemId, out var foundStarSystem))
            {
                if (foundStarSystem.ForceShowStarSystem(StarSystem.ForceShowType.Hide))
                {
                    ProjectileManager.Instance.RemoveGravitySources(foundStarSystem);
                }
            }
        }

        public void UnlockFunc(VirtualMachine vm, ArgumentsValue args)
        {
            var currentLevel = GameManager.Instance.CurrentLevel;
            if (currentLevel == null)
            {
                Debug.LogWarning($"No current level, cannot register unlock: {args}");
                return;
            }

            if (!args.TryGetIndex<IObjectValue>(0, out var value))
            {
                Debug.LogWarning($"unlock func expects an object value");
                return;
            }

            if (!LevelUnlock.TryParse(value, out var unlock))
            {
                Debug.LogWarning($"Unable to parse: {value} as level unlock");
                return;
            }

            currentLevel.LevelUnlocks.Add(unlock);
        }

        public void RegisterFunc(VirtualMachine vm, ArgumentsValue args)
        {
            var type = args.GetIndexString(0);
            var hasFunc = args.TryGetIndex<IFunctionValue>(1, out var func);

            if (hasFunc && func != null)
            {
                this.registeredFunctions[type] = func;
            }
            else
            {
                this.registeredFunctions.Remove(type);
            }
        }

        public void PauseGameFunc(VirtualMachine vm, ArgumentsValue args)
        {
            var paused = args.GetIndexBoolean(0);
            GameManager.Instance.GamePaused = paused;
        }

        public void GetStatFunc(VirtualMachine vm, ArgumentsValue args)
        {
            var statName = args.GetIndexString(0);
            var level = GameManager.Instance.CurrentLevel;
            if (level == null)
            {
                throw new Exception($"No current level to get stat {statName} from");
            }

            if (statName == "money")
            {
                vm.PushStack(level.Money);
                return;
            }
            else if (statName == "health")
            {
                vm.PushStack(level.PlanetCurrentHealth);
                return;
            }
            else if (statName == "maxHealth")
            {
                vm.PushStack(level.PlanetMaxHealth);
                return;
            }
            else if (statName == "shipsLeft")
            {
                vm.PushStack(level.ShipsLeft);
                return;
            }

            var message = $"Unknown level stat for VM: {statName}";
            Debug.LogError(message);
            throw new Exception(message);
        }

        public void PlayEffectFunc(VirtualMachine vm, ArgumentsValue args)
        {
            var effect = args.GetIndexString(0);
            var show = args.GetIndexBoolean(1);
            if (args.TryGetIndex(0, out BoolValue showValue))
            {
                show = showValue.Value;
            }

            var level = GameManager.Instance.CurrentLevel;
            var position = level.Player.Planet.transform.position;

            if (effect == "time")
            {
                SaturationEffect.Instance.SetShow(show, position, level.TimeControlVisualDistance);
            }
            else if (effect == "power")
            {
                var scale = level.Player.PlanetRadius * 1.2f;

                PowerUpEffect.Instance.SetShow(show, position, scale);
            }
            else if (effect == "split")
            {
                var scale = level.Player.PlanetRadius * 1.2f;

                SplitEffect.Instance.SetShow(show, position, scale);
            }
            else if (effect == "reverse-lightning")
            {
                LightningReverseEffect.Instance.SetShow(show);
            }
        }

        public void GetTimeFunc(VirtualMachine vm, ArgumentsValue args)
        {
            var t = Time.timeSinceLevelLoadAsDouble;
            vm.PushStack(t);
        }

        public void StartWaveFunc(VirtualMachine vm, ArgumentsValue args)
        {
            if (GameManager.Instance.CurrentLevel == null)
            {
                Debug.LogError($"No current level to start waves");
                return;
            }
            GameManager.Instance.CurrentLevel.StartWaves();
        }

        public void StartLevelFunc(VirtualMachine vm, ArgumentsValue args)
        {
            if (GameManager.Instance.CurrentLevel == null)
            {
                Debug.LogError($"No current level to start");
                return;
            }
            GameManager.Instance.CurrentLevel.StartLevel();
        }

        public void EndLevelFunc(VirtualMachine vm, ArgumentsValue args)
        {
            if (GameManager.Instance.CurrentLevel == null)
            {
                Debug.LogError($"No current level to end");
                return;
            }
            GameManager.Instance.LevelEnd(GameManager.Instance.CurrentLevel);
        }

        public void FinishLevelFunc(VirtualMachine vm, ArgumentsValue args)
        {
            if (GameManager.Instance.CurrentLevel == null)
            {
                Debug.LogError($"No current level to finish");
                return;
            }
            GameManager.Instance.LevelFinished(GameManager.Instance.CurrentLevel);
        }

        public void IsCharFunc(VirtualMachine vm, ArgumentsValue args)
        {
            if (args.ArrayValues.Count == 0 || this.currentCharacters.ArrayValues.Count == 0)
            {
                vm.PushStack(BoolValue.False);
                return;
            }

            foreach (var a in args.ArrayValues)
            {
                if (a is GameCharacterValue gameChar)
                {
                    if (!this.currentCharacters.ArrayValues.Contains(gameChar))
                    {
                        vm.PushStack(BoolValue.False);
                        return;
                    }
                }
            }

            vm.PushStack(BoolValue.True);
        }

        public void GetFlagFunc(VirtualMachine vm, ArgumentsValue args)
        {
            var key = args.GetIndexString(0);
            var hasFlag = PlayerState.Instance.HasGameFlag(key);
            vm.PushStack(hasFlag);
        }

        public void SetFlagFunc(VirtualMachine vm, ArgumentsValue args)
        {
            var key = args.GetIndexString(0);
            var value = true;
            if (args.TryGetIndex<BoolValue>(1, out var indexValue))
            {
                value = indexValue.Value;
            }

            PlayerState.Instance.SetGameFlag(key, value);
        }

        public void MoveToFunc(VirtualMachine vm, ArgumentsValue args)
        {
            var func = args.GetIndex<IFunctionValue>(0);
            vm.CallFunction(func, 0, false);

            if (args.Length > 1)
            {
                var label = args.GetIndex(1);
                vm.Jump(label.ToString());
            }
        }

        public void BeginLineFunc(VirtualMachine vm, ArgumentsValue args)
        {
            this.CurrentTalkingActorPosition = args.GetIndexEnum(0, ActorPosition.Unknown);
            UICharacterPortraitManager.Instance.CurrentTalkingActorPosition = this.CurrentTalkingActorPosition;

            this.DoBeginLine();
        }

        public void EndLineFunc(VirtualMachine vm, ArgumentsValue args)
        {
            this.DoEndLine();
        }

        private void LineFunc(VirtualMachine vm, ArgumentsValue args)
        {
            this.CurrentTalkingActorPosition = args.GetIndexEnum(0, ActorPosition.Unknown);
            UICharacterPortraitManager.Instance.CurrentTalkingActorPosition = this.CurrentTalkingActorPosition;

            var text = string.Join("", args.Value.Skip(1));

            this.DoBeginLine();
            this.DoText(text);
            this.DoEndLine();
        }

        private void TextFunc(VirtualMachine vm, ArgumentsValue args)
        {
            var text = string.Join("", args.Value);
            this.DoText(text);
        }

        private void ActorFunc(VirtualMachine vm, ArgumentsValue args)
        {
            var position = args.GetIndexEnum(0, ActorPosition.Unknown);
            if (args.TryGetIndex<IObjectValue>(1, out var values))
            {
                if (values.TryGetKey("actor", out GameCharacterValue actor))
                {
                    if (position == ActorPosition.Left)
                    {
                        this.LeftActor = new ActorInfo(actor.Value);
                    }
                    else if (position == ActorPosition.Right)
                    {
                        this.RightActor = new ActorInfo(actor.Value);
                    }
                    UICharacterPortraitManager.Instance.ShowCharacterDialogue(actor.Value, position);
                }

                if (values.TryGetValue("emotion", out string emotion))
                {
                    if (this.TryGetActorAtPosition(position, out var actorInfo))
                    {
                        actorInfo.Emotion = emotion;
                    }
                }
            }
            else
            {
                if (position == ActorPosition.Left)
                {
                    this.LeftActor = null;
                }
                else if (position == ActorPosition.Right)
                {
                    this.RightActor = null;
                }
            }

            if (this.TryGetActorAtPosition(position, out var changedActor))
            {
                this.OnActorChange?.Invoke(position, changedActor);
            }
            else
            {
                this.OnActorChange?.Invoke(position, null);
            }
        }

        private void ChoiceFunc(VirtualMachine vm, ArgumentsValue args)
        {
            var choiceLabel = args.GetIndex<StringValue>(0);
            var choiceValue = args.GetIndex(1);
            this.CreateChoice(choiceLabel.ToString(), choiceValue);
        }

        private void EmotionFunc(VirtualMachine vm, ArgumentsValue args)
        {
            var emotion = args.GetIndex(0).ToString();
            this.DoEmotion(emotion);
        }

        private void DoBeginLine()
        {
            this.choiceBuffer.Clear();
            this.OnSectionChange?.Invoke(SectionType.NewLine);
        }

        private void DoEndLine()
        {
            vm.Paused = true;
            this.OnSectionChange?.Invoke(this.choiceBuffer.Count > 0 ? SectionType.ForChoices : SectionType.ToContinue);
        }

        private void DoEmotion(string emotion)
        {
            this.OnEmotion?.Invoke(emotion);
        }

        // private void DoActor(GameCharacterValue actor, ActorPosition position)
        // {
        //     this.CurrentTalkingActorPosition = position;

        //     if (position == ActorPosition.Left)
        //     {
        //         this.LeftActor = actor.Value;
        //     }
        //     else if (position == ActorPosition.Right)
        //     {
        //         this.RightActor = actor.Value;
        //     }
        // }

        private void DoText(string text)
        {
            this.OnTextSegment?.Invoke(text);
        }

        private void WaitFunc(VirtualMachine vm, ArgumentsValue args)
        {
            var waitTime = TimeSpan.FromMilliseconds(args.GetIndex<NumberValue>(0).Value);
            this.VMRunner.Wait(waitTime);
        }
    }
}
