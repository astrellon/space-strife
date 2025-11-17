using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#nullable enable

namespace Orbits
{
    public class TankShipEngine
    {
        #region Fields
        public readonly ParticleSystem Graphic;

        private readonly Vector3 angularNormal;
        private readonly Transform transform;

        private float currentFrameThrust = 0.0f;
        #endregion

        #region Constructor
        public TankShipEngine(ParticleSystem graphic)
        {
            this.Graphic = graphic;
            this.transform = graphic.transform;
            var xzPosition = new Vector3(this.transform.localPosition.x, 0, this.transform.localPosition.z).normalized;
            this.angularNormal = Vector3.Cross(this.transform.forward, xzPosition);
        }
        #endregion

        #region Methods
        public void ResetThrust()
        {
            this.currentFrameThrust = 0.0f;
        }

        public void SetThrust(float thrust)
        {
            if (thrust > Mathf.Epsilon && !this.Graphic.isPlaying)
            {
                this.Graphic.Play(withChildren: true);
            }
            else if (thrust <= Mathf.Epsilon && this.Graphic.isPlaying)
            {
                this.Graphic.Stop(withChildren: true);
            }
        }

        public void SetLinearThrustFromInput(Vector3 inputGlobal)
        {
            var graphicForward = -this.Graphic.transform.forward;
            var thrust = Vector3.Dot(graphicForward, inputGlobal);
            this.currentFrameThrust = Mathf.Max(this.currentFrameThrust, thrust);
        }

        public void SetAngularThrustFromInput(float angularInput)
        {
            var torqueInput = new Vector3(0.0f, -angularInput, 0.0f).normalized;
            var thrust = Vector3.Dot(torqueInput, this.angularNormal);
            this.currentFrameThrust = Mathf.Max(this.currentFrameThrust, thrust);
        }

        public void PostInputUpdate()
        {
            this.SetThrust(this.currentFrameThrust);
        }

        public void Clear()
        {
            this.Graphic.Stop(withChildren: true);
            this.Graphic.Clear(withChildren: true);
        }

        public static TankShipEngine Create(ParticleSystem graphic) => new(graphic);
        #endregion
    }
}