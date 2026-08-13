using UnityEngine;
using EvoLife.Common;
using EvoLife.Creatures;
using EvoLife.Environment;
using EvoLife.Simulation;

namespace EvoLife.Presentation
{
    /// <summary>
    /// Wires the presentation demo scene: creature/resource templates, biome layout,
    /// lighting hook, and simulation tickables. Does not create a camera controller or HUD.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class PresentationDemoBootstrap : MonoBehaviour
    {
        [SerializeField] SimulationClock clock;
        [SerializeField] SimulationRunner runner;
        [SerializeField] PopulationTracker population;
        [SerializeField] CreatureLifecycleHub lifecycleHub;
        [SerializeField] CreatureSpawner spawner;
        [SerializeField] ReproductionSystem reproduction;
        [SerializeField] EcosystemManager ecosystem;
        [SerializeField] EnvironmentalCreatureBridge environmentalCreatures;
        [SerializeField] TrainingRespawnController trainingRespawn;
        [SerializeField] ResourceRegistry registry;
        [SerializeField] ResourceManager resourceManager;
        [SerializeField] DayNightManager dayNight;
        [SerializeField] EnvironmentalEventManager events;
        [SerializeField] PresentationWorldBuilder worldBuilder;
        [SerializeField] DayNightLightingPresenter lighting;
        [SerializeField] EnvironmentalEventVisualAdapter eventVisuals;
        [SerializeField] BiomeGroundPresenter ground;
        [SerializeField] Light sun;
        [SerializeField] Transform worldRoot;
        [SerializeField] int herbivores = 24;
        [SerializeField] int predators = 6;
        [SerializeField] GameObject herbivorePrefabOverride;
        [SerializeField] GameObject predatorPrefabOverride;
        [SerializeField] GameObject plantPrefabOverride;
        [SerializeField] GameObject waterPrefabOverride;

        bool wired;

        void Awake() => Wire();

        public void Wire()
        {
            if (wired)
            {
                return;
            }

            ResolveReferences();
            var templates = EnsureTemplates();
            var config = ScriptableObject.CreateInstance<SimulationConfig>();
            config.SetInitialPopulation(Mathf.Max(0, herbivores), Mathf.Max(0, predators));
            config.Ecosystem.Mode = EcosystemMode.Persistent;
            config.Ecosystem.TrainingRespawnEnabled = false;
            config.Ecosystem.FounderSpawnRadius = 16f;
            config.Ecosystem.MaxHerbivores = 80;
            config.Ecosystem.MaxPredators = 24;

            var vitals = ScriptableObject.CreateInstance<SpeciesVitalsDefinition>();
            if (resourceManager != null)
            {
                resourceManager.Configure(
                    registry != null ? registry : resourceManager.Registry,
                    DemoBiomeLayout.CreateSpawnSettings(config.RandomSeed),
                    DemoBiomeLayout.CreateZones(),
                    DemoBiomeLayout.WaterSourceCount);
                resourceManager.SetPresentationPrefabs(templates.Plant, templates.Water);
                resourceManager.PlaceOnStart = false;
            }

            if (spawner != null)
            {
                spawner.Configure(population, lifecycleHub, reproduction, vitals);
            }

            if (ecosystem != null)
            {
                ecosystem.Configure(
                    config,
                    clock,
                    spawner,
                    population,
                    lifecycleHub,
                    reproduction,
                    trainingRespawn,
                    templates.Herbivore,
                    templates.Predator,
                    resourceManager,
                    dayNight,
                    events,
                    environmentalCreatures);
                ecosystem.ApplyEnvironmentOnStart = true;
                ecosystem.SpawnFoundersOnStart = true;
            }

            if (runner != null)
            {
                if (resourceManager != null)
                {
                    runner.RegisterTickable(resourceManager);
                }

                if (dayNight != null)
                {
                    runner.RegisterTickable(dayNight);
                }

                if (events != null)
                {
                    runner.RegisterTickable(events);
                }

                if (trainingRespawn != null)
                {
                    runner.RegisterTickable(trainingRespawn);
                }
            }

            if (lighting != null)
            {
                lighting.BindSun(sun);
                dayNight?.BindLightingHook(lighting);
            }

            if (worldBuilder != null)
            {
                worldBuilder.Bind(resourceManager, worldRoot, ground, eventVisuals);
            }

            if (eventVisuals != null)
            {
                eventVisuals.Bind(events, ground);
            }

            wired = true;
        }

        void ResolveReferences()
        {
            clock = clock != null ? clock : GetComponent<SimulationClock>() ?? FindObjectOfType<SimulationClock>();
            runner = runner != null ? runner : GetComponent<SimulationRunner>() ?? FindObjectOfType<SimulationRunner>();
            population = population != null ? population : GetComponent<PopulationTracker>() ?? FindObjectOfType<PopulationTracker>();
            lifecycleHub = lifecycleHub != null ? lifecycleHub : GetComponent<CreatureLifecycleHub>() ?? FindObjectOfType<CreatureLifecycleHub>();
            spawner = spawner != null ? spawner : GetComponent<CreatureSpawner>() ?? FindObjectOfType<CreatureSpawner>();
            reproduction = reproduction != null ? reproduction : GetComponent<ReproductionSystem>() ?? FindObjectOfType<ReproductionSystem>();
            ecosystem = ecosystem != null ? ecosystem : GetComponent<EcosystemManager>() ?? FindObjectOfType<EcosystemManager>();
            environmentalCreatures = environmentalCreatures != null ? environmentalCreatures : GetComponent<EnvironmentalCreatureBridge>() ?? FindObjectOfType<EnvironmentalCreatureBridge>();
            trainingRespawn = trainingRespawn != null ? trainingRespawn : GetComponent<TrainingRespawnController>() ?? FindObjectOfType<TrainingRespawnController>();
            registry = registry != null ? registry : FindObjectOfType<ResourceRegistry>();
            resourceManager = resourceManager != null ? resourceManager : FindObjectOfType<ResourceManager>();
            dayNight = dayNight != null ? dayNight : FindObjectOfType<DayNightManager>();
            events = events != null ? events : FindObjectOfType<EnvironmentalEventManager>();
            worldBuilder = worldBuilder != null ? worldBuilder : GetComponent<PresentationWorldBuilder>() ?? FindObjectOfType<PresentationWorldBuilder>();
            lighting = lighting != null ? lighting : GetComponent<DayNightLightingPresenter>() ?? FindObjectOfType<DayNightLightingPresenter>();
            eventVisuals = eventVisuals != null ? eventVisuals : GetComponent<EnvironmentalEventVisualAdapter>() ?? FindObjectOfType<EnvironmentalEventVisualAdapter>();
            ground = ground != null ? ground : GetComponent<BiomeGroundPresenter>() ?? FindObjectOfType<BiomeGroundPresenter>();
            if (sun == null)
            {
                sun = FindObjectOfType<Light>();
            }

            if (worldRoot == null)
            {
                var found = GameObject.Find("WorldPresentation");
                worldRoot = found != null ? found.transform : transform;
            }
        }

        Templates EnsureTemplates()
        {
            var holder = new GameObject("PresentationTemplates");
            holder.transform.SetParent(transform, false);
            holder.SetActive(false);

            var herbivore = herbivorePrefabOverride != null
                ? herbivorePrefabOverride
                : CreaturePresentationFactory.CreateTemplate(CreatureRole.Herbivore, holder.transform);
            var predator = predatorPrefabOverride != null
                ? predatorPrefabOverride
                : CreaturePresentationFactory.CreateTemplate(CreatureRole.Predator, holder.transform);
            var plant = plantPrefabOverride != null
                ? plantPrefabOverride
                : ResourcePresentationFactory.CreatePlantTemplate(holder.transform);
            var water = waterPrefabOverride != null
                ? waterPrefabOverride
                : ResourcePresentationFactory.CreateWaterTemplate(holder.transform);

            herbivore.name = "HerbivoreTemplate";
            predator.name = "PredatorTemplate";
            return new Templates(herbivore, predator, plant, water);
        }

        readonly struct Templates
        {
            public Templates(GameObject herbivore, GameObject predator, GameObject plant, GameObject water)
            {
                Herbivore = herbivore;
                Predator = predator;
                Plant = plant;
                Water = water;
            }

            public GameObject Herbivore { get; }
            public GameObject Predator { get; }
            public GameObject Plant { get; }
            public GameObject Water { get; }
        }
    }
}
