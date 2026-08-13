using System;
using System.Threading.Tasks;
using UnityEngine;
using EvoLife.Common;
using EvoLife.Environment;

namespace EvoLife.Simulation
{
    /// <summary>
    /// Experiment run orchestrator. Loads a config, asks existing owners to initialize
    /// environment and population, starts/stops analytics, and ends on time, extinction,
    /// or a manual request. Owns pause/unpause for the experiment lifecycle:
    /// the clock is paused from initialization through analytics startup, and only
    /// <see cref="BeginRunning"/> unpauses it.
    /// Intentionally small — do not fold spawn, genetics, or HTTP here.
    /// </summary>
    public sealed class ExperimentOrchestrator : MonoBehaviour
    {
        [SerializeField] SimulationConfig simulationConfig;
        [SerializeField] ExperimentConfigurationAsset experimentAsset;
        [SerializeField] SimulationClock clock;
        [SerializeField] EcosystemManager ecosystem;
        [SerializeField] ResourceManager resourceManager;
        [SerializeField] DayNightManager dayNight;
        [SerializeField] EnvironmentalEventManager environmentalEvents;
        [SerializeField] EnvironmentalEventConfig eventConfig;
        [SerializeField] ReproductionSystem reproduction;
        [SerializeField] PopulationTracker populationTracker;
        [SerializeField] MonoBehaviour analyticsSessionBehaviour;
        [SerializeField] bool autoStart = true;
        [SerializeField] bool spawnFounders = true;

        readonly ExperimentCoordinator coordinator = new ExperimentCoordinator();
        IExperimentAnalyticsSession analytics;
        bool finishInFlight;
        bool beginAsyncInvoked;
        bool environmentApplied;
        bool populationInitialized;

        public ExperimentCoordinator Coordinator => coordinator;

        public ExperimentRunState State => coordinator.State;

        public ExperimentConfiguration Configuration => coordinator.Configuration;

        public bool HasBeginBeenInvoked => beginAsyncInvoked;

        void Awake()
        {
            analytics = analyticsSessionBehaviour as IExperimentAnalyticsSession;
            PrepareOrchestratedScene();
        }

        void Start()
        {
            if (autoStart)
            {
                _ = BeginAsync();
            }
        }

        void Update()
        {
            var state = coordinator.State;
            if (state == null || !state.IsRunning || clock == null)
            {
                return;
            }

            var reason = coordinator.Evaluate(clock.SimulationTimeSeconds, populationTracker);
            if (reason != ExperimentStopReason.None)
            {
                _ = FinishAsync(reason);
            }
        }

        public void Configure(
            SimulationConfig config,
            SimulationClock simulationClock,
            EcosystemManager ecosystemManager = null,
            ResourceManager resources = null,
            DayNightManager dayNightManager = null,
            EnvironmentalEventManager events = null,
            EnvironmentalEventConfig eventsConfig = null,
            ReproductionSystem reproductionSystem = null,
            PopulationTracker tracker = null,
            IExperimentAnalyticsSession session = null,
            bool startAutomatically = false,
            bool spawnFounderPopulation = true)
        {
            simulationConfig = config;
            clock = simulationClock;
            ecosystem = ecosystemManager;
            resourceManager = resources;
            dayNight = dayNightManager;
            environmentalEvents = events;
            eventConfig = eventsConfig;
            reproduction = reproductionSystem;
            populationTracker = tracker;
            analytics = session;
            autoStart = startAutomatically;
            spawnFounders = spawnFounderPopulation;
            PrepareOrchestratedScene();
        }

        public Task<ExperimentRunState> BeginAsync()
        {
            var configuration = ResolveConfiguration();
            return BeginAsync(configuration);
        }

        public async Task<ExperimentRunState> BeginAsync(ExperimentConfiguration configuration)
        {
            if (beginAsyncInvoked || coordinator.RejectsSecondBegin)
            {
                Debug.LogError(
                    "ExperimentOrchestrator.BeginAsync rejected: an experiment has already been started in this scene. Reload the scene before another run.");
                return coordinator.State;
            }

            beginAsyncInvoked = true;
            PauseSimulation();
            Load(configuration);
            InitializeEnvironment();
            InitializePopulation();
            var analyticsStarted = await StartAnalyticsAsync();
            if (!analyticsStarted)
            {
                PauseSimulation();
                Debug.LogError(
                    "ExperimentOrchestrator: analytics startup failed; simulation remains paused and will not enter Running.");
                return coordinator.State;
            }

            BeginRunning();
            return coordinator.State;
        }

        public ExperimentRunState Load(ExperimentConfiguration configuration)
        {
            PauseSimulation();
            if (coordinator.State != null)
            {
                switch (coordinator.State.Phase)
                {
                    case ExperimentRunPhase.Running:
                    case ExperimentRunPhase.Stopping:
                        Debug.LogError(
                            "ExperimentOrchestrator.Load rejected: an experiment is already in progress. Reload the scene before another run.");
                        return coordinator.State;
                    case ExperimentRunPhase.Created:
                    case ExperimentRunPhase.Loaded:
                    case ExperimentRunPhase.EnvironmentInitialized:
                    case ExperimentRunPhase.PopulationInitialized:
                    case ExperimentRunPhase.AnalyticsStarted:
                    case ExperimentRunPhase.Finished:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(
                            nameof(coordinator.State.Phase),
                            coordinator.State.Phase,
                            "Unhandled ExperimentRunPhase.");
                }
            }

            ExperimentConfigurationValidator.ThrowIfInvalid(configuration);
            if (simulationConfig != null)
            {
                simulationConfig.ApplyExperiment(configuration);
            }

            return coordinator.Load(configuration);
        }

