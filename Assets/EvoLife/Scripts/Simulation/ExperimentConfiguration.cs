using System;
using System.Collections.Generic;
using UnityEngine;
using EvoLife.Common;
using EvoLife.Genetics;

namespace EvoLife.Simulation
{
    /// <summary>
    /// Serializable, reproducible experiment settings. Unity inspector and JSON share this type.
    /// Runtime systems still own their own objects; this model is applied onto them.
    /// </summary>
    [Serializable]
    public sealed class ExperimentConfiguration
    {
        public const float DefaultResourceAbundance = 1f;
        public const float DefaultPlantRegeneration = 1f;
        public const float DefaultDayLengthSeconds = 120f;
        public const float DefaultMutationProbability = 0.15f;
        public const float DefaultMutationMagnitude = 1f;

        [SerializeField] string experimentName = "baseline";
        [SerializeField] int randomSeed = 42;
        [SerializeField] int initialHerbivores = 20;
        [SerializeField] int initialPredators = 5;
        [SerializeField] float resourceAbundance = DefaultResourceAbundance;
        [SerializeField] float plantRegenerationMultiplier = DefaultPlantRegeneration;
        [SerializeField] float mutationProbability = DefaultMutationProbability;
        [SerializeField] float mutationMagnitudeScale = DefaultMutationMagnitude;
        [SerializeField] float dayLengthSeconds = DefaultDayLengthSeconds;
        [SerializeField] string[] enabledEnvironmentalEvents = new string[0];
        [SerializeField] ExperimentScheduledEvent[] scheduledEvents = new ExperimentScheduledEvent[0];
        [SerializeField] AgentPolicyKind herbivorePolicy = AgentPolicyKind.ScriptedBaseline;
        [SerializeField] AgentPolicyKind predatorPolicy = AgentPolicyKind.ScriptedBaseline;
        [SerializeField] int maxHerbivores = 80;
        [SerializeField] int maxPredators = 24;
        [SerializeField] int minHerbivores = 4;
        [SerializeField] int minPredators = 2;
        [SerializeField] EcosystemMode ecosystemMode = EcosystemMode.Persistent;
        [SerializeField] bool trainingRespawnEnabled;
        [SerializeField] float trainingRespawnIntervalSeconds = 2f;
        [SerializeField] float founderSpawnRadius = 12f;
        [SerializeField] float defaultTimeScale = 1f;
        [SerializeField] string scenarioId = "";
        [SerializeField] string modelId = "";
        [SerializeField] string curriculumStageId = "";
        [SerializeField] float predatorSpeedBias;
        [SerializeField] ExperimentStoppingConditions stopping = new ExperimentStoppingConditions();

        public string ExperimentName
        {
            get => string.IsNullOrEmpty(experimentName) ? "baseline" : experimentName;
            set => experimentName = value;
        }

        public int RandomSeed
        {
            get => randomSeed;
            set => randomSeed = value;
        }

        public int InitialHerbivores
        {
            get => initialHerbivores;
            set => initialHerbivores = value;
        }

        public int InitialPredators
        {
            get => initialPredators;
            set => initialPredators = value;
        }

        public float ResourceAbundance
        {
            get => resourceAbundance;
            set => resourceAbundance = value;
        }

        public float PlantRegenerationMultiplier
        {
            get => plantRegenerationMultiplier;
            set => plantRegenerationMultiplier = value;
        }

        public float MutationProbability
        {
            get => mutationProbability;
            set => mutationProbability = value;
        }

        public float MutationMagnitudeScale
        {
            get => mutationMagnitudeScale;
            set => mutationMagnitudeScale = value;
        }

        public float DayLengthSeconds
        {
            get => dayLengthSeconds;
            set => dayLengthSeconds = value;
        }

        public string[] EnabledEnvironmentalEvents
        {
            get => enabledEnvironmentalEvents ?? (enabledEnvironmentalEvents = new string[0]);
            set => enabledEnvironmentalEvents = value ?? new string[0];
        }

        public ExperimentScheduledEvent[] ScheduledEvents
        {
            get => scheduledEvents ?? (scheduledEvents = new ExperimentScheduledEvent[0]);
            set => scheduledEvents = value ?? new ExperimentScheduledEvent[0];
        }

