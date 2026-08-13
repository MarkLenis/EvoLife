using System;
using System.Collections.Generic;
using EvoLife.Common;
using EvoLife.Simulation;

namespace EvoLife.Analytics
{
    /// <summary>
    /// Compact experiment identity for reproducibility. Does not dump the full Unity config asset.
    /// </summary>
    [Serializable]
    public sealed class ExperimentRunMetadata
    {
        public string ExperimentName;
        public int RandomSeed;
        public string HerbivorePolicy;
        public string PredatorPolicy;
        public int InitialHerbivores;
        public int InitialPredators;
        public float TimeScale;
        public string ScenarioId;
        public string TrainingModelId;
        public string EcosystemMode;
        public bool TrainingRespawnEnabled;
        public int MaxHerbivores;
        public int MaxPredators;
        public double StartedAtUnix;

        public static ExperimentRunMetadata FromConfig(SimulationConfig config, double startedAtUnix)
        {
            if (config == null)
            {
                return new ExperimentRunMetadata
                {
                    ExperimentName = "unnamed",
                    HerbivorePolicy = PolicyKindNames.ScriptedBaseline,
                    PredatorPolicy = PolicyKindNames.ScriptedBaseline,
                    EcosystemMode = EcosystemModeNames.Persistent,
                    TrainingRespawnEnabled = false,
                    StartedAtUnix = startedAtUnix
                };
            }

            return new ExperimentRunMetadata
            {
                ExperimentName = config.ExperimentName,
                RandomSeed = config.RandomSeed,
                HerbivorePolicy = PolicyKindNames.ToWireName(config.HerbivorePolicy),
                PredatorPolicy = PolicyKindNames.ToWireName(config.PredatorPolicy),
                InitialHerbivores = config.InitialHerbivores,
                InitialPredators = config.InitialPredators,
                TimeScale = config.DefaultTimeScale,
                ScenarioId = config.ScenarioId,
                TrainingModelId = config.TrainingModelId,
                EcosystemMode = EcosystemModeNames.ToWireName(config.Ecosystem.Mode),
                TrainingRespawnEnabled = config.Ecosystem.TrainingRespawnEnabled,
                MaxHerbivores = config.Ecosystem.MaxHerbivores,
                MaxPredators = config.Ecosystem.MaxPredators,
                StartedAtUnix = startedAtUnix
            };
        }

        public Dictionary<string, object> ToConfigurationDictionary()
        {
            var config = new Dictionary<string, object>
            {
                ["policy_herbivore"] = HerbivorePolicy ?? PolicyKindNames.ScriptedBaseline,
                ["policy_predator"] = PredatorPolicy ?? PolicyKindNames.ScriptedBaseline,
                ["initial_herbivores"] = InitialHerbivores,
                ["initial_predators"] = InitialPredators,
                ["time_scale"] = TimeScale,
                ["ecosystem_mode"] = string.IsNullOrEmpty(EcosystemMode)
                    ? EcosystemModeNames.Persistent
                    : EcosystemMode,
                ["training_respawn_enabled"] = TrainingRespawnEnabled,
                ["max_herbivores"] = MaxHerbivores,
                ["max_predators"] = MaxPredators
            };

            if (!string.IsNullOrEmpty(ScenarioId))
            {
                config["scenario_id"] = ScenarioId;
            }

            if (!string.IsNullOrEmpty(TrainingModelId))
            {
                config["training_model_id"] = TrainingModelId;
            }

            return config;
        }
    }
}
