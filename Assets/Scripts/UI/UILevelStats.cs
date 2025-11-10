using UnityEngine;
using TMPro;
using System.Linq;

#nullable enable

namespace Orbits
{
    public class UILevelStats : MonoBehaviour
    {
        #region Fields
        public TMP_Text? DescriptionText;
        public TMP_Text? StatsText;
        #endregion

        #region Unity Methods
        private void OnEnable()
        {
            if (this.StatsText == null)
                // this.DescriptionText == null)
            {
                return;
            }

            var starSystem = UIManager.Instance.CurrentStarSystem;
            var levelContainer = UIManager.Instance.CurrentLevelSelected;
            if (starSystem == null || levelContainer == null)
            {
                this.StatsText.text = "Nothing selected";
                return;
            }

            var level = levelContainer.LevelPrefab;
            if (level == null)
            {
                this.StatsText.text = "Nothing selected";
                return;
            }

            // this.DescriptionText.text = levelContainer.GetDescription();
            if (!PlayerState.Instance.TryGetLevelTotal(starSystem.Id, level.Id, out var stats))
            {
                this.StatsText.text = "Not played";
                return;
            }

            // var shotsFired = stats.ShotsFired.Any() ? stats.ShotsFired.Sum(kvp => kvp.Value) : 0L;
            // var shotsHit = stats.ShotsHit.Any() ? stats.ShotsHit.Sum(kvp => kvp.Value) : 0L;
            // var targetsDestroyed = stats.TargetsDestroyed.Any() ? stats.TargetsDestroyed.Sum(kvp => kvp.Value) : 0L;

            // this.StatsText.text = $"Shots Fired: {shotsFired}\nShots Hit: {shotsHit}\nDestroyed: {targetsDestroyed}";
            if (stats.PlanetHealthRemaining >= 0)
            {
                this.StatsText.text = $"Planet Health: {stats.PlanetHealthRemaining}/{level.PlanetMaxHealth}";
            }
        }
        #endregion
    }
}