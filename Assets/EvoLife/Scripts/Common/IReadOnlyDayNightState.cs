namespace EvoLife.Common
{
    /// <summary>
    /// Simulation-time day/night cycle. Progression uses tick delta, not wall-clock time.
    /// </summary>
    public interface IReadOnlyDayNightState
    {
        /// <summary>Elapsed fraction of the current day in [0, 1).</summary>
        float NormalizedTimeOfDay { get; }

        float DayDurationSeconds { get; }

        bool IsDay { get; }

        bool IsNight { get; }

        DayNightPhase Phase { get; }
    }

    /// <summary>
    /// Optional lighting/presentation sink. Environment never requires a lighting implementation.
    /// </summary>
    public interface IDayNightLightingHook
    {
        void OnDayNightUpdated(IReadOnlyDayNightState state);
    }
}
