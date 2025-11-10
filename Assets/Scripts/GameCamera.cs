using System;
using System.Threading;
using UnityEngine;

#nullable enable

namespace Orbits
{
    [Serializable]
    public class CameraTransition
    {
        public Vector3 StartPosition;
        public Vector3 EndPosition;
        public GameObject? Target;
        public float Time = -1.0f;
        public float TimePercent;
        public float CurrentTime;
        public Easing.Function EasingType = Easing.Quadratic.InOut;
        public bool OnFinishChangeFocus;

        public float Lerped;

        public bool IsComplete => this.CurrentTime >= this.Time;
        public bool IsEmpty => this.Time <= -1.0f;

        public static CameraTransition CreateEmpty => new();

        public Vector3 Evaluate(float dt)
        {
            this.CurrentTime += dt;
            this.TimePercent = Mathf.Clamp01(this.CurrentTime / this.Time);

            var target = this.Target != null ? this.Target.transform.position : this.EndPosition;
            this.Lerped = this.EasingType.Invoke(this.TimePercent);
            return Vector3.Lerp(this.StartPosition, target, this.Lerped);
        }

        public void Finish()
        {
            this.CurrentTime = this.Time;
            this.TimePercent = 1.0f;
            this.Lerped = this.EasingType.Invoke(1.0f);
        }
    }

    [DefaultExecutionOrder(1)]
    public class GameCamera : MonoBehaviour
    {
        #region Fields
        public static GameCamera Instance;
        public static Camera? MainCamera => Instance != null ? Instance.Camera : null;

        public float InGameHeightOffset = 40.0f;
        public float Zoom;
        public float PlayerZoomOffset;
        public float HeightOffset;
        public float ZoomOverride = 0.0f;
        public float MouseOffsetScale = 1.0f;
        public bool EnableFollowMovement = false;
        public bool EnablePlayerZoom = false;
        public bool SmoothFollow = false;
        public Camera? Camera;
        public GameObject? FocusOn;
        public GameObject? FocusOnOverride;
        public float MouseMoveAmount = 1.0f;

        public CameraTransition Transition
        {
            get
            {
                return this.transition;
            }
            set
            {
                if (this.transition != value)
                {
                    if (this.transition != null)
                    {
                        this.transition.Finish();
                    }
                    this.transition = value;
                }
            }
        }
        private CameraTransition transition;

        private float zoomVelocity;
        private float posXVelocity;
        private float posYVelocity;
        private float posZVelocity;
        private Vector3 cameraPos;
        private float mouseOffsetScaleVelocity;

        public Action? AfterCameraUpdate;
        #endregion

        #region Unity Methods
        private void Awake()
        {
            Instance = this;

            this.OnOrientationChange(GameManager.Instance.IsLandscape);

            GameManager.Instance.OnOrientationChange += this.OnOrientationChange;
        }

        private void OnDestroy()
        {
            GameManager.Instance.OnOrientationChange -= this.OnOrientationChange;
        }

