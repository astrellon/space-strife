using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LysitheaVM.Unity
{
    public class VirtualMachineStatusUI : MonoBehaviour
    {
        public VMRunnerUI VMRunnerUI;
        public Color OnColour;
        public Color OffColour;
        public Image WaitingImage;
        public Image RunningImage;
        public Image PausedImage;

        // Update is called once per frame
        void Update()
        {
            this.WaitingImage.color = this.GetColour(this.VMRunnerUI.VMRunner.IsWaiting);

            var vm = this.VMRunnerUI.VM;
            this.RunningImage.color = this.GetColour(vm != null ? vm.Running : false);
            this.PausedImage.color = this.GetColour(vm != null ? vm.Paused : false);
        }

        private Color GetColour(bool input)
        {
            return input ? this.OnColour : this.OffColour;
        }
    }
}
