using System;
using UnityEngine;
using EvoLife.Common;

namespace EvoLife.Simulation
{
    /// <summary>
    /// JSON DTO for <see cref="ExperimentConfiguration"/>. Field names are snake_case
    /// so Python, Unity, and the analytics backend share one document shape.
    /// </summary>
    [Serializable]
    public sealed class ExperimentConfigurationJson
    {
        public string experiment_name;
        public int random_seed;
        public int initial_herbivores;
        public int initial_predators;
        public float resource_abundance;
        public float plant_regeneration_multiplier;
        public float mutation_probability;
        public float mutation_magnitude_scale;
        public float day_length_seconds;
        public string[] enabled_environmental_events;
        public ExperimentScheduledEventJson[] scheduled_events;
        public string herbivore_policy;
        public string predator_policy;
        public int max_herbivores;
        public int max_predators;
        public int min_herbivores;
        public int min_predators;
        public string ecosystem_mode;
        public bool training_respawn_enabled;
        public float training_respawn_interval_seconds;
        public float founder_spawn_radius;
        public float default_time_scale;
        public string scenario_id;
        public string model_id;
        public string curriculum_stage_id;
        public float predator_speed_bias;
        public float max_simulation_time_seconds;
        public bool stop_on_ecosystem_extinction;
        public bool stop_on_herbivore_extinction;
        public bool stop_on_predator_extinction;
    }

    [Serializable]
    public sealed class ExperimentScheduledEventJson
    {
        public string kind;
        public float at_simulation_time;
    }

    /// <summary>
    /// JsonUtility round-trip for experiment configs. Does not dump Unity assets.
    /// </summary>
    public static class ExperimentConfigurationSerializer
    {
        public static string ToJson(ExperimentConfiguration configuration, bool prettyPrint = true)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            return JsonUtility.ToJson(ToDto(configuration), prettyPrint);
        }

