using System;
using EvoLife.Common;

namespace EvoLife.Simulation
{
    public enum TrainingCurriculumFocus : byte
    {
        Herbivore = 0,
        Predator = 1,
        Combined = 2
    }

    /// <summary>
    /// Lightweight training stages. Scenes stay small; terrain is not required.
    /// These are curriculum configurations, not claimed performance schedules.
    /// </summary>
    public static class TrainingCurriculum
    {
        public const string Stage1Movement = "stage1_movement";
        public const string Stage2FoodWater = "stage2_food_water";
        public const string Stage3PredatorPrey = "stage3_predator_prey";
        public const string Stage4ResourceScarcity = "stage4_resource_scarcity";
        public const string Stage5PersistentEcosystem = "stage5_persistent_ecosystem";
        public const string Stage6ReproductionEvents = "stage6_reproduction_events";

        public static readonly string[] AllStages =
        {
            Stage1Movement,
            Stage2FoodWater,
            Stage3PredatorPrey,
            Stage4ResourceScarcity,
            Stage5PersistentEcosystem,
            Stage6ReproductionEvents
        };

        public static bool IsKnownStage(string stageId)
        {
            if (string.IsNullOrEmpty(stageId))
            {
                return false;
            }

            for (var i = 0; i < AllStages.Length; i++)
            {
                if (string.Equals(AllStages[i], stageId, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public static ExperimentConfiguration Create(
            int stageNumber,
            TrainingCurriculumFocus focus = TrainingCurriculumFocus.Combined,
            ExperimentConfiguration baseline = null)
        {
            return Create(StageId(stageNumber), focus, baseline);
        }

        public static ExperimentConfiguration Create(
            string stageId,
            TrainingCurriculumFocus focus = TrainingCurriculumFocus.Combined,
            ExperimentConfiguration baseline = null)
        {
            if (!TryCreate(stageId, focus, out var configuration, baseline))
            {
                throw new ExperimentConfigurationException("unknown curriculum stage '" + stageId + "'.");
            }

            return configuration;
        }

        public static bool TryCreate(
            string stageId,
            TrainingCurriculumFocus focus,
            out ExperimentConfiguration configuration,
            ExperimentConfiguration baseline = null)
        {
            configuration = null;
            if (string.IsNullOrWhiteSpace(stageId))
            {
                return false;
            }

            var id = stageId.Trim().ToLowerInvariant();
            if (!IsKnownStage(id))
            {
                return false;
            }

            var config = (baseline ?? ExperimentConfiguration.CreateDefault()).Clone();
            ApplyTrainingDefaults(config, focus);
            config.CurriculumStageId = id;
            config.ScenarioId = id;
            ApplyStage(config, id, focus);
            configuration = config;
            return true;
        }

        public static string StageId(int stageNumber)
        {
            switch (stageNumber)
            {
                case 1:
                    return Stage1Movement;
                case 2:
                    return Stage2FoodWater;
                case 3:
                    return Stage3PredatorPrey;
                case 4:
                    return Stage4ResourceScarcity;
                case 5:
                    return Stage5PersistentEcosystem;
                case 6:
                    return Stage6ReproductionEvents;
                default:
                    throw new ExperimentConfigurationException("curriculum stage must be 1-6.");
            }
        }

        static void ApplyTrainingDefaults(ExperimentConfiguration config, TrainingCurriculumFocus focus)
        {
            config.EcosystemMode = EcosystemMode.TrainingSupport;
            config.TrainingRespawnEnabled = true;
            config.TrainingRespawnIntervalSeconds = 2f;
            config.Stopping = ExperimentStoppingConditions.ForTrainingEpisode(180f);
            config.EnabledEnvironmentalEvents = Array.Empty<string>();
            config.ScheduledEvents = Array.Empty<ExperimentScheduledEvent>();
            config.MutationProbability = 0f;
            config.MutationMagnitudeScale = 0f;
            ApplyFocusPolicies(config, focus);
        }

        static void ApplyFocusPolicies(ExperimentConfiguration config, TrainingCurriculumFocus focus)
        {
            switch (focus)
            {
                case TrainingCurriculumFocus.Herbivore:
                    config.HerbivorePolicy = AgentPolicyKind.LearnedPpo;
                    config.PredatorPolicy = AgentPolicyKind.ScriptedBaseline;
                    break;
                case TrainingCurriculumFocus.Predator:
                    config.HerbivorePolicy = AgentPolicyKind.ScriptedBaseline;
                    config.PredatorPolicy = AgentPolicyKind.LearnedPpo;
                    break;
                case TrainingCurriculumFocus.Combined:
                    config.HerbivorePolicy = AgentPolicyKind.LearnedPpo;
                    config.PredatorPolicy = AgentPolicyKind.LearnedPpo;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(focus), focus, "Unhandled TrainingCurriculumFocus.");
            }
        }

        static void ApplyStage(ExperimentConfiguration config, string stageId, TrainingCurriculumFocus focus)
        {
            switch (stageId)
            {
                case Stage1Movement:
                    ApplyStage1(config, focus);
                    break;
                case Stage2FoodWater:
                    ApplyStage2(config, focus);
                    break;
                case Stage3PredatorPrey:
                    ApplyStage3(config, focus);
                    break;
                case Stage4ResourceScarcity:
                    ApplyStage4(config, focus);
                    break;
                case Stage5PersistentEcosystem:
                    ApplyStage5(config, focus);
                    break;
                case Stage6ReproductionEvents:
                    ApplyStage6(config, focus);
                    break;
                default:
                    throw new ExperimentConfigurationException("unknown curriculum stage '" + stageId + "'.");
            }
        }

        static void ApplyStage1(ExperimentConfiguration config, TrainingCurriculumFocus focus)
        {
            config.ExperimentName = "curriculum_" + Stage1Movement + "_" + FocusSuffix(focus);
            config.ResourceAbundance = 1.5f;
            config.PlantRegenerationMultiplier = 1.2f;
            config.DayLengthSeconds = 90f;
            config.Stopping = ExperimentStoppingConditions.ForTrainingEpisode(120f);
            if (focus == TrainingCurriculumFocus.Predator)
            {
                config.SetInitialPopulation(4, 4);
                config.MinHerbivores = 2;
                config.MinPredators = 2;
            }
            else
            {
                config.SetInitialPopulation(8, 0);
                config.MinHerbivores = 4;
                config.MinPredators = 0;
                config.MaxPredators = 0;
            }
        }

        static void ApplyStage2(ExperimentConfiguration config, TrainingCurriculumFocus focus)
        {
            config.ExperimentName = "curriculum_" + Stage2FoodWater + "_" + FocusSuffix(focus);
            config.ResourceAbundance = 0.8f;
            config.PlantRegenerationMultiplier = 0.9f;
            config.Stopping = ExperimentStoppingConditions.ForTrainingEpisode(180f);
            if (focus == TrainingCurriculumFocus.Predator)
            {
                config.SetInitialPopulation(8, 4);
                config.MinHerbivores = 4;
                config.MinPredators = 2;
            }
            else
            {
                config.SetInitialPopulation(12, 0);
                config.MinHerbivores = 4;
                config.MinPredators = 0;
                config.MaxPredators = 0;
            }
        }

        static void ApplyStage3(ExperimentConfiguration config, TrainingCurriculumFocus focus)
        {
            config.ExperimentName = "curriculum_" + Stage3PredatorPrey + "_" + FocusSuffix(focus);
            config.ResourceAbundance = 1f;
            config.PlantRegenerationMultiplier = 1f;
            config.SetInitialPopulation(16, 4);
            config.MinHerbivores = 6;
            config.MinPredators = 2;
            config.Stopping = ExperimentStoppingConditions.ForTrainingEpisode(240f);
        }

        static void ApplyStage4(ExperimentConfiguration config, TrainingCurriculumFocus focus)
        {
            config.ExperimentName = "curriculum_" + Stage4ResourceScarcity + "_" + FocusSuffix(focus);
            config.ResourceAbundance = 0.35f;
            config.PlantRegenerationMultiplier = 0.5f;
            config.SetInitialPopulation(16, 4);
            config.MinHerbivores = 4;
            config.MinPredators = 2;
            config.Stopping = ExperimentStoppingConditions.ForTrainingEpisode(240f);
        }

        static void ApplyStage5(ExperimentConfiguration config, TrainingCurriculumFocus focus)
        {
            config.ExperimentName = "curriculum_" + Stage5PersistentEcosystem + "_" + FocusSuffix(focus);
            config.EcosystemMode = EcosystemMode.Persistent;
            config.TrainingRespawnEnabled = false;
            config.ResourceAbundance = 1f;
            config.PlantRegenerationMultiplier = 1f;
            config.SetInitialPopulation(20, 5);
            config.MinHerbivores = 0;
            config.MinPredators = 0;
            config.Stopping = ExperimentStoppingConditions.ForPersistentEcosystem(600f);
        }

        static void ApplyStage6(ExperimentConfiguration config, TrainingCurriculumFocus focus)
        {
            config.ExperimentName = "curriculum_" + Stage6ReproductionEvents + "_" + FocusSuffix(focus);
            config.EcosystemMode = EcosystemMode.Persistent;
            config.TrainingRespawnEnabled = false;
            config.MutationProbability = ExperimentConfiguration.DefaultMutationProbability;
            config.MutationMagnitudeScale = ExperimentConfiguration.DefaultMutationMagnitude;
            config.ResourceAbundance = 1f;
            config.PlantRegenerationMultiplier = 1f;
            config.SetInitialPopulation(20, 5);
            config.MinHerbivores = 0;
            config.MinPredators = 0;
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
                    AtSimulationTime = 120f
                },
                new ExperimentScheduledEvent
                {
                    Kind = EnvironmentalEventKindNames.FoodBoom,
                    AtSimulationTime = 240f
                }
            };
            config.Stopping = ExperimentStoppingConditions.ForPersistentEcosystem(900f);
        }

        static string FocusSuffix(TrainingCurriculumFocus focus)
        {
            switch (focus)
            {
                case TrainingCurriculumFocus.Herbivore:
                    return "herbivore";
                case TrainingCurriculumFocus.Predator:
                    return "predator";
                case TrainingCurriculumFocus.Combined:
                    return "combined";
                default:
                    throw new ArgumentOutOfRangeException(nameof(focus), focus, "Unhandled TrainingCurriculumFocus.");
            }
        }
    }
}