        public AgentPolicyKind HerbivorePolicy
        {
            get => herbivorePolicy;
            set => herbivorePolicy = value;
        }

        public AgentPolicyKind PredatorPolicy
        {
            get => predatorPolicy;
            set => predatorPolicy = value;
        }

        public int MaxHerbivores
        {
            get => maxHerbivores;
            set => maxHerbivores = value;
        }

        public int MaxPredators
        {
            get => maxPredators;
            set => maxPredators = value;
        }

        public int MinHerbivores
        {
            get => minHerbivores;
            set => minHerbivores = value;
        }

        public int MinPredators
        {
            get => minPredators;
            set => minPredators = value;
        }

        public EcosystemMode EcosystemMode
        {
            get => ecosystemMode;
            set => ecosystemMode = value;
        }

        public bool TrainingRespawnEnabled
        {
            get => trainingRespawnEnabled;
            set => trainingRespawnEnabled = value;
        }

        public float TrainingRespawnIntervalSeconds
        {
            get => trainingRespawnIntervalSeconds;
            set => trainingRespawnIntervalSeconds = value;
        }

        public float FounderSpawnRadius
        {
            get => founderSpawnRadius;
            set => founderSpawnRadius = value;
        }

        public float DefaultTimeScale
        {
            get => defaultTimeScale;
            set => defaultTimeScale = value;
        }

        public string ScenarioId
        {
            get => scenarioId ?? "";
            set => scenarioId = value ?? "";
        }

        public string ModelId
        {
            get => modelId ?? "";
            set => modelId = value ?? "";
        }

        public string CurriculumStageId
        {
            get => curriculumStageId ?? "";
            set => curriculumStageId = value ?? "";
        }

        public float PredatorSpeedBias
        {
            get => predatorSpeedBias;
            set => predatorSpeedBias = value;
        }

        public ExperimentStoppingConditions Stopping
        {
            get
            {
                if (stopping == null)
                {
                    stopping = new ExperimentStoppingConditions();
                }

                return stopping;
            }
            set => stopping = value ?? new ExperimentStoppingConditions();
        }

        public DeterministicSeedTable Seeds => new DeterministicSeedTable(RandomSeed);

        public void SetInitialPopulation(int herbivores, int predators)
        {
            InitialHerbivores = herbivores;
            InitialPredators = predators;
        }

        public AgentPolicyKind PolicyFor(CreatureRole role)
        {
            switch (role)
            {
                case CreatureRole.Herbivore:
                    return HerbivorePolicy;
                case CreatureRole.Predator:
                    return PredatorPolicy;
                default:
                    throw new ArgumentOutOfRangeException(nameof(role), role, "Unhandled CreatureRole.");
            }
        }

        public EcosystemSettings ToEcosystemSettings()
        {
            return new EcosystemSettings
            {
                Mode = EcosystemMode,
                TrainingRespawnEnabled = TrainingRespawnEnabled,
                MaxHerbivores = MaxHerbivores,
                MaxPredators = MaxPredators,
                MinHerbivores = MinHerbivores,
                MinPredators = MinPredators,
                TrainingRespawnIntervalSeconds = TrainingRespawnIntervalSeconds,
                FounderSpawnRadius = FounderSpawnRadius
            };
        }

        public void ApplyToEcosystem(EcosystemSettings target)
        {
            if (target == null)
            {
                return;
            }

            target.Mode = EcosystemMode;
            target.TrainingRespawnEnabled = TrainingRespawnEnabled;
            target.MaxHerbivores = MaxHerbivores;
            target.MaxPredators = MaxPredators;
            target.MinHerbivores = MinHerbivores;
            target.MinPredators = MinPredators;
            target.TrainingRespawnIntervalSeconds = TrainingRespawnIntervalSeconds;
            target.FounderSpawnRadius = FounderSpawnRadius;
        }

        public void ApplyMutationTo(ReproductionSettings settings)
        {
            if (settings == null)
            {
                return;
            }

            settings.MutationProbability = MutationProbability;
            settings.MutationMagnitudeScale = MutationMagnitudeScale;
        }

        public GeneticsConfig ToGeneticsConfig() =>
            new GeneticsConfig(
                CrossoverConfig.Default,
                new MutationConfig(MutationProbability, MutationMagnitudeScale));

