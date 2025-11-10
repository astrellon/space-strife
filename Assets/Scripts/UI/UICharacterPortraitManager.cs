using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

#nullable enable

namespace Orbits
{
    public class CharacterPortraitLocation
    {
        private static readonly Vector3 OffScreen = new(-200, -200, 0);
        public readonly GameCharacter ForCharacter;
        public readonly LerpTransform PositionLerp;
        public readonly LerpFloat AlphaLerp;
        public readonly LerpFloat RotationLerp;

        public UICharacterPortrait2 Portrait;
        public bool IsOffScreen = true;

        public CharacterPortraitLocation(GameCharacter forCharacter, UICharacterPortrait2 portrait)
        {
            this.ForCharacter = forCharacter;
            this.Portrait = portrait;
            portrait.Image.sprite = forCharacter.Portrait;
            var easing = Easing.Map[forCharacter.PortraitEasing];

            this.PositionLerp = new() { Easing = easing };
            this.PositionLerp.LerpTo(null, OffScreen, t: 1);
            this.PositionLerp.Value = OffScreen;
            this.AlphaLerp = new() { Easing = easing };
            this.RotationLerp = new () { Easing = easing };
        }

        public void Update(float dt)
        {
            this.PositionLerp.Update(dt);

            var pos = this.PositionLerp.Value;
            this.IsOffScreen = pos.x < -128.0f || pos.y < -128.0f ||
                pos.x > Screen.width + 128.0f ||
                pos.y > Screen.height + 128.0f;

            this.Portrait.Rect.position = pos;

            this.AlphaLerp.Update(dt);
            this.Portrait.SetAlpha(this.AlphaLerp.Value);

            this.RotationLerp.Update(dt);
            this.Portrait.SetRotation(this.RotationLerp.Value);
        }

        public void LerpAlpha(float target, float lerpFrom = -1f, float lerpSpeed = 0.5f, float lerpTime = 0.0f)
        {
            this.AlphaLerp.LerpTo(target, lerpFrom, lerpSpeed, lerpTime);
        }

        public void LerpTo(RectTransform? target, float lerpSpeed = 1.0f, float lerpTime = 0.0f, Vector3? lerpFrom = null)
        {
            this.PositionLerp.LerpTo(target, lerpFrom, lerpSpeed, lerpTime);
        }
    }

    public class UICharacterPortraitManager : MonoBehaviour
    {
        #region Fields

        public static UICharacterPortraitManager Instance { get; private set; }
        public UICharacterPortrait2? PortraitPrefab;
        public float InactiveAlpha = 0.4f;
        public float MinimumThresholdToUseMidOffset = 400.0f;
        public RectTransform? LeftPortraitTarget;
        public RectTransform? RightPortraitTarget;
        public RectTransform? LeftOffScreenPortraitTarget;
        public RectTransform? RightOffScreenPortraitTarget;
        public GameCharacter? LeftActor { get; private set; }
        public GameCharacter? RightActor { get; private set; }
        public ActorPosition CurrentTalkingActorPosition
        {
            get => this.currentTalkingActorPosition;
            set
            {
                if (this.currentTalkingActorPosition != value)
                {
                    if (this.TryGetActorAtPosition(this.currentTalkingActorPosition, out var currentlyTalking))
                    {
                        this.portraitLocations[currentlyTalking.Id].LerpAlpha(this.InactiveAlpha);
                    }

                    this.currentTalkingActorPosition = value;

                    if (this.TryGetActorAtPosition(this.currentTalkingActorPosition, out var nowCurrentlyTalking))
                    {
                        this.portraitLocations[nowCurrentlyTalking.Id].LerpAlpha(1.0f);
                    }
                }
            }
        }
        public GameCharacter? CurrentTalkingActor
        {
            get
            {
                return this.currentTalkingActorPosition switch
                {
                    ActorPosition.Left => this.LeftActor,
                    ActorPosition.Right => this.RightActor,
                    _ => null,
                };
            }
        }
        private ActorPosition currentTalkingActorPosition = ActorPosition.Unknown;
        private readonly Dictionary<GameCharacterId, CharacterPortraitLocation> portraitLocations = new();
        #endregion

        #region Unity Methods
        private void Start()
        {
            Instance = this;

            foreach (var gameChar in GameManager.Instance.Characters)
            {
                var portrait = Instantiate(this.PortraitPrefab, this.transform);
                this.portraitLocations[gameChar.Id] = new CharacterPortraitLocation(gameChar, portrait);
            }

            UIManager.Instance.LevelVM.OnSectionChange += this.OnDialogueSectionChange;
        }

