using System;
using System.Linq;
using UnityEngine;

#nullable enable

namespace Orbits
{
    public class TankSelector : MonoBehaviour
    {
        #region Fields
        public MeshRenderer Renderer;
        public UITankUpgrade Parent;
        public float MaxAlpha = 0.4f;

        private float alphaVelocity = 0;
        private float alpha = 0;
        #endregion

        #region Unity Methods
        private void Start()
        {
            this.MaxAlpha = this.Renderer.material.color.a;
        }

        private void Update()
        {
            var level = GameManager.Instance.CurrentLevel;
            if (UIManager.Instance.State == InterfaceState.Equipment && level != null && level.Player.TryCalculateGlobalPosition(this.Parent.TankIndex, -1.5f, out var globalPosition, out var globalRotation))
            {
                var newPosition = level.Player.Planet.transform.position;
                var scaledDt = Time.unscaledDeltaTime * 12.0f;

                if (this.alpha > 0.01f)
                {
                    var targetPosition = new Vector3(globalPosition.x, 0, globalPosition.z);
                    newPosition = Vector3.Slerp(this.transform.position, targetPosition, scaledDt);
                }

                var targetAngle = globalRotation.eulerAngles.y + 90.0f;
                var targetRotation = Quaternion.Euler(0, targetAngle, 0);
                var newRotation = Quaternion.Slerp(this.transform.rotation, targetRotation, scaledDt);

                this.transform.SetPositionAndRotation(newPosition, newRotation);

                this.alpha = UIManager.EquipmentSmoothDamp(this.alpha, this.MaxAlpha, ref this.alphaVelocity);
            }
            else
            {
                this.alpha = UIManager.EquipmentSmoothDamp(this.alpha, 0.0f, ref this.alphaVelocity);
            }

            this.Renderer.material.color = Utils.ColourAlpha(this.Renderer.material.color, this.alpha);
        }
        #endregion

        #region Methods
        private void UpdatePosition()
        {
            if (GameManager.Instance.CurrentLevel != null &&
                GameManager.Instance.CurrentLevel.Player.Planet != null)
            {
                this.transform.position = GameManager.Instance.CurrentLevel.Player.Planet.transform.position - Vector3.up;
            }
        }
        #endregion
    }
}