        public IReadOnlyList<EnvironmentalEventKind> ResolveEnabledEvents()
        {
            var names = EnabledEnvironmentalEvents;
            var kinds = new List<EnvironmentalEventKind>(names.Length);
            for (var i = 0; i < names.Length; i++)
            {
                if (EnvironmentalEventKindNames.TryParse(names[i], out var kind) && !kinds.Contains(kind))
                {
                    kinds.Add(kind);
                }
            }

            return kinds;
        }

        public ExperimentConfiguration Clone()
        {
            var copy = new ExperimentConfiguration
            {
                ExperimentName = ExperimentName,
                RandomSeed = RandomSeed,
                InitialHerbivores = InitialHerbivores,
                InitialPredators = InitialPredators,
                ResourceAbundance = ResourceAbundance,
                PlantRegenerationMultiplier = PlantRegenerationMultiplier,
                MutationProbability = MutationProbability,
                MutationMagnitudeScale = MutationMagnitudeScale,
                DayLengthSeconds = DayLengthSeconds,
                HerbivorePolicy = HerbivorePolicy,
                PredatorPolicy = PredatorPolicy,
                MaxHerbivores = MaxHerbivores,
                MaxPredators = MaxPredators,
                MinHerbivores = MinHerbivores,
                MinPredators = MinPredators,
                EcosystemMode = EcosystemMode,
                TrainingRespawnEnabled = TrainingRespawnEnabled,
                TrainingRespawnIntervalSeconds = TrainingRespawnIntervalSeconds,
                FounderSpawnRadius = FounderSpawnRadius,
                DefaultTimeScale = DefaultTimeScale,
                ScenarioId = ScenarioId,
                ModelId = ModelId,
                CurriculumStageId = CurriculumStageId,
                PredatorSpeedBias = PredatorSpeedBias,
                Stopping = Stopping.Clone()
            };

            var enabled = EnabledEnvironmentalEvents;
            copy.EnabledEnvironmentalEvents = new string[enabled.Length];
            Array.Copy(enabled, copy.EnabledEnvironmentalEvents, enabled.Length);

            var scheduled = ScheduledEvents;
            copy.ScheduledEvents = new ExperimentScheduledEvent[scheduled.Length];
            for (var i = 0; i < scheduled.Length; i++)
            {
                copy.ScheduledEvents[i] = scheduled[i] != null ? scheduled[i].Clone() : null;
            }

            return copy;
        }

        public static ExperimentConfiguration CreateDefault() => new ExperimentConfiguration();
    }

    /// <summary>
    /// Snapshot of derived seeds for one master seed. Useful for metadata and tests.
    /// </summary>
    public readonly struct DeterministicSeedTable
    {
        public DeterministicSeedTable(int masterSeed)
        {
            MasterSeed = masterSeed;
            FounderGenomes = DeterministicSeeds.FounderGenomes(masterSeed);
            Reproduction = DeterministicSeeds.Reproduction(masterSeed);
            ResourceSpawn = DeterministicSeeds.ResourceSpawn(masterSeed);
            EventSchedule = DeterministicSeeds.EventSchedule(masterSeed);
            TrainingRespawn = DeterministicSeeds.TrainingRespawn(masterSeed);
            EnvironmentalCreatures = DeterministicSeeds.EnvironmentalCreatures(masterSeed);
            ScriptedWander = DeterministicSeeds.Combine(masterSeed, DeterministicSeeds.ScriptedWanderOffset);
        }

        public int MasterSeed { get; }
        public int FounderGenomes { get; }
        public int Reproduction { get; }
        public int ResourceSpawn { get; }
        public int EventSchedule { get; }
        public int TrainingRespawn { get; }
        public int EnvironmentalCreatures { get; }
        public int ScriptedWander { get; }
    }

    [CreateAssetMenu(fileName = "ExperimentConfiguration", menuName = "EvoLife/Simulation/Experiment Configuration")]
    public sealed class ExperimentConfigurationAsset : ScriptableObject
    {
        [SerializeField] ExperimentConfiguration configuration = new ExperimentConfiguration();

        public ExperimentConfiguration Configuration
        {
            get
            {
                if (configuration == null)
                {
                    configuration = ExperimentConfiguration.CreateDefault();
                }

                return configuration;
            }
        }
    }
}
