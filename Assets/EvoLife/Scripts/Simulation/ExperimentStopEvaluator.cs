using EvoLife.Common;

namespace EvoLife.Simulation
{
    /// <summary>
    /// Pure stop-condition check. Does not tick the world or talk to analytics.
    /// </summary>
    public static class ExperimentStopEvaluator
    {
        public static ExperimentStopReason Evaluate(
            ExperimentStoppingConditions conditions,
            float simulationTimeSeconds,
            IPopulationSnapshot population,
            bool manualStopRequested)
        {
            if (manualStopRequested)
            {
                return ExperimentStopReason.ManualStop;
            }

            conditions = conditions ?? new ExperimentStoppingConditions();
            var extinction = ExtinctionEvaluator.Evaluate(population);

            if (conditions.StopOnEcosystemExtinction && extinction == ExtinctionState.EcosystemExtinct)
            {
                return ExperimentStopReason.EcosystemExtinct;
            }

            if (conditions.StopOnHerbivoreExtinction
                && (extinction == ExtinctionState.HerbivoresExtinct
                    || extinction == ExtinctionState.EcosystemExtinct))
            {
                return ExperimentStopReason.HerbivoresExtinct;
            }

            if (conditions.StopOnPredatorExtinction
                && (extinction == ExtinctionState.PredatorsExtinct
                    || extinction == ExtinctionState.EcosystemExtinct))
            {
                return ExperimentStopReason.PredatorsExtinct;
            }

            if (conditions.HasTimeLimit && simulationTimeSeconds >= conditions.MaxSimulationTimeSeconds)
            {
                return ExperimentStopReason.MaxSimulationTime;
            }

            return ExperimentStopReason.None;
        }
    }

    /// <summary>
    /// Population ratios that return 0 when a side is extinct (never divide by zero).
    /// </summary>
    public static class ExperimentPopulationRates
    {
        public static float HerbivoreFraction(IPopulationSnapshot snapshot)
        {
            if (snapshot == null || snapshot.TotalAlive <= 0)
            {
                return 0f;
            }

            return snapshot.HerbivoreCount / (float)snapshot.TotalAlive;
        }

        public static float PredatorFraction(IPopulationSnapshot snapshot)
        {
            if (snapshot == null || snapshot.TotalAlive <= 0)
            {
                return 0f;
            }

            return snapshot.PredatorCount / (float)snapshot.TotalAlive;
        }

        public static float PredatorsPerHerbivore(IPopulationSnapshot snapshot)
        {
            if (snapshot == null || snapshot.HerbivoreCount <= 0)
            {
                return 0f;
            }

            return snapshot.PredatorCount / (float)snapshot.HerbivoreCount;
        }
    }
}
