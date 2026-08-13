using UnityEngine;
using EvoLife.Common;
using EvoLife.Environment;

namespace EvoLife.Simulation
{
    /// <summary>
    /// Thin experiment coordinator for founders, extinction reporting, and wiring.
    /// Reproduction, respawn, and population counts stay in their dedicated types.
    /// Environment experiment knobs are applied by <see cref="ExperimentOrchestrator"/>
    /// during orchestrated runs, or by <see cref="ApplyStandaloneEnvironment"/> in demo scenes.
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
        [SerializeField] bool applyEnvironmentOnStart = true;

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

        public bool ApplyEnvironmentOnStart
        {
            get => applyEnvironmentOnStart;
            set => applyEnvironmentOnStart = value;
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
            GameObject predator,
            ResourceManager resources = null,
            DayNightManager dayNightManager = null,
            EnvironmentalEventManager events = null,
            EnvironmentalCreatureBridge creatureBridge = null)
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
            if (resources != null)
            {
                resourceManager = resources;
            }

            if (dayNightManager != null)
            {
                dayNight = dayNightManager;
            }

            if (events != null)
            {
                environmentalEvents = events;
            }

            if (creatureBridge != null)
            {
                environmentalCreatures = creatureBridge;
            }
        }

        void Awake()
        {
            if (applyEnvironmentOnStart && resourceManager != null)
            {
                resourceManager.PlaceOnStart = false;
            }
        }

        void Start()
        {
            ApplyExperimentSettings();
            if (applyEnvironmentOnStart)
            {
                ApplyStandaloneEnvironment();
            }

            if (spawnFoundersOnStart)
            {
                SpawnFounders();
            }
        }

        /// <summary>
        /// Wires population, reproduction, spawner, and event-bridge bindings.
        /// Does not apply ResourceManager / day-night / event experiment configuration.
        /// </summary>
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
            environmentalEvents?.Bind(
                resourceManager,
                environmentalCreatures,
                environmentalCreatures,
                eventConfig: null,
                clock);
        }

        /// <summary>
        /// Standalone/demo path when <see cref="ExperimentOrchestrator"/> is absent.
        /// Orchestrated experiments disable <see cref="ApplyEnvironmentOnStart"/> and call
        /// <see cref="ExperimentEnvironmentApplicator"/> themselves.
        /// </summary>
        public void ApplyStandaloneEnvironment()
        {
            if (config == null)
            {
                return;
            }

            ExperimentEnvironmentApplicator.Apply(
                config.ToExperimentConfiguration(),
                resourceManager,
                dayNight,
                environmentalEvents,
                eventConfig: null);
            resourceManager?.EnsurePlaced();
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
