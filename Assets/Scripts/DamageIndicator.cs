using UnityEngine;
using TMPro;

#nullable enable

namespace Orbits
{
    public class DamageIndicator : MonoBehaviour
    {
        #region Fields
        public TMP_Text Text;
        private Vector3 from;
        private Vector3 to;
        private float lerp;
        #endregion

        #region Unity Methods
        void Awake()
        {
            this.hideFlags = HideFlags.HideInInspector;
        }
        #endregion

        #region Methods
        public bool ManagedUpdate(float dt)
        {
            this.lerp += Time.deltaTime;
            var t = Easing.Cubic.Out(Mathf.Clamp01(this.lerp));
            this.transform.position = Vector3.Lerp(this.from, this.to, t);

            return this.lerp >= 1.0f;
        }

        public void Show(Vector3 from, Vector3 to, string amount)
        {
            this.from = from;
            this.to = to;
            this.lerp = 0.0f;
            this.Text.text = amount;
            this.transform.localRotation = Quaternion.Euler(90, Random.Range(-10.0f, 10.0f), 0.0f);
        }
        #endregion
    }
}