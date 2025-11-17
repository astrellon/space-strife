using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#nullable enable

namespace Orbits
{
    public class TankShip : ITankContainer, IReset
    {

        #region Fields
        public List<Transform> Locations = new();
        public float ShipRadius = 2.0f;
        public Rigidbody Rigidbody;
        public float LinearThrust = 10.0f;
        public float LateralThrust = 10.0f;
        public float AngularThrust = 10.0f;
        public List<ParticleSystem> Engines = new();
        public List<ParticleSystem> AngularEngines = new();
        public Target Target;
        public AnimatePlanet AnimatePlanet;

        private bool isLocked = false;

        private readonly List<TankShipEngine> linearEngineStates = new();
        private readonly List<TankShipEngine> angularEngineStates = new();
        private readonly List<TankShipEngine> allEngineStates = new();
        #endregion

        #region Methods
        void Start()
        {
            if (this.Target == null)
            {
                this.Target = this.GetComponent<Target>();
            }

            this.linearEngineStates.AddRange(this.Engines.Select(TankShipEngine.Create));
            this.angularEngineStates.AddRange(this.AngularEngines.Select(TankShipEngine.Create));

            this.allEngineStates.AddRange(this.linearEngineStates);
            this.allEngineStates.AddRange(this.angularEngineStates);

            GameManager.Instance.OnOrientationChange += this.OnOrientationChange;
        }

        public void Reset()
        {
            this.isLocked = true;

            foreach (var engine in this.allEngineStates)
            {
                engine.Clear();
            }
        }

        public void Unlock()
        {
            this.isLocked = false;
        }

        public override void MoveTanksByKeyboard(PlayerTankController player, Vector3 input)
        {
            foreach (var engine in this.allEngineStates)
            {
                engine.ResetThrust();
            }

            if (!this.isLocked)
            {
                this.DoLinearThrust(input.y, input.z);
                this.DoAngularThrust(input.x);
            }

            foreach (var engine in this.allEngineStates)
            {
                engine.PostInputUpdate();
            }
        }

        public override void MoveTanksByJoystick(PlayerTankController player, Vector2 input, Vector3 localMainTankPos)
        {
            foreach (var engine in this.allEngineStates)
            {
                engine.ResetThrust();
            }

            if (!this.isLocked)
            {
                this.DoJoystickInput(input);
            }

            foreach (var engine in this.allEngineStates)
            {
                engine.PostInputUpdate();
            }
        }

        private void DoJoystickInput(Vector2 input)
        {
            var inputLength = input.magnitude;
            if (inputLength < 0.01f)
            {
                return;
            }

            var inputXYZ = Utils.FromXZ(input);
            var inputNorm = inputXYZ.normalized;
            var currentForward = this.transform.right;
            var dot = 0.0f;

            if (inputLength > 0.01f)
            {
                dot = Vector3.Dot(currentForward, inputNorm);
                this.DoLinearThrust(-dot, 0.0f);
            }

            var cross = Vector3.Cross(inputNorm, currentForward);
            if (Mathf.Abs(cross.y) > 0.01f)
            {
                var angularThrust = cross.y * 0.25f * this.AngularThrust;
                if (dot < 0.0f)
                {
                    angularThrust = -angularThrust;
                }
                this.DoAngularThrust(angularThrust);
            }
        }

        public override void SetTank(Tank? tank, int index)
        {
            if (tank != null)
            {
                tank.Planet = this.gameObject;
                tank.PlanetRadius = this.ShipRadius;
                tank.transform.localScale = this.Locations[index].localScale;
                // tank.transform.SetParent(this.transform, worldPositionStays: false);
            }
        }

        public override bool TryCalculateLocalPosition(int tankIndex, float radiusOffset, out Vector3 localPosition)
        {
            if (tankIndex < 0 || tankIndex >= this.Locations.Count)
            {
                localPosition = Vector3.zero;
                return false;
            }

            var transform = this.Locations[tankIndex];
            localPosition = transform.position - this.transform.position;
            return true;
        }

        public override bool TryCalculateLocalPosition(int tankIndex, float radiusOffset, out Vector3 localPosition, out Quaternion localRotation)
        {
            if (tankIndex < 0 || tankIndex >= this.Locations.Count)
            {
                localPosition = Vector3.zero;
                localRotation = Quaternion.identity;
                return false;
            }

            var transform = this.Locations[tankIndex];
            localPosition = transform.position - this.transform.position;
            localRotation = transform.localRotation;
            return true;
        }

        public override bool TryCalculateGlobalPosition(int tankIndex, float radiusOffset, out Vector3 globalPosition)
        {
            if (tankIndex < 0 || tankIndex >= this.Locations.Count)
            {
                globalPosition = Vector3.zero;
                return false;
            }

            var transform = this.Locations[tankIndex];
            globalPosition = transform.position + transform.right * radiusOffset;
            return true;
        }

        public override bool TryCalculateGlobalPosition(int tankIndex, float radiusOffset, out Vector3 globalPosition, out Quaternion globalRotation)
        {
            if (tankIndex < 0 || tankIndex >= this.Locations.Count)
            {
                globalPosition = Vector3.zero;
                globalRotation = Quaternion.identity;
                return false;
            }

            var transform = this.Locations[tankIndex];
            globalPosition = transform.position + transform.right * radiusOffset;
            globalRotation = transform.rotation;
            return true;
        }

        private void OnOrientationChange(bool landscape)
        {
        }

        private void DoLinearThrust(float forwardInput, float lateralInput)
        {
            var inputLinear = forwardInput * -this.LinearThrust;
            var inputLateral = lateralInput * this.LateralThrust;

            if (!Mathf.Approximately(inputLinear, 0.0f))
            {
                var thrustForward = this.transform.right * inputLinear;
                this.Rigidbody.AddForce(thrustForward, ForceMode.Force);
                thrustForward.Normalize();

                foreach (var engine in this.linearEngineStates)
                {
                    engine.SetLinearThrustFromInput(thrustForward);
                }
            }

            if (!Mathf.Approximately(inputLateral, 0.0f))
            {
                var thrustLateral = this.transform.forward * inputLateral;
                this.Rigidbody.AddForce(thrustLateral, ForceMode.Force);
                thrustLateral.Normalize();

                foreach (var engine in this.angularEngineStates)
                {
                    engine.SetLinearThrustFromInput(thrustLateral);
                }
            }
        }

        private void DoAngularThrust(float input)
        {
            var inputAngular = input * -this.AngularThrust;

            if (!Mathf.Approximately(inputAngular, 0.0f))
            {
                this.Rigidbody.AddTorque(0, inputAngular, 0);

                foreach (var engine in this.angularEngineStates)
                {
                    engine.SetAngularThrustFromInput(input);
                }
            }
        }

        #endregion
    }
}