using UnityEngine;

#nullable enable

namespace Orbits
{
    public class AIShipController : MonoBehaviour
    {
        public Ship? Ship;
        public bool PointAtPlayer = false;

        // Update is called once per frame
        private void Update()
        {
            if (this.Ship != null)
            {
                this.Ship.InputFire = true;
                if (this.PointAtPlayer)
                {
                    var fireAt = GameManager.Instance.CurrentLevelContainer?.Ship?.transform.position;
                    this.Ship.HasFireAtPoint = fireAt.HasValue;
                    if (fireAt.HasValue)
                    {
                        this.Ship.FireAt = fireAt.Value;
                    }
                }
            }
        }
    }
}
