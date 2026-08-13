using System.Collections.Generic;
using EvoLife.Common;

namespace EvoLife.Simulation
{
    public enum ExperimentRunPhase : byte
    {
        Created = 0,
        Loaded = 1,
        EnvironmentInitialized = 2,
        PopulationInitialized = 3,
        AnalyticsStarted = 4,
        Running = 5,
        Stopping = 6,
        Finished = 7
    }

    /// <summary>
    /// Snapshot of an experiment run. Analytics metadata is recorded by
    /// <see cref="IExperimentAnalyticsSession"/>, not by this type.
    /// </summary>
    public sealed class ExperimentRunState
    {
        public ExperimentRunPhase Phase;
        public ExperimentConfiguration Configuration;
        public ExperimentStopReason StopReason;
        public float SimulationTimeSeconds;
        public bool ManualStopRequested;
        public string RunId;

        public bool IsRunning => Phase == ExperimentRunPhase.Running;

        public bool HasFinished => Phase == ExperimentRunPhase.Finished;
    }

    /// <summary>
    /// Thin experiment lifecycle state machine. Does not spawn creatures, tick plants,
    /// or upload stats — callers invoke existing owners for those steps.
    /// </summary>
    public sealed class ExperimentCoordinator
    {
        ExperimentConfiguration configuration;
        ExperimentRunState state;

        public ExperimentRunState State => state;

        public ExperimentConfiguration Configuration => configuration;

        public ExperimentCoordinator(ExperimentConfiguration experimentConfiguration = null)
        {
            if (experimentConfiguration != null)
            {
                Load(experimentConfiguration);
            }
        }

        public ExperimentRunState Load(ExperimentConfiguration experimentConfiguration)
        {
            ExperimentConfigurationValidator.ThrowIfInvalid(experimentConfiguration);
            configuration = experimentConfiguration.Clone();
            state = new ExperimentRunState
            {
                Phase = ExperimentRunPhase.Loaded,
                Configuration = configuration,
                StopReason = ExperimentStopReason.None
            };
            return state;
        }

        public void MarkEnvironmentInitialized()
        {
            RequirePhaseAtLeast(ExperimentRunPhase.Loaded);
            if (state.Phase < ExperimentRunPhase.EnvironmentInitialized)
            {
                state.Phase = ExperimentRunPhase.EnvironmentInitialized;
            }
        }

        public void MarkPopulationInitialized()
        {
            RequirePhaseAtLeast(ExperimentRunPhase.EnvironmentInitialized);
            if (state.Phase < ExperimentRunPhase.PopulationInitialized)
            {
                state.Phase = ExperimentRunPhase.PopulationInitialized;
            }
        }

        public void MarkAnalyticsStarted(string runId = null)
        {
            RequirePhaseAtLeast(ExperimentRunPhase.PopulationInitialized);
            if (!string.IsNullOrEmpty(runId))
            {
                state.RunId = runId;
            }

            state.Phase = ExperimentRunPhase.AnalyticsStarted;
        }

        public void BeginRunning()
        {
            RequirePhaseAtLeast(ExperimentRunPhase.AnalyticsStarted);
            state.Phase = ExperimentRunPhase.Running;
        }

        public void RequestManualStop()
        {
            if (state == null)
            {
                return;
            }

            state.ManualStopRequested = true;
        }

        public ExperimentStopReason Evaluate(float simulationTimeSeconds, IPopulationSnapshot population)
        {
            if (state == null || configuration == null)
            {
                return ExperimentStopReason.None;
            }

            state.SimulationTimeSeconds = simulationTimeSeconds;
            if (state.Phase != ExperimentRunPhase.Running)
            {
                return ExperimentStopReason.None;
            }

            var reason = ExperimentStopEvaluator.Evaluate(
                configuration.Stopping,
                simulationTimeSeconds,
                population,
                state.ManualStopRequested);
            if (reason != ExperimentStopReason.None)
            {
                state.StopReason = reason;
                state.Phase = ExperimentRunPhase.Stopping;
            }

            return reason;
        }

        public ExperimentRunState Finish(string runId = null)
        {
            if (state == null)
            {
                state = new ExperimentRunState { Phase = ExperimentRunPhase.Finished };
                return state;
            }

            if (runId != null)
            {
                state.RunId = runId;
            }

            state.Phase = ExperimentRunPhase.Finished;
            return state;
        }

        public IReadOnlyList<string> Validate() => ExperimentConfigurationValidator.Validate(configuration);

        void RequirePhaseAtLeast(ExperimentRunPhase minimum)
        {
            if (state == null || state.Phase < minimum)
            {
                throw new System.InvalidOperationException(
                    "ExperimentCoordinator phase " + (state != null ? state.Phase.ToString() : "none")
                    + " is before " + minimum + ".");
            }
        }
    }
}
