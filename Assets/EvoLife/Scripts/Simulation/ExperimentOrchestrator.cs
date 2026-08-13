using System.Threading.Tasks;
using UnityEngine;
using EvoLife.Common;
using EvoLife.Environment;

namespace EvoLife.Simulation
{
    /// <summary>
    /// Experiment run orchestrator. Loads a config, asks existing owners to initialize
    /// environment and population, starts/stops analytics, and ends on time, extinction,
    /// or a manual request. Intentionally small — do not fold spawn, genetics, or HTTP here.
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

        public ExperimentCoordinator Coordinator => coordinator;

        public ExperimentRunState State => coordinator.State;

        public ExperimentConfiguration Configuration => coordinator.Configuration;

        void Awake()
        {
            analytics = analyticsSessionBehaviour as IExperimentAnalyticsSession;
            if (autoStart)
            {
                if (ecosystem != null)
                {
                    ecosystem.SpawnFoundersOnStart = false;
                }

                if (analytics != null)
                {
                    analytics.SetAutoStart(false);
                }
            }
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

        public Task<ExperimentRunState> BeginAsync()
        {
            var configuration = ResolveConfiguration();
            return BeginAsync(configuration);
        }

        public async Task<ExperimentRunState> BeginAsync(ExperimentConfiguration configuration)
        {
            Load(configuration);
            InitializeEnvironment();
            InitializePopulation();
            await StartAnalyticsAsync();
            BeginRunning();
            return coordinator.State;
        }

        public ExperimentRunState Load(ExperimentConfiguration configuration)
        {
            ExperimentConfigurationValidator.ThrowIfInvalid(configuration);
            if (simulationConfig != null)
            {
                simulationConfig.ApplyExperiment(configuration);
            }

            return coordinator.Load(configuration);
        }

        public void InitializeEnvironment()
        {
            var configuration = RequireLoaded();
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
            coordinator.MarkEnvironmentInitialized();
        }

        public void InitializePopulation()
        {
            RequirePhase(ExperimentRunPhase.EnvironmentInitialized);
            if (spawnFounders)
            {
                ecosystem?.SpawnFounders();
            }

            coordinator.MarkPopulationInitialized();
        }

        public async Task StartAnalyticsAsync()
        {
            RequirePhase(ExperimentRunPhase.PopulationInitialized);
            if (analytics == null)
            {
                analytics = analyticsSessionBehaviour as IExperimentAnalyticsSession;
            }

            string runId = null;
            if (analytics != null)
            {
                await analytics.BeginAsync();
                runId = analytics.RunId;
            }

            coordinator.MarkAnalyticsStarted(runId);
        }

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

                clock?.SetPaused(true);
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
                throw new System.InvalidOperationException("Load an experiment configuration first.");
            }

            return coordinator.Configuration;
        }

        void RequirePhase(ExperimentRunPhase phase)
        {
            if (coordinator.State == null || coordinator.State.Phase < phase)
            {
                throw new System.InvalidOperationException("Expected experiment phase " + phase + ".");
            }
        }
    }
}
