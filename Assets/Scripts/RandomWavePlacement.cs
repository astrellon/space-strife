using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

#nullable enable

namespace Orbits
{
    public class RandomWavePlacement : MonoBehaviour
    {
        [Serializable]
        public struct Avoid
        {
            public Vector3 Location;
            public float Radius;
        }

        #region Fields
        public List<Avoid> ToAvoid = new();
        public float Size = 50.0f;
        #endregion

        #region Methods
        void OnDrawGizmos()
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(this.transform.position, this.Size);

            Gizmos.color = Color.magenta;
            foreach (var toAvoid in this.ToAvoid)
            {
                Gizmos.DrawWireSphere(toAvoid.Location, toAvoid.Radius);
            }
        }

        public Vector3 CalculatePos(uint index)
        {
            var count = 10;
            do
            {
                var normalisedPos = UnityEngine.Random.insideUnitCircle;
                var distance = UnityEngine.Random.value * this.Size;
                var pos = new Vector3(normalisedPos.x * distance, 0.0f, normalisedPos.y * distance) + this.transform.position;

                if (!this.IntersectsWithAvoid(pos))
                {
                    return pos;
                }
            } while (count-- >= 0);

            Debug.LogWarning($"Failed to create position for index: {index}");
            return Vector3.zero;
        }

        private bool IntersectsWithAvoid(Vector3 input)
        {
            foreach (var toAvoid in this.ToAvoid)
            {
                var distance = Vector3.Distance(toAvoid.Location, input);
                if (distance < toAvoid.Radius)
                {
                    return true;
                }
            }

            return false;
        }
        #endregion
    }
}