        public void InitializeEnvironment()
        {
            PauseSimulation();
            var configuration = RequireLoaded();
            if (!environmentApplied)
            {
                if (reproduction != null)
                {
                    configuration.ApplyMutationTo(reproduction.Settings);
                    reproduction.SetSettings(reproduction.Settings);
                }

                ExperimentEnvironmentApplicator.Apply(
                    configuration,
                    resourceManager,
                    dayNight,
                    environmentalEvents,
                    eventConfig);
                ecosystem?.ApplyExperimentSettings();
                resourceManager?.EnsurePlaced();
                environmentApplied = true;
            }

            coordinator.MarkEnvironmentInitialized();
        }

        public void InitializePopulation()
        {
            PauseSimulation();
            RequirePhase(ExperimentRunPhase.EnvironmentInitialized);
            if (spawnFounders && !populationInitialized)
            {
                ecosystem?.SpawnFounders();
                populationInitialized = true;
            }

            coordinator.MarkPopulationInitialized();
        }

        public async Task<bool> StartAnalyticsAsync()
        {
            PauseSimulation();
            RequirePhase(ExperimentRunPhase.PopulationInitialized);
            if (analytics == null)
            {
                analytics = analyticsSessionBehaviour as IExperimentAnalyticsSession;
            }

            if (analytics == null)
            {
                coordinator.MarkAnalyticsStarted(null);
                return true;
            }

            try
            {
                var started = await analytics.BeginAsync();
                if (!started)
                {
                    Debug.LogError("ExperimentOrchestrator: IExperimentAnalyticsSession.BeginAsync returned false.");
                    return false;
                }

                coordinator.MarkAnalyticsStarted(analytics.RunId);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError("ExperimentOrchestrator: analytics startup failed: " + exception.Message);
                return false;
            }
        }

        /// <summary>
        /// Transitions to Running and is the only method that unpauses <see cref="SimulationClock"/>.
        /// </summary>
        public void BeginRunning()
        {
            coordinator.BeginRunning();
            clock?.SetPaused(false);
        }

        public void RequestManualStop()
        {
            coordinator.RequestManualStop();
            if (coordinator.State != null && coordinator.State.IsRunning && clock != null)
            {
                var reason = coordinator.Evaluate(clock.SimulationTimeSeconds, populationTracker);
                if (reason != ExperimentStopReason.None)
                {
                    _ = FinishAsync(reason);
                }
            }
        }

        public async Task<ExperimentRunState> FinishAsync(ExperimentStopReason reason)
        {
            if (finishInFlight)
            {
                return coordinator.State;
            }

            finishInFlight = true;
            try
            {
                if (coordinator.State != null)
                {
                    coordinator.State.StopReason = reason;
                    coordinator.State.Phase = ExperimentRunPhase.Stopping;
                }

                PauseSimulation();
                var status = reason == ExperimentStopReason.ManualStop ? "cancelled" : "completed";
                if (analytics != null)
                {
                    await analytics.FinishAsync(status, ExperimentStopReasonNames.ToWireName(reason));
                }

                return coordinator.Finish(analytics != null ? analytics.RunId : coordinator.State?.RunId);
            }
            finally
            {
                finishInFlight = false;
            }
        }

        void PrepareOrchestratedScene()
        {
            PauseSimulation();
            if (ecosystem != null)
            {
                ecosystem.SpawnFoundersOnStart = false;
                ecosystem.ApplyEnvironmentOnStart = false;
            }

            if (resourceManager != null)
            {
                resourceManager.PlaceOnStart = false;
            }

            if (analytics == null)
            {
                analytics = analyticsSessionBehaviour as IExperimentAnalyticsSession;
            }

            analytics?.SetAutoStart(false);
        }

        void PauseSimulation() => clock?.SetPaused(true);

        ExperimentConfiguration ResolveConfiguration()
        {
            if (experimentAsset != null)
            {
                return experimentAsset.Configuration.Clone();
            }

            if (simulationConfig != null)
            {
                return simulationConfig.ToExperimentConfiguration();
            }

            return ExperimentConfiguration.CreateDefault();
        }

        ExperimentConfiguration RequireLoaded()
        {
            if (coordinator.Configuration == null)
            {
                throw new InvalidOperationException("Load an experiment configuration first.");
            }

            return coordinator.Configuration;
        }

        void RequirePhase(ExperimentRunPhase phase)
        {
            if (coordinator.State == null || coordinator.State.Phase < phase)
            {
                throw new InvalidOperationException("Expected experiment phase " + phase + ".");
            }
        }
    }
}
