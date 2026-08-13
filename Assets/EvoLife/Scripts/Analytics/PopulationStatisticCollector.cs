using System;
using UnityEngine;
using EvoLife.Common;
using EvoLife.Simulation;

namespace EvoLife.Analytics
{
    /// <summary>
    /// Point-in-time simulation metrics for UI and backend export.
    /// </summary>
    [Serializable]
    public sealed class SimulationStatsSnapshot
    {
        public string experimentId;
        public float simulationTimeSeconds;
        public int herbivoreCount;
        public int predatorCount;
        public int totalAlive;
        public float timestampUtcUnix;
    }

    public interface IStatisticCollector
    {
        SimulationStatsSnapshot Capture();
    }

    /// <summary>
    /// Collects population/time stats. Add new metrics here (or via small focused collectors).
    /// </summary>
    public sealed class PopulationStatisticCollector : MonoBehaviour, IStatisticCollector
    {
        [SerializeField] SimulationClock clock;
        [SerializeField] PopulationTracker population;
        [SerializeField] string experimentId = "local-dev";

        public SimulationStatsSnapshot Capture()
        {
            return new SimulationStatsSnapshot
            {
                experimentId = experimentId,
                simulationTimeSeconds = clock != null ? clock.SimulationTimeSeconds : 0f,
                herbivoreCount = population != null ? population.HerbivoreCount : 0,
                predatorCount = population != null ? population.PredatorCount : 0,
                totalAlive = population != null ? population.TotalAlive : 0,
                timestampUtcUnix = (float)DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
        }

        public void SetExperimentId(ExperimentId id) => experimentId = id.Value;
    }
}
