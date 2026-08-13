using UnityEngine;
using EvoLife.Common;
using EvoLife.Environment;

namespace EvoLife.Simulation
{
    /// <summary>
    /// Thin experiment coordinator for founders, extinction reporting, and wiring.
    /// Reproduction, respawn, and population counts stay in their dedicated types.
    /// </summary>
    public sealed class EcosystemManager : MonoBehaviour
    {
        [SerializeField] SimulationConfig config;
        [SerializeField] SimulationClock clock;
        [SerializeField] CreatureSpawner spawner;
        [SerializeField] PopulationTracker populationTracker;
        [SerializeField] CreatureLifecycleHub lifecycleHub;
        [SerializeField] ReproductionSystem reproduction;
        [SerializeField] TrainingRespawnController trainingRespawn;
        [SerializeField] GameObject herbivorePrefab;
        [SerializeField] GameObject predatorPrefab;
        [SerializeField] ReproductionConfig reproductionConfig;
        [SerializeField] EnvironmentalCreatureBridge environmentalCreatures;
        [SerializeField] EnvironmentalEventManager environmentalEvents;
        [SerializeField] ResourceManager resourceManager;
        [SerializeField] DayNightManager dayNight;
        [SerializeField] bool spawnFoundersOnStart = true;

        public SimulationConfig Config => config;
        public ExtinctionState CurrentExtinction => ExtinctionEvaluator.Evaluate(populationTracker);
        public IReadOnlyDayNightState DayNight => dayNight;

        public IReadOnlyEnvironmentState CaptureEnvironmentState()
        {
            if (environmentalEvents != null)
            {
                return environmentalEvents.CaptureState(dayNight, resourceManager);
            }

            return new EnvironmentStateSnapshot(
                dayNight,
                resourceManager != null ? resourceManager.CaptureCensus() : default,
                System.Array.Empty<IReadOnlyEnvironmentalEvent>(),
                resourceManager != null ? resourceManager.TemperatureNormalized : 0f);
        }

        public bool SpawnFoundersOnStart
        {
            get => spawnFoundersOnStart;
            set => spawnFoundersOnStart = value;
        }

        public void Configure(
            SimulationConfig simulationConfig,
            SimulationClock simulationClock,
            CreatureSpawner creatureSpawner,
            PopulationTracker tracker,
            CreatureLifecycleHub hub,
            ReproductionSystem reproductionSystem,
            TrainingRespawnController respawn,
            GameObject herbivore,
            GameObject predator)
        {
            config = simulationConfig;
            clock = simulationClock;
            spawner = creatureSpawner;
            populationTracker = tracker;
            lifecycleHub = hub;
            reproduction = reproductionSystem;
            trainingRespawn = respawn;
            herbivorePrefab = herbivore;
            predatorPrefab = predator;
        }

        void Start()
        {
            ApplyExperimentSettings();
            if (spawnFoundersOnStart)
            {
                SpawnFounders();
            }
        }

        public void ApplyExperimentSettings()
        {
            if (config == null)
            {
                return;
            }

            clock?.SetTimeScale(config.DefaultTimeScale);
            var experiment = config.ToExperimentConfiguration();
            var seeds = experiment.Seeds;
            spawner?.SetSeed(seeds.FounderGenomes);
            spawner?.SetPolicyMasterSeed(experiment.RandomSeed);
            spawner?.SetPredatorSpeedBias(experiment.PredatorSpeedBias);
            spawner?.Configure(populationTracker, lifecycleHub, reproduction);
            var reproductionSettings = reproductionConfig != null
                ? reproductionConfig.Settings
                : reproduction != null ? reproduction.Settings : new ReproductionSettings();
            experiment.ApplyMutationTo(reproductionSettings);
            reproduction?.Configure(
                spawner,
                populationTracker,
                lifecycleHub,
                clock,
                reproductionSettings,
                config.Ecosystem,
                config,
                herbivorePrefab,
                predatorPrefab);
            reproduction?.SetSeed(seeds.Reproduction);
            trainingRespawn?.Configure(
                spawner,
                populationTracker,
                config,
                herbivorePrefab,
                predatorPrefab,
                seeds.TrainingRespawn);
            environmentalCreatures?.Configure(
                spawner,
                lifecycleHub,
                populationTracker,
                config,
                herbivorePrefab,
                predatorPrefab,
                transform,
                seeds.EnvironmentalCreatures);
            ExperimentEnvironmentApplicator.Apply(
                experiment,
                resourceManager,
                dayNight,
                environmentalEvents,
                eventConfig: null);
            environmentalEvents?.Bind(
                resourceManager,
                environmentalCreatures,
                environmentalCreatures,
                eventConfig: null,
                clock);
        }

        public void SpawnFounders()
        {
            if (config == null || spawner == null)
            {
                return;
            }

            InitialPopulationSpawner.SpawnFounders(
                spawner,
                config,
                herbivorePrefab,
                predatorPrefab,
                transform.position,
                new System.Random(config.RandomSeed));
        }
    }
}
