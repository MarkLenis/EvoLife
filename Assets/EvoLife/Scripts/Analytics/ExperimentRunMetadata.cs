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
        public string CurriculumStageId;
        public string EcosystemMode;
        public bool TrainingRespawnEnabled;
        public int MaxHerbivores;
        public int MaxPredators;
        public float ResourceAbundance;
        public float PlantRegenerationMultiplier;
        public float MutationProbability;
        public float MutationMagnitudeScale;
        public float DayLengthSeconds;
        public string[] EnabledEnvironmentalEvents;
        public float MaxSimulationTimeSeconds;
        public bool StopOnEcosystemExtinction;
        public int FounderGenomeSeed;
        public int ReproductionSeed;
        public int ResourceSpawnSeed;
        public int EventScheduleSeed;
        public int ScriptedWanderSeed;
        public string StopReason;
        public string RunId;
        public double StartedAtUnix;

        public static ExperimentRunMetadata FromConfig(SimulationConfig config, double startedAtUnix)
        {
            if (config == null)
            {
                return FromExperiment(null, startedAtUnix);
            }

            return FromExperiment(config.ToExperimentConfiguration(), startedAtUnix);
        }

        public static ExperimentRunMetadata FromExperiment(ExperimentConfiguration configuration, double startedAtUnix)
        {
            if (configuration == null)
            {
                return new ExperimentRunMetadata
                {
                    ExperimentName = "unnamed",
                    HerbivorePolicy = PolicyKindNames.ScriptedBaseline,
                    PredatorPolicy = PolicyKindNames.ScriptedBaseline,
                    EcosystemMode = EcosystemModeNames.Persistent,
                    TrainingRespawnEnabled = false,
                    ResourceAbundance = ExperimentConfiguration.DefaultResourceAbundance,
                    PlantRegenerationMultiplier = ExperimentConfiguration.DefaultPlantRegeneration,
                    MutationProbability = ExperimentConfiguration.DefaultMutationProbability,
                    MutationMagnitudeScale = ExperimentConfiguration.DefaultMutationMagnitude,
                    DayLengthSeconds = ExperimentConfiguration.DefaultDayLengthSeconds,
                    EnabledEnvironmentalEvents = Array.Empty<string>(),
                    StartedAtUnix = startedAtUnix
                };
            }

            var seeds = configuration.Seeds;
            var enabled = configuration.EnabledEnvironmentalEvents;
            var enabledCopy = new string[enabled.Length];
            Array.Copy(enabled, enabledCopy, enabled.Length);

            return new ExperimentRunMetadata
            {
                ExperimentName = configuration.ExperimentName,
                RandomSeed = configuration.RandomSeed,
                HerbivorePolicy = PolicyKindNames.ToWireName(configuration.HerbivorePolicy),
                PredatorPolicy = PolicyKindNames.ToWireName(configuration.PredatorPolicy),
                InitialHerbivores = configuration.InitialHerbivores,
                InitialPredators = configuration.InitialPredators,
                TimeScale = configuration.DefaultTimeScale,
                ScenarioId = configuration.ScenarioId,
                TrainingModelId = configuration.ModelId,
                CurriculumStageId = configuration.CurriculumStageId,
                EcosystemMode = EcosystemModeNames.ToWireName(configuration.EcosystemMode),
                TrainingRespawnEnabled = configuration.TrainingRespawnEnabled,
                MaxHerbivores = configuration.MaxHerbivores,
                MaxPredators = configuration.MaxPredators,
                ResourceAbundance = configuration.ResourceAbundance,
                PlantRegenerationMultiplier = configuration.PlantRegenerationMultiplier,
                MutationProbability = configuration.MutationProbability,
                MutationMagnitudeScale = configuration.MutationMagnitudeScale,
                DayLengthSeconds = configuration.DayLengthSeconds,
                EnabledEnvironmentalEvents = enabledCopy,
                MaxSimulationTimeSeconds = configuration.Stopping.MaxSimulationTimeSeconds,
                StopOnEcosystemExtinction = configuration.Stopping.StopOnEcosystemExtinction,
                FounderGenomeSeed = seeds.FounderGenomes,
                ReproductionSeed = seeds.Reproduction,
                ResourceSpawnSeed = seeds.ResourceSpawn,
                EventScheduleSeed = seeds.EventSchedule,
                ScriptedWanderSeed = seeds.ScriptedWander,
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
                ["max_predators"] = MaxPredators,
                ["resource_abundance"] = ResourceAbundance,
                ["plant_regeneration_multiplier"] = PlantRegenerationMultiplier,
                ["mutation_probability"] = MutationProbability,
                ["mutation_magnitude_scale"] = MutationMagnitudeScale,
                ["day_length_seconds"] = DayLengthSeconds,
                ["max_simulation_time_seconds"] = MaxSimulationTimeSeconds,
                ["stop_on_ecosystem_extinction"] = StopOnEcosystemExtinction,
                ["seed_founder_genomes"] = FounderGenomeSeed,
                ["seed_reproduction"] = ReproductionSeed,
                ["seed_resource_spawn"] = ResourceSpawnSeed,
                ["seed_event_schedule"] = EventScheduleSeed,
                ["seed_scripted_wander"] = ScriptedWanderSeed
            };

            if (EnabledEnvironmentalEvents != null && EnabledEnvironmentalEvents.Length > 0)
            {
                config["enabled_environmental_events"] = EnabledEnvironmentalEvents;
            }

            if (!string.IsNullOrEmpty(ScenarioId))
            {
                config["scenario_id"] = ScenarioId;
            }

            if (!string.IsNullOrEmpty(TrainingModelId))
            {
                config["training_model_id"] = TrainingModelId;
            }

            if (!string.IsNullOrEmpty(CurriculumStageId))
            {
                config["curriculum_stage_id"] = CurriculumStageId;
            }

            if (!string.IsNullOrEmpty(StopReason))
            {
                config["stop_reason"] = StopReason;
            }

            return config;
        }
    }
}
