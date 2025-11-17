using System.Linq;
using System.Collections.Generic;
using UnityEngine;

#nullable enable

namespace Orbits
{
    public class TankPlanet : ITankContainer
    {
        #region Fields
        private static readonly IReadOnlyList<float> AngleOffsets = new float[] { 0, 30, -30, 60, -60, 90, -90, 120, -120, 150, -150 };

        public float PlanetRadius = 5.0f;
        public float TankSpeed = 300;

        public float RotationAngle = 90.0f;

        public GameObject Planet;

        public Vector3 LocalMainTankPosition = Vector3.zero;
        #endregion

        #region Methods
        public override void InitPlanet(GameObject planet)
        {
            this.Planet = planet;
        }

        public override void MoveTanksByKeyboard(PlayerTankController player, Vector3 inputMove)
        {
            this.MoveTanksLinear(player, inputMove.x);
        }

        public override void MoveTanksByJoystick(PlayerTankController player, Vector2 input, Vector3 localMainTankPos)
        {
            var move = 0.0f;
            if (input.magnitude > 0.01f)
            {
                var normalised = input.normalized;
                var onPlanetDir = localMainTankPos.normalized;
                var cross = Vector3.Cross(Utils.FromXZ(normalised), onPlanetDir);
                move = cross.y > 0.1f ? 1 : (cross.y < -0.1f ? -1 : 0);
            }

            this.MoveTanksLinear(player, move);
        }

        private void MoveTanksLinear(PlayerTankController player, float input)
        {
            if (player.ValidTanks.Count == 0)
            {
                return;
            }

            if (!Mathf.Approximately(input, Mathf.Epsilon))
            {
                var move = input / this.PlanetRadius * Time.deltaTime * GameManager.Instance.PlayerDeltaTimeScale * this.TankSpeed;
                this.RotationAngle = Utils.WrapAngle(this.RotationAngle + move);
            }
        }

        public override void SetTank(Tank? tank, int index)
        {
            if (tank != null)
            {
                tank.Planet = this.Planet;
                tank.PlanetRadius = this.PlanetRadius;
            }
        }

        public override bool TryCalculateLocalPosition(int tankIndex, float radiusOffset, out Vector3 localPosition)
        {
            if (tankIndex < 0 || tankIndex >= AngleOffsets.Count)
            {
                localPosition = Vector3.zero;
                return false;
            }

            var currentOffset = this.RotationAngle;
            localPosition = CalculatePosition(AngleOffsets[tankIndex] + currentOffset, this.PlanetRadius + radiusOffset);
            return true;
        }

        public override bool TryCalculateLocalPosition(int tankIndex, float radiusOffset, out Vector3 localPosition, out Quaternion localRotation)
        {
            if (!this.TryCalculateLocalPosition(tankIndex, radiusOffset, out localPosition))
            {
                localRotation = Quaternion.identity;
                return false;
            }

            var currentOffset = this.RotationAngle;
            localRotation = Quaternion.Euler(0, -AngleOffsets[tankIndex] - currentOffset, 0);
            return true;
        }

        public override bool TryCalculateGlobalPosition(int tankIndex, float radiusOffset, out Vector3 globalPosition)
        {
            if (this.TryCalculateLocalPosition(tankIndex, radiusOffset, out var localPosition))
            {
                globalPosition = this.Planet.transform.position + localPosition;
                return true;
            }

            globalPosition = Vector3.zero;
            return false;
        }

        public override bool TryCalculateGlobalPosition(int tankIndex, float radiusOffset, out Vector3 globalPosition, out Quaternion globalRotation)
        {
            if (this.TryCalculateLocalPosition(tankIndex, radiusOffset, out var localPosition, out var localRotation))
            {
                globalPosition = this.Planet.transform.position + localPosition;
                globalRotation = localRotation;
                return true;
            }

            globalPosition = Vector3.zero;
            globalRotation = Quaternion.identity;
            return false;
        }

        public static Vector3 CalculatePosition(float angleOnPlanet, float planetRadius)
        {
            var radians = angleOnPlanet * Mathf.Deg2Rad;
            var xPos = Mathf.Cos(radians) * planetRadius;
            var zPos = Mathf.Sin(radians) * planetRadius;
            return new Vector3(xPos, 0.0f, zPos);
        }
        #endregion
    }
}