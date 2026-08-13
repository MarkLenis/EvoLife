using System;
using UnityEngine;

namespace EvoLife.Simulation
{
    /// <summary>
    /// Time and extinction stop rules. Manual stop is requested by the orchestrator, not stored here.
    /// A max time of 0 or less means no time limit.
    /// </summary>
    [Serializable]
    public sealed class ExperimentStoppingConditions
    {
        [SerializeField] float maxSimulationTimeSeconds = 600f;
        [SerializeField] bool stopOnEcosystemExtinction = true;
        [SerializeField] bool stopOnHerbivoreExtinction;
        [SerializeField] bool stopOnPredatorExtinction;

        public float MaxSimulationTimeSeconds
        {
            get => maxSimulationTimeSeconds;
            set => maxSimulationTimeSeconds = value;
        }

        public bool StopOnEcosystemExtinction
        {
            get => stopOnEcosystemExtinction;
            set => stopOnEcosystemExtinction = value;
        }

        public bool StopOnHerbivoreExtinction
        {
            get => stopOnHerbivoreExtinction;
            set => stopOnHerbivoreExtinction = value;
        }

        public bool StopOnPredatorExtinction
        {
            get => stopOnPredatorExtinction;
            set => stopOnPredatorExtinction = value;
        }

        public bool HasTimeLimit => maxSimulationTimeSeconds > 0f;

        public ExperimentStoppingConditions Clone()
        {
            return new ExperimentStoppingConditions
            {
                MaxSimulationTimeSeconds = MaxSimulationTimeSeconds,
                StopOnEcosystemExtinction = StopOnEcosystemExtinction,
                StopOnHerbivoreExtinction = StopOnHerbivoreExtinction,
                StopOnPredatorExtinction = StopOnPredatorExtinction
            };
        }

        public static ExperimentStoppingConditions ForTrainingEpisode(float maxSimulationTimeSeconds)
        {
            return new ExperimentStoppingConditions
            {
                MaxSimulationTimeSeconds = Mathf.Max(0f, maxSimulationTimeSeconds),
                StopOnEcosystemExtinction = false,
                StopOnHerbivoreExtinction = false,
                StopOnPredatorExtinction = false
            };
        }

        public static ExperimentStoppingConditions ForPersistentEcosystem(float maxSimulationTimeSeconds)
        {
            return new ExperimentStoppingConditions
            {
                MaxSimulationTimeSeconds = Mathf.Max(0f, maxSimulationTimeSeconds),
                StopOnEcosystemExtinction = true,
                StopOnHerbivoreExtinction = false,
                StopOnPredatorExtinction = false
            };
        }
    }
}
