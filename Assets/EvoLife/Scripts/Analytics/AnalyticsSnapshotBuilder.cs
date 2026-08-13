using System;
using System.Collections.Generic;
using EvoLife.Common;

namespace EvoLife.Analytics
{
    /// <summary>
    /// Point-in-time simulation metrics for UI and backend export.
    /// The first six fields remain the Unity v1 <c>/api/v1/stats</c> contract.
    /// Additional fields are backward-compatible extras.
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
        public int births;
        public int deaths;
        public int populationChange;
        public int scriptedAlive;
        public int ppoAlive;
        public int maxGeneration;
    }

    public interface IStatisticCollector
    {
        SimulationStatsSnapshot Capture();
    }

    public sealed class PolicyCensus
    {
        public int ScriptedAlive;
        public int PpoAlive;
        public int MaxGeneration;
    }

    /// <summary>
    /// Pure snapshot math. Safe for empty populations (no divide-by-zero).
    /// </summary>
    public static class AnalyticsSnapshotBuilder
    {
        public static SimulationStatsSnapshot Build(
            string experimentId,
            float simulationTimeSeconds,
            IPopulationSnapshot population,
            int previousTotalAlive = 0,
            PolicyCensus census = null,
            float timestampUtcUnix = 0f)
        {
            var herb = population != null ? population.HerbivoreCount : 0;
            var pred = population != null ? population.PredatorCount : 0;
            var alive = population != null ? population.TotalAlive : herb + pred;
            var births = population != null ? population.Births : 0;
            var deaths = population != null ? population.Deaths : 0;

            return new SimulationStatsSnapshot
            {
                experimentId = experimentId ?? string.Empty,
                simulationTimeSeconds = simulationTimeSeconds,
                herbivoreCount = herb,
                predatorCount = pred,
                totalAlive = alive,
                timestampUtcUnix = timestampUtcUnix,
                births = births,
                deaths = deaths,
                populationChange = alive - previousTotalAlive,
                scriptedAlive = census != null ? census.ScriptedAlive : 0,
                ppoAlive = census != null ? census.PpoAlive : 0,
                maxGeneration = census != null ? census.MaxGeneration : 0
            };
        }

        public static PolicyCensus Census(IEnumerable<IAnalyticsCreatureView> liveViews)
        {
            var census = new PolicyCensus();
            if (liveViews == null)
            {
                return census;
            }

            foreach (var view in liveViews)
            {
                if (view == null)
                {
                    continue;
                }

                if (view.Vitals != null && !view.Vitals.IsAlive)
                {
                    continue;
                }

                var policy = PolicyClassifier.Classify(view);
                if (PolicyClassifier.IsLearnedPpo(policy))
                {
                    census.PpoAlive++;
                }
                else
                {
                    census.ScriptedAlive++;
                }

                var generation = view.Lineage != null ? view.Lineage.Generation : 0;
                if (generation > census.MaxGeneration)
                {
                    census.MaxGeneration = generation;
                }
            }

            return census;
        }
    }
}
