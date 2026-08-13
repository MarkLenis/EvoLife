using System;
using System.Collections.Generic;
using EvoLife.Common;

namespace EvoLife.Simulation
{
    /// <summary>
    /// Starter evaluation scenarios. These only change configuration knobs.
    /// They do not claim expected population or training outcomes.
    /// </summary>
    public static class ExperimentScenarios
    {
        public const string NormalControl = "normal_control";
        public const string ReducedFood = "reduced_food";
        public const string Drought = "drought";
        public const string FastPredators = "fast_predators";
        public const string HighMutation = "high_mutation";
        public const string LowMutation = "low_mutation";
        public const string PredatorPressure = "predator_pressure";
        public const string RecoveryAfterEvent = "recovery_after_event";

        public static readonly string[] All =
        {
            NormalControl,
            ReducedFood,
            Drought,
            FastPredators,
            HighMutation,
            LowMutation,
            PredatorPressure,
            RecoveryAfterEvent
        };

        public static bool IsKnown(string scenarioId)
        {
            if (string.IsNullOrEmpty(scenarioId))
            {
                return false;
            }

            for (var i = 0; i < All.Length; i++)
            {
                if (string.Equals(All[i], scenarioId, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public static ExperimentConfiguration Create(string scenarioId, ExperimentConfiguration baseline = null)
        {
            if (!TryCreate(scenarioId, out var configuration, baseline))
            {
                throw new ExperimentConfigurationException("unknown scenario id '" + scenarioId + "'.");
            }

            return configuration;
        }

        public static bool TryCreate(
            string scenarioId,
            out ExperimentConfiguration configuration,
            ExperimentConfiguration baseline = null)
        {
            configuration = null;
            if (string.IsNullOrWhiteSpace(scenarioId))
            {
                return false;
            }

            var id = scenarioId.Trim().ToLowerInvariant();
            var config = (baseline ?? ExperimentConfiguration.CreateDefault()).Clone();
            config.ScenarioId = id;
            ApplyOverrides(config, id);
            configuration = config;
            return IsKnown(id);
        }

        static void ApplyOverrides(ExperimentConfiguration config, string scenarioId)
        {
            switch (scenarioId)
            {
                case NormalControl:
                    ApplyNormalControl(config);
                    break;
                case ReducedFood:
                    ApplyReducedFood(config);
                    break;
                case Drought:
                    ApplyDrought(config);
                    break;
                case FastPredators:
                    ApplyFastPredators(config);
                    break;
                case HighMutation:
                    ApplyHighMutation(config);
                    break;
                case LowMutation:
                    ApplyLowMutation(config);
                    break;
                case PredatorPressure:
                    ApplyPredatorPressure(config);
                    break;
                case RecoveryAfterEvent:
                    ApplyRecoveryAfterEvent(config);
                    break;
                default:
                    throw new ExperimentConfigurationException("unknown scenario id '" + scenarioId + "'.");
            }
        }

        static void ApplyNormalControl(ExperimentConfiguration config)
        {
            config.ExperimentName = NormalControl;
            config.ResourceAbundance = 1f;
            config.PlantRegenerationMultiplier = 1f;
            config.MutationProbability = ExperimentConfiguration.DefaultMutationProbability;
            config.MutationMagnitudeScale = ExperimentConfiguration.DefaultMutationMagnitude;
            config.EnabledEnvironmentalEvents = Array.Empty<string>();
            config.ScheduledEvents = Array.Empty<ExperimentScheduledEvent>();
            config.PredatorSpeedBias = 0f;
            config.EcosystemMode = EcosystemMode.Persistent;
            config.TrainingRespawnEnabled = false;
            config.Stopping = ExperimentStoppingConditions.ForPersistentEcosystem(600f);
        }

        static void ApplyReducedFood(ExperimentConfiguration config)
        {
            ApplyNormalControl(config);
            config.ExperimentName = ReducedFood;
            config.ScenarioId = ReducedFood;
            config.ResourceAbundance = 0.35f;
            config.PlantRegenerationMultiplier = 0.7f;
        }

        static void ApplyDrought(ExperimentConfiguration config)
        {
            ApplyNormalControl(config);
            config.ExperimentName = Drought;
            config.ScenarioId = Drought;
            config.ResourceAbundance = 0.7f;
            config.PlantRegenerationMultiplier = 0.4f;
            config.EnabledEnvironmentalEvents = new[] { EnvironmentalEventKindNames.Drought };
            config.ScheduledEvents = new[]
            {
                new ExperimentScheduledEvent
                {
                    Kind = EnvironmentalEventKindNames.Drought,
                    AtSimulationTime = 60f
                }
            };
        }

        static void ApplyFastPredators(ExperimentConfiguration config)
        {
            ApplyNormalControl(config);
            config.ExperimentName = FastPredators;
            config.ScenarioId = FastPredators;
            config.PredatorSpeedBias = 1.2f;
            config.InitialPredators = Math.Max(config.InitialPredators, 7);
        }

        static void ApplyHighMutation(ExperimentConfiguration config)
        {
            ApplyNormalControl(config);
            config.ExperimentName = HighMutation;
            config.ScenarioId = HighMutation;
            config.MutationProbability = 0.45f;
            config.MutationMagnitudeScale = 2.5f;
        }

        static void ApplyLowMutation(ExperimentConfiguration config)
        {
            ApplyNormalControl(config);
            config.ExperimentName = LowMutation;
            config.ScenarioId = LowMutation;
            config.MutationProbability = 0.02f;
            config.MutationMagnitudeScale = 0.25f;
        }

        static void ApplyPredatorPressure(ExperimentConfiguration config)
        {
            ApplyNormalControl(config);
            config.ExperimentName = PredatorPressure;
            config.ScenarioId = PredatorPressure;
            config.InitialHerbivores = 16;
            config.InitialPredators = 12;
            config.MaxPredators = Math.Max(config.MaxPredators, 32);
            config.MinPredators = Math.Max(config.MinPredators, 0);
        }

        static void ApplyRecoveryAfterEvent(ExperimentConfiguration config)
        {
            ApplyNormalControl(config);
            config.ExperimentName = RecoveryAfterEvent;
            config.ScenarioId = RecoveryAfterEvent;
            config.EnabledEnvironmentalEvents = new[]
            {
                EnvironmentalEventKindNames.Drought,
                EnvironmentalEventKindNames.FoodBoom
            };
            config.ScheduledEvents = new[]
            {
                new ExperimentScheduledEvent
                {
                    Kind = EnvironmentalEventKindNames.Drought,
                    AtSimulationTime = 40f
                },
                new ExperimentScheduledEvent
                {
                    Kind = EnvironmentalEventKindNames.FoodBoom,
                    AtSimulationTime = 100f
                }
            };
            config.Stopping = ExperimentStoppingConditions.ForPersistentEcosystem(300f);
        }

        public static IReadOnlyDictionary<string, ExperimentConfiguration> CreateAll()
        {
            var map = new Dictionary<string, ExperimentConfiguration>(All.Length);
            for (var i = 0; i < All.Length; i++)
            {
                map[All[i]] = Create(All[i]);
            }

            return map;
        }
    }
}
