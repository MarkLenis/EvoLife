using System.Globalization;
using EvoLife.Common;

namespace EvoLife.UI
{
    public static class SimulationSpeedPresets
    {
        public const float One = 1f;
        public const float Two = 2f;
        public const float Five = 5f;
        public const float Ten = 10f;

        public static readonly float[] Values = { One, Two, Five, Ten };
    }

    public sealed class SimulationControlModel
    {
        public float SimulationTimeSeconds;
        public float TimeScale;
        public bool IsPaused;
        public string StatusLabel;
        public string ExperimentName;
        public string Scenario;
        public string RunState;
        public string RestartNote;
        public bool RestartRequiresSceneReload;
    }

    /// <summary>
    /// View-model for pause/speed. Applies only through <see cref="ISimulationClockControl"/>.
    /// </summary>
    public static class SimulationControlPresenter
    {
        public const string RestartRequiresReload =
            "Reload the scene to restart an experiment. ExperimentOrchestrator rejects a second BeginAsync.";

        public static SimulationControlModel Build(
            ISimulationClock clock,
            string experimentName,
            string scenario,
            string runState)
        {
            var paused = clock != null && clock.IsPaused;
            var scale = clock != null ? clock.TimeScale : 1f;
            var time = clock != null ? clock.SimulationTimeSeconds : 0f;
            return new SimulationControlModel
            {
                SimulationTimeSeconds = time,
                TimeScale = scale,
                IsPaused = paused,
                StatusLabel = paused ? "paused" : "running",
                ExperimentName = string.IsNullOrEmpty(experimentName) ? "none" : experimentName,
                Scenario = string.IsNullOrEmpty(scenario) ? "none" : scenario,
                RunState = string.IsNullOrEmpty(runState) ? "n/a" : runState,
                RestartNote = RestartRequiresReload,
                RestartRequiresSceneReload = true
            };
        }

        public static void Pause(ISimulationClockControl clock) => clock?.SetPaused(true);

        public static void Resume(ISimulationClockControl clock) => clock?.SetPaused(false);

        public static void SetSpeed(ISimulationClockControl clock, float scale)
        {
            if (clock == null)
            {
                return;
            }

            if (scale < 0f)
            {
                scale = 0f;
            }

            clock.SetTimeScale(scale);
        }

        public static string FormatTime(float seconds) =>
            seconds.ToString("0.0", CultureInfo.InvariantCulture) + "s";

        public static string FormatSpeed(float scale) =>
            scale.ToString("0.##", CultureInfo.InvariantCulture) + "x";
    }
}
