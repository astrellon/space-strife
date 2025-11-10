using System.Collections;
using UnityEngine;

#nullable enable

namespace Orbits
{
    public class WaveIndicator : MonoBehaviour
    {
        #region Fields
        public GameObject Prefab;
        public TrailRenderer? TrailRenderer;
        public Wave ForWave;
        public int SplineIndex;
        public float CurrentTime;
        private float t;
        private bool returning = false;

        public Gradient GruntGradient = new();
        public Gradient BossGradient = new();
        public Gradient FinalBossGradient = new();
        #endregion

        #region Unity Methods
        private void OnEnable()
        {
            this.t = 0.0f;
            this.returning = false;
            this.CurrentTime = 0.0f;

            if (this.TrailRenderer != null &&
                this.ForWave != null &&
                this.ForWave.Prefab != null &&
                this.ForWave.Prefab.TryGetComponent<Target>(out var waveTarget))
            {
                if (waveTarget.TargetType == TargetType.Boss)
                {
                    this.TrailRenderer.colorGradient = this.BossGradient;
                }
                else if (waveTarget.TargetType == TargetType.FinalBoss)
                {
                    this.TrailRenderer.colorGradient = this.FinalBossGradient;
                }
            }
        }

        private void Update()
        {
            if (this.ForWave == null)
            {
                GameObjectPools.Instance.Release(this.Prefab, this.gameObject);
                return;
            }

            if (this.returning)
            {
                return;
            }

            if ((this.ForWave.NumShipsLeft == 0 || this.t >= 1.0f) && !this.returning)
            {
                if (this.ForWave.NumShipsLeft > 0)
                {
                    StartCoroutine(this.Reset());
                }
                else
                {
                    StartCoroutine(this.Release());
                }
                this.returning = true;
            }

            var isFirst = Mathf.Approximately(this.t, 0.0f);
            this.CurrentTime += Time.deltaTime * 3.0f;
            this.transform.position = this.ForWave.CalculatePosition(this.CurrentTime, this.SplineIndex, out this.t);

            if (isFirst && this.TrailRenderer != null)
            {
                this.TrailRenderer.Clear();
            }
        }
        #endregion

        #region Methods
        IEnumerator Release()
        {
            yield return new WaitForSeconds(4.0f);
            GameObjectPools.Instance.Release(this.Prefab, this.gameObject);
        }

        IEnumerator Reset()
        {
            yield return new WaitForSeconds(4.0f);
            this.CurrentTime = 0.0f;
            this.t = 0;
            this.returning = false;
        }
        #endregion
    }
}