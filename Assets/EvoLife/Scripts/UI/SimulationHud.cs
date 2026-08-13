using UnityEngine;
using UnityEngine.UI;
using EvoLife.Analytics;
using EvoLife.Simulation;

namespace EvoLife.UI
{
    /// <summary>
    /// Minimal HUD for population and time. Presentation only — no simulation logic.
    /// </summary>
    public sealed class SimulationHud : MonoBehaviour
    {
        [SerializeField] PopulationStatisticCollector collector;
        [SerializeField] SimulationClock clock;
        [SerializeField] Text statusText;

        void Update()
        {
            if (statusText == null)
            {
                return;
            }

            var snapshot = collector != null ? collector.Capture() : null;
            var time = clock != null ? clock.SimulationTimeSeconds : snapshot?.simulationTimeSeconds ?? 0f;
            var herb = snapshot?.herbivoreCount ?? 0;
            var pred = snapshot?.predatorCount ?? 0;

            statusText.text = $"t={time:0.0}s  herbivores={herb}  predators={pred}";
        }
    }
}
