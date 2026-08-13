using System;
using System.Collections.Generic;
using UnityEngine;
using EvoLife.Common;
using EvoLife.Simulation;

namespace EvoLife.Analytics
{
    /// <summary>
    /// Collects population/time stats. Add new metrics via small focused collectors.
    /// </summary>
    public sealed class PopulationStatisticCollector : MonoBehaviour, IStatisticCollector
    {
        [SerializeField] SimulationClock clock;
        [SerializeField] PopulationTracker population;
        [SerializeField] string experimentId = "local-dev";

        int previousTotalAlive;
        bool hasPrevious;
        readonly List<IAnalyticsCreatureView> liveBuffer = new List<IAnalyticsCreatureView>();

        public string ExperimentIdValue => experimentId;

        public void SetExperimentId(ExperimentId id) => experimentId = id.Value;

        public void SetLiveViews(IEnumerable<IAnalyticsCreatureView> views)
        {
            liveBuffer.Clear();
            if (views == null)
            {
                return;
            }

            foreach (var view in views)
            {
                if (view != null)
                {
                    liveBuffer.Add(view);
                }
            }
        }

        public SimulationStatsSnapshot Capture()
        {
            var total = population != null ? population.TotalAlive : 0;
            var previous = hasPrevious ? previousTotalAlive : total;
            var snapshot = AnalyticsSnapshotBuilder.Build(
                experimentId,
                clock != null ? clock.SimulationTimeSeconds : 0f,
                population,
                previous,
                AnalyticsSnapshotBuilder.Census(liveBuffer),
                (float)DateTimeOffset.UtcNow.ToUnixTimeSeconds());

            previousTotalAlive = total;
            hasPrevious = true;
            return snapshot;
        }
    }
}
