using UnityEngine;

#nullable enable

namespace Orbits
{
    public class UIFadeCanvasGroup : MonoBehaviour
    {
        #region Fields
        public CanvasGroup Group;
        public float Alpha = 0.0f;
        private float alphaVelocity = 0.0f;
        private bool ignoreAwake = false;
        #endregion

        #region Unity Methods
        private void Awake()
        {
            if (!this.ignoreAwake)
            {
                this.Group.alpha = 0;
                this.gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            this.Group.alpha = UIManager.EquipmentSmoothDamp(this.Group.alpha, this.Alpha, ref this.alphaVelocity);
            if (this.Group.alpha < 0.001f)
            {
                this.gameObject.SetActive(false);
                return;
            }

            if (this.Group.alpha < 0.1)
            {
                this.Group.interactable = false;
                this.Group.blocksRaycasts = false;
            }
            else
            {
                this.Group.interactable = true;
                this.Group.blocksRaycasts = true;
            }
        }

        public void SetAlpha(float alpha)
        {
            alpha = Mathf.Clamp01(alpha);
            if (alpha == this.Alpha)
            {
                return;
            }

            this.ignoreAwake = true;
            this.Alpha = alpha;
            this.gameObject.SetActive(true);
        }
        #endregion
    }
}