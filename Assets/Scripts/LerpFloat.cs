using System;
using System.Linq;
using UnityEngine;

#nullable enable

namespace Orbits
{
    public class LerpFloat
    {
        #region Fields
        public float T;
        public float Value;
        public float From;
        public float To;
        public float Speed = 1.0f;
        public Easing.Function Easing = global::Easing.Linear;

        public bool IsComplete => this.T >= 1.0f;
        #endregion

        #region Methods
        public void LerpTo(float to, float from = -1.0f, float? speed = null, float t = 0.0f)
        {
            this.To = to;
            this.From = from >= 0.0f ? from : this.Value;
            this.Speed = speed ?? this.Speed;
            this.T = t;
        }

        public void Update(float dt)
        {
            this.T = Mathf.Clamp01(this.T + dt / this.Speed);
            if (!this.IsComplete)
            {
                this.Value = Mathf.Lerp(this.From, this.To, this.Easing(this.T));
            }
            else
            {
                this.Value = this.To;
            }
        }

        public static LerpFloat Empty()
        {
            return new() { T = 1.0f };
        }
        #endregion
    }

    public class LerpVector3
    {
        #region Fields
        public float T;
        public Vector3 Value;
        public Vector3 From;
        public Vector3 To;
        public float Speed = 1.0f;
        public Easing.Function Easing = global::Easing.Linear;
        #endregion

        #region Methods
        public void LerpTo(Vector3 to, Vector3? from = null, float? speed = null, float t = 0.0f)
        {
            this.To = to;
            this.From = from ?? this.Value;
            this.Speed = speed ?? this.Speed;
            this.T = t;
        }

        public void Update(float dt)
        {
            this.T = Mathf.Clamp01(this.T + dt / this.Speed);
            if (this.T < 1.0f)
            {
                this.Value = Vector3.Lerp(this.From, this.To, this.Easing(this.T));
            }
            else
            {
                this.Value = this.To;
            }
        }
        #endregion
    }

    public class LerpTransform
    {
        #region Fields
        public float T;
        public Vector3 Value;
        public Vector3 From;
        public Transform? To;
        public Vector3 MidOffset = Vector3.zero;
        public bool HasMidOffset = false;
        public float Speed = 1.0f;
        public Easing.Function Easing = global::Easing.Linear;
        #endregion

        #region Methods
        public void SetMidOffset(Vector3 midOffset)
        {
            this.MidOffset = midOffset;
            this.HasMidOffset = midOffset.magnitude > Mathf.Epsilon;
        }

        public void LerpTo(Transform? to, Vector3? from = null, float? speed = null, float t = 0.0f)
        {
            this.To = to;
            this.From = from ?? this.Value;
            this.Speed = speed ?? this.Speed;
            this.T = t;
        }

        public void Update(float dt)
        {
            this.T = Mathf.Clamp01(this.T + dt / this.Speed);
            if (this.To == null)
            {
                return;
            }

            if (this.T < 1.0f)
            {
                var easeT = this.Easing(this.T);
                this.Value = Vector3.Lerp(this.From, this.To.position, easeT);
                if (this.HasMidOffset)
                {
                    this.Value += global::Easing.Quadratic.Loop(easeT) * this.MidOffset;
                }
            }
            else
            {
                this.Value = this.To.position;
            }
        }
        #endregion
    }
}