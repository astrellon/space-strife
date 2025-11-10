using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

#nullable enable

namespace Orbits
{
    public class OpeningScene : MonoBehaviour
    {
        #region Fields
        public GameCamera? Camera;
        public Volume? Volume;
        public LiftGammaGain? GainEffect;
        public Vector3 StartingPosition;
        public Vector3 TargetPosition;
        public float OpeningTime = 5.0f;
        public int FirstFrameCounter = 2;

        public List<GameObject> ToTrigger = new();
        public float ToTriggerTime = 4.0f;
        private bool triggered = false;
        private CameraTransition transition = CameraTransition.CreateEmpty;
        #endregion

        public void ForceFinish()
        {
            this.DoTrigger();
            this.ShowUI();
        }

        #region Unity Methods
        private void Start()
        {
            if (this.Camera != null)
            {
                this.TargetPosition = this.Camera.transform.position;
                this.Camera.SetPosition(this.StartingPosition);
            }

            if (this.Volume != null)
            {
                this.Volume.profile.TryGet<LiftGammaGain>(out this.GainEffect);
                if (this.GainEffect != null)
                {
                    this.SetGamma(0);
                }
            }
        }

        private void Update()
        {
            this.FirstFrameCounter--;
            if (this.FirstFrameCounter == 0)
            {
                if (this.Camera != null)
                {
                    this.transition = this.Camera.TransitionTo(this.StartingPosition, this.TargetPosition, this.OpeningTime, Easing.Quadratic.Out);
                }
                return;
            }

            if (this.FirstFrameCounter >= 0)
            {
                return;
            }

            if (UIManager.ContinueButtonsPressed())
            {
                this.ForceFinish();
            }

            if (this.transition.CurrentTime >= this.ToTriggerTime)
            {
                this.DoTrigger();
            }

            this.SetGamma(this.transition.Lerped);

            if (this.transition.IsComplete)
            {
                this.gameObject.SetActive(false);
                this.ShowUI();
            }
        }

        private void SetGamma(float t)
        {
            if (this.GainEffect != null)
            {
                this.GainEffect.gamma.Override(new Vector4(1.0f, 1.0f, 1.0f, -1.0f + t));
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(this.StartingPosition, 1.0f);

            if (this.Camera != null)
            {
                Gizmos.DrawLine(this.StartingPosition, this.Camera.transform.position);
            }
        }

        private void DoTrigger()
        {
            if (this.triggered)
            {
                return;
            }

            this.triggered = true;
            foreach (var item in this.ToTrigger)
            {
                item.SetActive(true);
            }
        }

        private void ShowUI()
        {
            if (UIManager.Instance.State == InterfaceState.MainMenuOpening)
            {
                UIManager.Instance.State = InterfaceState.MainMenu;
            }
        }
        #endregion
    }
}