        private void LateUpdate()
        {
            if (this.Camera == null)
            {
                return;
            }

            var zoomTarget = this.ZoomOverride > 0.0f ? this.ZoomOverride : (this.Zoom + this.PlayerZoomOffset);
            this.HeightOffset = UIManager.EquipmentSmoothDamp(this.HeightOffset, zoomTarget, ref this.zoomVelocity);

            if (this.EnableFollowMovement)
            {
                var mouseOffset = GameManager.Instance.MouseTouchPosition;
                mouseOffset.x = (mouseOffset.x / Screen.width - 0.5f) * this.MouseMoveAmount;
                mouseOffset.z = (mouseOffset.y / Screen.height - 0.5f) * this.MouseMoveAmount;
                mouseOffset.y = 0.0f;

                this.MouseOffsetScale = UIManager.EquipmentSmoothDamp(this.MouseOffsetScale, 1.0f, ref this.mouseOffsetScaleVelocity);
                this.Camera.transform.localPosition = mouseOffset * this.MouseOffsetScale + new Vector3(0, this.HeightOffset, 0);
            }
            else
            {
                this.Camera.transform.localPosition = new Vector3(0, this.HeightOffset, 0);
            }

            var pos = this.transform.position;
            if (this.Transition != null && !this.Transition.IsEmpty)
            {
                pos = this.Transition.Evaluate(Time.unscaledDeltaTime);
                if (this.Transition.IsComplete)
                {
                    if (this.Transition.OnFinishChangeFocus)
                    {
                        this.FocusOn = this.Transition.Target;
                    }

                    this.Transition = CameraTransition.CreateEmpty;
                }
            }
            else if (this.FocusOnOverride != null)
            {
                pos = this.FocusOnOverride.transform.position;
            }
            else if (this.FocusOn != null)
            {
                pos = this.FocusOn.transform.position;
            }

            if (this.SmoothFollow)
            {
                var smoothX = UIManager.EquipmentSmoothDamp(this.cameraPos.x, pos.x, ref this.posXVelocity);
                var smoothY = UIManager.EquipmentSmoothDamp(this.cameraPos.y, pos.y, ref this.posYVelocity);
                var smoothZ = UIManager.EquipmentSmoothDamp(this.cameraPos.z, pos.z, ref this.posZVelocity);

                this.cameraPos = new Vector3(smoothX, smoothY, smoothZ);
            }
            else
            {
                this.cameraPos = pos;
            }

            this.transform.position = this.cameraPos;

            if (this.AfterCameraUpdate != null)
            {
                this.AfterCameraUpdate.Invoke();
                this.AfterCameraUpdate = null;
            }
        }

        public void SetPosition(Vector3 position)
        {
            this.transform.position = position;
            this.cameraPos = position;
        }

        public void SetFocusOn(GameObject? target)
        {
            if (!this.Transition.IsEmpty)
            {
                this.Transition = CameraTransition.CreateEmpty;
            }
            this.FocusOn = target;
            this.HeightOffset = this.Camera.transform.position.y;
        }

        public void ResetZoom()
        {
            this.cameraPos = this.Camera.transform.position;
            this.Zoom = 0;
            this.PlayerZoomOffset = 0.0f;
            this.HeightOffset = 0;
            this.zoomVelocity = 0;
        }

        public CameraTransition TransitionTo(Vector3 startingPosition, Vector3 endPosition, float time, Easing.Function? easingFunction = null)
        {
            return this.Transition = new CameraTransition()
            {
                StartPosition = startingPosition,
                EndPosition = endPosition,
                Time = time,
                EasingType = easingFunction ?? Easing.Quadratic.InOut
            };
        }

        public CameraTransition TransitionTo(Vector3 position, float time, Easing.Function? easingFunction = null)
        {
            return this.Transition = new CameraTransition()
            {
                StartPosition = this.Camera.transform.position,
                EndPosition = position,
                Time = time,
                EasingType = easingFunction ?? Easing.Quadratic.InOut
            };
        }

        public CameraTransition TransitionTo(GameObject target, float time, Easing.Function? easingFunction = null, bool focusOnComplete = true)
        {
            return this.Transition = new CameraTransition()
            {
                StartPosition = this.Camera.transform.position,
                Target = target,
                Time = time,
                EasingType = easingFunction ?? Easing.Quadratic.InOut,
                OnFinishChangeFocus = focusOnComplete
            };
        }
        #endregion

        #region Methods
        private void OnOrientationChange(bool landscape)
        {
            if (this.Camera != null)
            {
                var fov = 60.0f;
                if (!landscape)
                {
                    var aspect = (float)Screen.width / Screen.height;
                    if (aspect > 1.0f)
                    {
                        aspect = 1.0f / aspect;
                    }
                    fov = Camera.HorizontalToVerticalFieldOfView(60.0f, aspect);
                }

                this.Camera.fieldOfView = fov;
            }
        }
        #endregion
    }
}