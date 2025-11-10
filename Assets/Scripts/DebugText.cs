using System.Text;
using UnityEngine;
using TMPro;

namespace Orbits
{
    public class DebugText : MonoBehaviour
    {
        public TMP_Text Text;
        public bool Enabled = true;

        private readonly StringBuilder builder = new();

        // Update is called once per frame
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.BackQuote))
            {
                this.Enabled = !this.Enabled;
                this.Text.enabled = this.Enabled;
            }

            if (this.Enabled)
            {
                this.UpdateText();
            }
        }

        private void UpdateText()
        {
            this.builder.Clear();

            // this.AddObjectPools();
            // this.AddProjectileManagers();
            // this.AddCurrentLevelStats();
            this.AddSaturationStats();

            this.Text.text = this.builder.ToString();
        }

        private void AddSaturationStats()
        {
            var effect = SaturationEffect.Instance;
            if (effect == null)
            {
                return;
            }

            this.builder.AppendLine("Saturation Effect:");
            this.builder.AppendLine($"- Enabled: {effect.gameObject.activeSelf}");
            this.builder.AppendLine($"- Show: {effect.Show}");
            this.builder.AppendLine($"- Show Amount: {effect.showAmount}");
        }

        private void AddCurrentLevelStats()
        {
            if (GameManager.Instance.CurrentLevel == null)
            {
                return;
            }

            var stats = PlayerState.Instance.CurrentLevelStats;
            this.builder.AppendLine("Shots fired:");
            foreach (var shotsFiredKvp in stats.ShotsFired)
            {
                this.builder.AppendLine($"- {shotsFiredKvp.Key}: {shotsFiredKvp.Value}");
            }

            this.builder.AppendLine("Shots hit:");
            foreach (var shotsHitKvp in stats.ShotsHit)
            {
                this.builder.AppendLine($"- {shotsHitKvp.Key}: {shotsHitKvp.Value}");
            }
        }

        private void AddObjectPools()
        {
            this.builder.AppendLine("Pools:");
            foreach (var pool in GameObjectPools.Instance.Pools)
            {
                this.builder.AppendLine($"- {pool.Prefab.name}: {pool.Count} / {pool.CountActive}");
            }
        }

        private void AddProjectileManagers()
        {
            this.builder.AppendLine("\nProjectiles:");
            foreach (var kvp in ProjectileManager.Instance.Managers)
            {
                this.builder.AppendLine($"- {kvp.Key.name}: {kvp.Value.NumAlive}");
            }
        }
    }
}