        public static ExperimentConfiguration FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ExperimentConfigurationException("experiment JSON is empty.");
            }

            var dto = JsonUtility.FromJson<ExperimentConfigurationJson>(json);
            if (dto == null)
            {
                throw new ExperimentConfigurationException("experiment JSON could not be parsed.");
            }

            return FromDto(dto);
        }

        public static ExperimentConfigurationJson ToDto(ExperimentConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            var scheduled = configuration.ScheduledEvents;
            var scheduledDto = new ExperimentScheduledEventJson[scheduled.Length];
            for (var i = 0; i < scheduled.Length; i++)
            {
                if (scheduled[i] == null)
                {
                    continue;
                }

                scheduledDto[i] = new ExperimentScheduledEventJson
                {
                    kind = scheduled[i].Kind,
                    at_simulation_time = scheduled[i].AtSimulationTime
                };
            }

            var enabled = configuration.EnabledEnvironmentalEvents;
            var enabledCopy = new string[enabled.Length];
            Array.Copy(enabled, enabledCopy, enabled.Length);

            return new ExperimentConfigurationJson
            {
                experiment_name = configuration.ExperimentName,
                random_seed = configuration.RandomSeed,
                initial_herbivores = configuration.InitialHerbivores,
                initial_predators = configuration.InitialPredators,
                resource_abundance = configuration.ResourceAbundance,
                plant_regeneration_multiplier = configuration.PlantRegenerationMultiplier,
                mutation_probability = configuration.MutationProbability,
                mutation_magnitude_scale = configuration.MutationMagnitudeScale,
                day_length_seconds = configuration.DayLengthSeconds,
                enabled_environmental_events = enabledCopy,
                scheduled_events = scheduledDto,
                herbivore_policy = PolicyKindNames.ToWireName(configuration.HerbivorePolicy),
                predator_policy = PolicyKindNames.ToWireName(configuration.PredatorPolicy),
                max_herbivores = configuration.MaxHerbivores,
                max_predators = configuration.MaxPredators,
                min_herbivores = configuration.MinHerbivores,
                min_predators = configuration.MinPredators,
                ecosystem_mode = EcosystemModeNames.ToWireName(configuration.EcosystemMode),
                training_respawn_enabled = configuration.TrainingRespawnEnabled,
                training_respawn_interval_seconds = configuration.TrainingRespawnIntervalSeconds,
                founder_spawn_radius = configuration.FounderSpawnRadius,
                default_time_scale = configuration.DefaultTimeScale,
                scenario_id = configuration.ScenarioId,
                model_id = configuration.ModelId,
                curriculum_stage_id = configuration.CurriculumStageId,
                predator_speed_bias = configuration.PredatorSpeedBias,
                max_simulation_time_seconds = configuration.Stopping.MaxSimulationTimeSeconds,
                stop_on_ecosystem_extinction = configuration.Stopping.StopOnEcosystemExtinction,
                stop_on_herbivore_extinction = configuration.Stopping.StopOnHerbivoreExtinction,
                stop_on_predator_extinction = configuration.Stopping.StopOnPredatorExtinction
            };
        }

        public static ExperimentConfiguration FromDto(ExperimentConfigurationJson dto)
        {
            if (dto == null)
            {
                throw new ArgumentNullException(nameof(dto));
            }

            if (!PolicyKindNames.TryParse(
                    string.IsNullOrEmpty(dto.herbivore_policy) ? PolicyKindNames.ScriptedBaseline : dto.herbivore_policy,
                    out var herbivorePolicy))
            {
                throw new ExperimentConfigurationException("invalid herbivore policy '" + dto.herbivore_policy + "'.");
            }

            if (!PolicyKindNames.TryParse(
                    string.IsNullOrEmpty(dto.predator_policy) ? PolicyKindNames.ScriptedBaseline : dto.predator_policy,
                    out var predatorPolicy))
            {
                throw new ExperimentConfigurationException("invalid predator policy '" + dto.predator_policy + "'.");
            }

            if (!EcosystemModeNames.TryParse(
                    string.IsNullOrEmpty(dto.ecosystem_mode) ? EcosystemModeNames.Persistent : dto.ecosystem_mode,
                    out var ecosystemMode))
            {
                throw new ExperimentConfigurationException("invalid ecosystem mode '" + dto.ecosystem_mode + "'.");
            }

            var scheduledSource = dto.scheduled_events ?? Array.Empty<ExperimentScheduledEventJson>();
            var scheduled = new ExperimentScheduledEvent[scheduledSource.Length];
            for (var i = 0; i < scheduledSource.Length; i++)
            {
                if (scheduledSource[i] == null)
                {
                    continue;
                }

                scheduled[i] = new ExperimentScheduledEvent
                {
                    Kind = scheduledSource[i].kind,
                    AtSimulationTime = scheduledSource[i].at_simulation_time
                };
            }

            return new ExperimentConfiguration
            {
                ExperimentName = string.IsNullOrEmpty(dto.experiment_name) ? "baseline" : dto.experiment_name,
                RandomSeed = dto.random_seed,
                InitialHerbivores = dto.initial_herbivores,
                InitialPredators = dto.initial_predators,
                ResourceAbundance = dto.resource_abundance,
                PlantRegenerationMultiplier = dto.plant_regeneration_multiplier,
                MutationProbability = dto.mutation_probability,
                MutationMagnitudeScale = dto.mutation_magnitude_scale,
                DayLengthSeconds = dto.day_length_seconds <= 0f
                    ? ExperimentConfiguration.DefaultDayLengthSeconds
                    : dto.day_length_seconds,
                EnabledEnvironmentalEvents = dto.enabled_environmental_events ?? Array.Empty<string>(),
                ScheduledEvents = scheduled,
                HerbivorePolicy = herbivorePolicy,
                PredatorPolicy = predatorPolicy,
                MaxHerbivores = dto.max_herbivores,
                MaxPredators = dto.max_predators,
                MinHerbivores = dto.min_herbivores,
                MinPredators = dto.min_predators,
                EcosystemMode = ecosystemMode,
                TrainingRespawnEnabled = dto.training_respawn_enabled,
                TrainingRespawnIntervalSeconds = dto.training_respawn_interval_seconds,
                FounderSpawnRadius = dto.founder_spawn_radius,
                DefaultTimeScale = dto.default_time_scale <= 0f ? 1f : dto.default_time_scale,
                ScenarioId = dto.scenario_id ?? "",
                ModelId = dto.model_id ?? "",
                CurriculumStageId = dto.curriculum_stage_id ?? "",
                PredatorSpeedBias = dto.predator_speed_bias,
                Stopping = new ExperimentStoppingConditions
                {
                    MaxSimulationTimeSeconds = dto.max_simulation_time_seconds,
                    StopOnEcosystemExtinction = dto.stop_on_ecosystem_extinction,
                    StopOnHerbivoreExtinction = dto.stop_on_herbivore_extinction,
                    StopOnPredatorExtinction = dto.stop_on_predator_extinction
                }
            };
        }
    }
}