        private void OnDialogueSectionChange(LevelVM.SectionType sectionType)
        {
            if (sectionType == LevelVM.SectionType.DialogueEnded)
            {
                this.ShowCharacterDialogue(null, ActorPosition.Left);
                this.ShowCharacterDialogue(null, ActorPosition.Right);
            }
        }

        private void Update()
        {
            var dt = Time.unscaledDeltaTime;
            foreach (var kvp in this.portraitLocations)
            {
                kvp.Value.Update(dt);
            }
        }
        #endregion

        #region Methods
        public void ShowCharacter(GameCharacterId gameCharacterId, RectTransform target, Vector3? positionIfOffScreen)
        {
            var location = this.portraitLocations[gameCharacterId];
            if (location.IsOffScreen)
            {
                location.LerpTo(target, 1, 1, lerpFrom: positionIfOffScreen);
            }
            else
            {
                location.LerpTo(target);
            }
            location.LerpAlpha(1.0f);
            location.RotationLerp.LerpTo(0, 0, t: 1);
        }

        public void MarkOffScreenIfFollowingTarget(GameCharacter gameCharacter, RectTransform target)
        {
            var location = this.portraitLocations[gameCharacter.Id];
            if (location.PositionLerp.To == target)
            {
                location.IsOffScreen = true;
            }
        }

        public void ShowCharacterDialogue(GameCharacter? gameCharacter, ActorPosition position)
        {
            if (this.TryGetActorAtPosition(position, out var currentChar))
            {
                if (currentChar == gameCharacter)
                {
                    return;
                }

                if (this.TryGetOffScreenAtPosition(position, out var offScreen))
                {
                    var location = this.portraitLocations[currentChar.Id];
                    location.LerpTo(offScreen);
                    location.PositionLerp.SetMidOffset(Vector3.zero);
                    location.LerpAlpha(0.0f);
                }
            }

            this.SetActorAtPosition(gameCharacter, position);

            if (gameCharacter != null && this.TryGetDialogueAtPosition(position, out var portraitTarget))
            {
                var location = this.portraitLocations[gameCharacter.Id];
                var targetRotation = position == ActorPosition.Right ? 180.0f : 0.0f;
                if (location.IsOffScreen)
                {
                    location.RotationLerp.LerpTo(targetRotation, targetRotation, t: 1);
                    if (this.TryGetOffScreenAtPosition(position, out var offScreen))
                    {
                        location.LerpTo(portraitTarget, lerpFrom: offScreen.position, lerpSpeed: 1.5f);
                        location.PositionLerp.SetMidOffset(Vector3.zero);
                    }
                }
                else
                {
                    var distance = Vector3.Distance(location.PositionLerp.Value, portraitTarget.position);
                    location.RotationLerp.LerpTo(targetRotation);
                    location.LerpTo(portraitTarget, lerpSpeed: 1.5f);
                    if (distance < this.MinimumThresholdToUseMidOffset)
                    {
                        location.PositionLerp.SetMidOffset(Vector3.zero);
                    }
                    else
                    {
                        location.PositionLerp.SetMidOffset(new Vector3(0, distance * 0.1f, 0));
                    }
                }

                var alpha = this.CurrentTalkingActor == location.ForCharacter ? 1.0f : this.InactiveAlpha;
                location.LerpAlpha(alpha);
            }
        }

        public bool SetActorAtPosition(GameCharacter? gameCharacter, ActorPosition position)
        {
            if (position == ActorPosition.Left)
            {
                this.LeftActor = gameCharacter;
                if (this.RightActor == gameCharacter)
                {
                    this.RightActor = null;
                }
                return true;
            }
            else if (position == ActorPosition.Right)
            {
                this.RightActor = gameCharacter;
                if (this.LeftActor == gameCharacter)
                {
                    this.LeftActor = null;
                }
                return true;
            }

            return false;
        }

        public bool TryGetOffScreenAtPosition(ActorPosition position, [NotNullWhen(true)] out RectTransform? result)
        {
            result = position switch
            {
                ActorPosition.Left => this.LeftOffScreenPortraitTarget,
                ActorPosition.Right => this.RightOffScreenPortraitTarget,
                _ => null
            };

            return result != null;
        }

        public bool TryGetDialogueAtPosition(ActorPosition position, [NotNullWhen(true)] out RectTransform? result)
        {
            result = position switch
            {
                ActorPosition.Left => this.LeftPortraitTarget,
                ActorPosition.Right => this.RightPortraitTarget,
                _ => null
            };

            return result != null;
        }

        public bool TryGetActorAtPosition(ActorPosition position, [NotNullWhen(true)] out GameCharacter? result)
        {
            result = position switch
            {
                ActorPosition.Left => this.LeftActor,
                ActorPosition.Right => this.RightActor,
                _ => null,
            };

            return result != null;
        }
        #endregion
    }
}