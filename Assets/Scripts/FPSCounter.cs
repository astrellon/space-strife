using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Orbits
{
    public class FPSCounter : MonoBehaviour
    {
        public TMP_Text Text;
        public bool Enabled = false;

        private float[] fpsValues = new float[10];
        private int fpsCounter = 0;

        // Start is called before the first frame update
        void Start()
        {
            if (!this.Enabled)
            {
                this.Text.text = "";
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.O))
            {
                this.Enabled = !this.Enabled;
                if (!this.Enabled)
                {
                    this.Text.text = "";
                }
            }

            if (!this.Enabled)
            {
                return;
            }
            var fps = 1.0f / Time.unscaledDeltaTime;
            this.fpsValues[this.fpsCounter++] = fps;
            if (this.fpsCounter >= this.fpsValues.Length)
            {
                this.fpsCounter = 0;
            }

            var calculatedFps = this.CalculateFPS();
            this.Text.text = $"FPS: {calculatedFps.ToString("n2")}";
        }

        float CalculateFPS()
        {
            return this.fpsValues.Average();
        }
    }
}
