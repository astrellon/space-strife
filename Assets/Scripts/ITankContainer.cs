using System;
using UnityEngine;

#nullable enable

namespace Orbits
{
    public class ITankContainer : MonoBehaviour
    {
        public virtual void InitPlanet(GameObject planet) { }
        public virtual void SetTank(Tank? tank, int index) { }
        public virtual void MoveTanksByJoystick(PlayerTankController player, Vector2 input, Vector3 localMainTankPos) { }
        public virtual void MoveTanksByKeyboard(PlayerTankController player, Vector3 input) { }

        public bool HandleMovementInUpdate = true;

        public virtual bool TryCalculateLocalPosition(int tankIndex, float radiusOffset, out Vector3 localPosition)
        {
            localPosition = Vector3.zero;
            return false;
        }
        public virtual bool TryCalculateLocalPosition(int tankIndex, float radiusOffset, out Vector3 localPosition, out Quaternion localRotation)
        {
            localPosition = Vector3.zero;
            localRotation = Quaternion.identity;
            return false;
        }

        public virtual bool TryCalculateGlobalPosition(int tankIndex, float radiusOffset, out Vector3 globalPosition)
        {
            globalPosition = Vector3.zero;
            return false;
        }
        public virtual bool TryCalculateGlobalPosition(int tankIndex, float radiusOffset, out Vector3 globalPosition, out Quaternion globalRotation)
        {
            globalPosition = Vector3.zero;
            globalRotation = Quaternion.identity;
            return false;
        }
    }
}