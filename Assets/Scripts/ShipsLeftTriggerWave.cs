using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#nullable enable

namespace Orbits
{
    public class ShipsLeftTriggerWave : WaveTrigger
    {
        #region Fields
        public List<Wave> Targets = new();
        public List<Behaviour> ToEnable = new();
        public int TargetShipsLeft = 0;
        public float Delay = 0;

        private List<Wave> targetsLeft = new();
        #endregion

        #region Unity Methods
        private void Awake()
        {
            this.targetsLeft = this.Targets.ToList();
            foreach (var target in this.targetsLeft)
            {
                target.OnShipDestroyed += this.OnShipDestroyed;
            }
        }

        private void OnDestroy()
        {
            foreach (var target in this.Targets)
            {
                target.OnShipDestroyed -= this.OnShipDestroyed;
            }
        }
        #endregion

        #region Methods
        private void OnShipDestroyed(Wave source, Target target, WaveShipController waveShipController)
        {
            if (this.Triggered || WaveShipManager.Instance.Clearing)
            {
                this.enabled = false;
                return;
            }

            if (source.NumShipsLeft <= this.TargetShipsLeft)
            {
                source.OnShipDestroyed -= this.OnShipDestroyed;
                this.targetsLeft.Remove(source);
            }

            if (this.targetsLeft.Count == 0)
            {
                this.Triggered = true;
                this.enabled = false;
                if (this.Delay > 0)
                {
                    StartCoroutine(this.WaitToStart());
                }
                else
                {
                    this.StartWave();
                }
            }
        }

        private IEnumerator WaitToStart()
        {
            yield return new WaitForSeconds(this.Delay);
            this.StartWave();
        }

        private void StartWave()
        {
            foreach (var comp in this.ToEnable)
            {
                comp.enabled = true;
            }
        }
        #endregion
    }
}