using System;
using UnityEngine;

#nullable enable

namespace Orbits
{
    public class UITankSelect : MonoBehaviour
    {
        #region Fields
        public UITankUpgrade Parent;
        public int TankIndex = -1;
        private RectTransform? rect;
        #endregion

        #region Unity Methods
        private void Start()
        {
            this.rect = this.GetComponent<RectTransform>();
            GameManager.Instance.OnLevelLoad += this.OnLevelLoad;
        }

        private void OnDestroy()
        {
            GameManager.Instance.OnLevelLoad -= this.OnLevelLoad;
        }

        private void Update()
        {
            this.DoUpdate();
        }
        #endregion

        #region Methods
        public void SelectTank()
        {
            this.Parent.SetTankIndex(this.TankIndex);
        }

        private void OnLevelLoad(Level level)
        {
            this.DoUpdate();
        }

        private void DoUpdate()
        {
            var setActive = false;
            if (this.rect != null && GameManager.Instance.CurrentLevel != null)
            {
                var radius = 1.5f;
                if (GameManager.Instance.CurrentLevel.Container.Ship != null)
                {
                    radius = 2.5f;
                }

                if (GameManager.Instance.CurrentLevel.Player.TryCalculateGlobalPosition(this.TankIndex, radius, out var position))
                {
                    setActive = true;
                    this.gameObject.SetActive(true);
                    var combinedPosition = position;
                    var screenPosition = Camera.main.WorldToScreenPoint(combinedPosition);
                    this.rect.position = screenPosition;
                }
            }

            this.gameObject.SetActive(setActive);
        }
        #endregion
    }
}