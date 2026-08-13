using System.Collections.Generic;
using EvoLife.Common;

namespace EvoLife.Simulation
{
    /// <summary>
    /// Validation messages for an <see cref="ExperimentConfiguration"/>.
    /// An empty list means the configuration is usable.
    /// </summary>
    public static class ExperimentConfigurationValidator
    {
        public static IReadOnlyList<string> Validate(ExperimentConfiguration configuration)
        {
            var errors = new List<string>();
            if (configuration == null)
            {
                errors.Add("configuration is required.");
                return errors;
            }

            if (string.IsNullOrWhiteSpace(configuration.ExperimentName))
            {
                errors.Add("experiment name is required.");
            }

            if (configuration.InitialHerbivores < 0)
            {
                errors.Add("initial herbivore count must be >= 0.");
            }

            if (configuration.InitialPredators < 0)
            {
                errors.Add("initial predator count must be >= 0.");
            }

            if (configuration.ResourceAbundance < 0f)
            {
                errors.Add("resource abundance must be >= 0.");
            }

            if (configuration.PlantRegenerationMultiplier < 0f)
            {
                errors.Add("plant regeneration multiplier must be >= 0.");
            }

            if (configuration.MutationProbability < 0f || configuration.MutationProbability > 1f)
            {
                errors.Add("mutation probability must be in [0, 1].");
            }

            if (configuration.MutationMagnitudeScale < 0f)
            {
                errors.Add("mutation magnitude scale must be >= 0.");
            }

            if (configuration.DayLengthSeconds <= 0f)
            {
                errors.Add("day length must be > 0.");
            }

            if (configuration.DefaultTimeScale < 0f)
            {
                errors.Add("time scale must be >= 0.");
            }

            if (configuration.MaxHerbivores < 0)
            {
                errors.Add("max herbivores must be >= 0.");
            }

            if (configuration.MaxPredators < 0)
            {
                errors.Add("max predators must be >= 0.");
            }

            if (configuration.MinHerbivores < 0)
            {
                errors.Add("min herbivores must be >= 0.");
            }

            if (configuration.MinPredators < 0)
            {
                errors.Add("min predators must be >= 0.");
            }

            if (configuration.MaxHerbivores > 0 && configuration.MinHerbivores > configuration.MaxHerbivores)
            {
                errors.Add("min herbivores must be <= max herbivores.");
            }

            if (configuration.MaxPredators > 0 && configuration.MinPredators > configuration.MaxPredators)
            {
                errors.Add("min predators must be <= max predators.");
            }

            if (configuration.MaxHerbivores > 0 && configuration.InitialHerbivores > configuration.MaxHerbivores)
            {
                errors.Add("initial herbivores must be <= max herbivores.");
            }

            if (configuration.MaxPredators > 0 && configuration.InitialPredators > configuration.MaxPredators)
            {
                errors.Add("initial predators must be <= max predators.");
            }

            if (configuration.FounderSpawnRadius < 0f)
            {
                errors.Add("founder spawn radius must be >= 0.");
            }

            if (configuration.TrainingRespawnIntervalSeconds < 0f)
            {
                errors.Add("training respawn interval must be >= 0.");
            }

            if (configuration.TrainingRespawnEnabled && configuration.EcosystemMode != EcosystemMode.TrainingSupport)
            {
                errors.Add("training respawn requires ecosystem mode training_support.");
            }

            if (configuration.Stopping == null)
            {
                errors.Add("stopping conditions are required.");
            }
            else if (configuration.Stopping.MaxSimulationTimeSeconds < 0f)
            {
                errors.Add("max simulation time must be >= 0 (0 disables the time limit).");
            }

            var enabled = configuration.EnabledEnvironmentalEvents;
            for (var i = 0; i < enabled.Length; i++)
            {
                if (!EnvironmentalEventKindNames.TryParse(enabled[i], out _))
                {
                    errors.Add("unknown enabled environmental event '" + enabled[i] + "'.");
                }
            }

            var scheduled = configuration.ScheduledEvents;
            for (var i = 0; i < scheduled.Length; i++)
            {
                if (scheduled[i] == null)
                {
                    errors.Add("scheduled event at index " + i + " is null.");
                    continue;
                }

                if (!scheduled[i].TryGetKind(out _))
                {
                    errors.Add("unknown scheduled environmental event '" + scheduled[i].Kind + "'.");
                }

                if (scheduled[i].AtSimulationTime < 0f)
                {
                    errors.Add("scheduled event time must be >= 0.");
                }
            }

            if (!string.IsNullOrEmpty(configuration.CurriculumStageId)
                && !TrainingCurriculum.IsKnownStage(configuration.CurriculumStageId))
            {
                errors.Add("unknown curriculum stage '" + configuration.CurriculumStageId + "'.");
            }

            return errors;
        }

        public static bool IsValid(ExperimentConfiguration configuration) => Validate(configuration).Count == 0;

        public static void ThrowIfInvalid(ExperimentConfiguration configuration)
        {
            var errors = Validate(configuration);
            if (errors.Count == 0)
            {
                return;
            }

            throw new ExperimentConfigurationException(string.Join(" ", errors));
        }
    }

    public sealed class ExperimentConfigurationException : System.ArgumentException
    {
        public ExperimentConfigurationException(string message)
            : base(message)
        {
        }
    }
}
