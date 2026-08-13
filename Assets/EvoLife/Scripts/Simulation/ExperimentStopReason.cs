using System;

namespace EvoLife.Simulation
{
    /// <summary>
    /// Why an experiment run ended. <see cref="None"/> means the run is still active.
    /// </summary>
    public enum ExperimentStopReason : byte
    {
        None = 0,
        MaxSimulationTime = 1,
        EcosystemExtinct = 2,
        HerbivoresExtinct = 3,
        PredatorsExtinct = 4,
        ManualStop = 5
    }

    public static class ExperimentStopReasonNames
    {
        public const string None = "none";
        public const string MaxSimulationTime = "max_simulation_time";
        public const string EcosystemExtinct = "ecosystem_extinct";
        public const string HerbivoresExtinct = "herbivores_extinct";
        public const string PredatorsExtinct = "predators_extinct";
        public const string ManualStop = "manual_stop";

        public static string ToWireName(ExperimentStopReason reason)
        {
            switch (reason)
            {
                case ExperimentStopReason.None:
                    return None;
                case ExperimentStopReason.MaxSimulationTime:
                    return MaxSimulationTime;
                case ExperimentStopReason.EcosystemExtinct:
                    return EcosystemExtinct;
                case ExperimentStopReason.HerbivoresExtinct:
                    return HerbivoresExtinct;
                case ExperimentStopReason.PredatorsExtinct:
                    return PredatorsExtinct;
                case ExperimentStopReason.ManualStop:
                    return ManualStop;
                default:
                    return Unreachable(reason);
            }
        }

        static string Unreachable(ExperimentStopReason reason)
        {
            throw new ArgumentOutOfRangeException(nameof(reason), reason, "Unhandled ExperimentStopReason.");
        }
    }
}
