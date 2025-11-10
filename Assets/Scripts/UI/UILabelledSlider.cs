using TMPro;
using UnityEngine;
using UnityEngine.UI;

#nullable enable

namespace Orbits
{
    public class UILabelledSlider : MonoBehaviour
    {
        #region Fields
        public TMP_Text Label;
        public Slider Slider;
        public float DisplayMin = 0.0f;
        public float DisplayMax = 1.0f;
        public int DisplayPrecision = 1;
        public string Postfix = "";

        private string labelPrefix = "";
        #endregion

        #region Unity Methods
        private void Start()
        {
            this.labelPrefix = this.Label.text;

            this.UpdateLabel();
            this.Slider.onValueChanged.AddListener(this.OnSliderValueChange);
        }
        #endregion

        #region Methods
        public void OnSliderValueChange(float value)
        {
            this.UpdateLabel();
        }

        private void UpdateLabel()
        {
            var diff = this.DisplayMax - this.DisplayMin;
            var displayValue = this.Slider.value * diff + this.DisplayMin;
            var strValue = decimal.Round((decimal)displayValue, this.DisplayPrecision);
            this.Label.text = $"{this.labelPrefix}: {strValue}{this.Postfix}";
        }
        #endregion
    }
}