using System;
using UnityEngine;
using UnityEngine.UI;

#nullable enable

namespace Orbits
{
    public class UICharacterPortrait : MonoBehaviour
    {
        #region Fields
        public static readonly Color Selected = new(1, 1, 1, 1);
        public static readonly Color Unselected = new(1, 1, 1, 0.4f);
        public Image Image;
        public ActorPosition ForCharacter;
        public Vector3 InactiveOffset = Vector3.zero;
        public LevelVM LevelVM;
        #endregion

        #region Methods
        public void SetAlpha(float alpha)
        {
            this.Image.color = Utils.ColourAlpha(Color.white, alpha);
        }

        private void Start()

        {
            if (this.LevelVM.TryGetActorAtPosition(this.ForCharacter, out var actorInfo))
            {
                this.UpdateSprite(actorInfo);
            }

            this.LevelVM.OnActorChange += this.OnActorChange;
            this.LevelVM.OnTextSegment += this.OnTextChange;
        }

        private void OnDestroy()
        {
            this.LevelVM.OnActorChange -= this.OnActorChange;
        }

        private void OnTextChange(string text)
        {
            if (this.LevelVM.TryGetActorAtPosition(this.ForCharacter, out var actorInfo))
            {
                this.UpdateSprite(actorInfo);
            }
        }

        private void OnActorChange(ActorPosition position, ActorInfo? actorInfo)
        {
            if (position == this.ForCharacter)
            {
                this.UpdateSprite(actorInfo);
            }
        }

        private void UpdateSprite(ActorInfo? actorInfo)
        {
            if (actorInfo != null)
            {
                this.Image.enabled = true;
                this.Image.sprite = actorInfo.Actor.Portrait;

                if (this.LevelVM.CurrentTalkingActorPosition == this.ForCharacter)
                {
                    this.Image.color = Selected;
                }
                else
                {
                    this.Image.color = Unselected;
                }
            }
            else
            {
                this.Image.enabled = false;
            }
        }
        #endregion
    }
}