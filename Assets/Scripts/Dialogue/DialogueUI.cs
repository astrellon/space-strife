using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using RichTextSubstringHelper;

namespace Orbits
{
    public class DialogueUI : MonoBehaviour
    {
        public TMP_Text DialogueText;
        public TMP_Text CharNameText;
        public LevelVM DialogueVM;
        public DialogueChoiceUI ChoicePrefab;
        public Transform ChoiceTarget;
        public GameObject ShowNextGraphic;

        private RichTextSubStringMaker textMaker;
        private float textShowCooldown = 0.0f;

        // Start is called before the first frame update
        void Start()
        {
            this.gameObject.SetActive(false);
        }

        void Update()
        {
            this.textShowCooldown = Mathf.Max(0.0f, this.textShowCooldown - Time.unscaledDeltaTime);
            if (UIManager.ContinueButtonsPressed())
            {
                if (this.textMaker.IsConsumable())
                {
                    this.DialogueText.text = this.textMaker.OriginalText;
                    this.textMaker.Finish();
                }
                else
                {
                    this.ContinueDialogue();
                }
            }
            else
            {
                if (this.textShowCooldown <= 0.0f && this.textMaker.IsConsumable())
                {
                    if (this.DialogueVM.CurrentTalkingActor != null &&
                        this.DialogueVM.CurrentTalkingActor.Actor != null &&
                        this.DialogueVM.CurrentTalkingActor.Actor.CharacterTalk != null)
                    {
                        AudioManager.PlayOneOffDialogue(this.DialogueVM.CurrentTalkingActor.Actor.CharacterTalk, Vector3.zero);
                    }

                    this.textMaker.Consume();
                    var text = this.textMaker.GetRichText();
                    var lastChar = text.Length > 0 ? text.Last() : ' ';
                    this.DialogueText.text = text;

                    if (lastChar == '.' || lastChar == '!' || lastChar == '?' || lastChar == ',')
                    {
                        this.textShowCooldown = 0.35f;
                    }
                    else
                    {
                        this.textShowCooldown = 1.0f / 30.0f;
                    }
                }
            }
        }

        public void OnEmotion(string emotion)
        {
            // switch (emotion)
            // {
            //     case "shocked":
            //     {
            //         this.Portrait.sprite = this.DialogueVM.CurrentActor.FaceShock;
            //         break;
            //     }
            //     case "happy":
            //     {
            //         this.Portrait.sprite = this.DialogueVM.CurrentActor.FaceHappy;
            //         break;
            //     }
            //     case "sad":
            //     {
            //         this.Portrait.sprite = this.DialogueVM.CurrentActor.FaceSad;
            //         break;
            //     }
            //     case "idle":
            //     {
            //         this.Portrait.sprite = this.DialogueVM.CurrentActor.FaceIdle;
            //         break;
            //     }
            //     default:
            //     {
            //         Debug.Log($"Unknown emotion: {emotion}");
            //         this.Portrait.sprite = this.DialogueVM.CurrentActor.FaceIdle;
            //         break;
            //     }
            // }
        }

        public void OnTextSegment(string text)
        {
            this.textMaker.AppendText(text);
        }

        public void OnShowChoice(string text, int index)
        {
            var newChoice = Instantiate(this.ChoicePrefab, this.ChoiceTarget);
            newChoice.ChoiceText = text;
            newChoice.ChoiceIndex = index;
            newChoice.LevelVM = this.DialogueVM;
        }

        public void OnSectionChange(LevelVM.SectionType waitType)
        {
            if (waitType == LevelVM.SectionType.NewLine)
            {
                this.OnBeginLine();
            }
            else if (waitType == LevelVM.SectionType.DialogueEnded)
            {
                this.gameObject.SetActive(false);
            }
            else
            {
                this.ShowNextGraphic.SetActive(waitType == LevelVM.SectionType.ToContinue);
            }
        }

        public void OnBeginLine()
        {
            this.gameObject.SetActive(true);
            this.Clear();

            var actor = this.DialogueVM.CurrentTalkingActor;
            if (actor != null)
            {
                var alignment = this.DialogueVM.CurrentTalkingActorPosition == ActorPosition.Left ?
                    TextAlignmentOptions.TopLeft : TextAlignmentOptions.TopRight;
                this.CharNameText.alignment = alignment;
                this.CharNameText.text = actor.Actor.NameWithColour;
            }
        }

        private void Clear()
        {
            this.DialogueText.text = "";
            this.CharNameText.text = "";
            this.textMaker = new RichTextSubStringMaker("");

            var children = new List<GameObject>();
            foreach (Transform child in this.ChoiceTarget) children.Add(child.gameObject);
            children.ForEach(child => Destroy(child));
        }

        public void ContinueDialogue()
        {
            this.DialogueVM.Continue();
        }
    }
}
