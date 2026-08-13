using System.Collections.Generic;
using EvoLife.Common;

namespace EvoLife.Environment
{
    /// <summary>
    /// Immutable environment snapshot for Analytics and optional AI context.
    /// </summary>
    public readonly struct EnvironmentStateSnapshot : IReadOnlyEnvironmentState
    {
        public EnvironmentStateSnapshot(
            IReadOnlyDayNightState dayNight,
            IReadOnlyResourceCensus resources,
            IReadOnlyList<IReadOnlyEnvironmentalEvent> activeEvents,
            float temperatureNormalized)
        {
            DayNight = dayNight;
            Resources = resources;
            ActiveEvents = activeEvents ?? System.Array.Empty<IReadOnlyEnvironmentalEvent>();
            TemperatureNormalized = temperatureNormalized < 0f
                ? 0f
                : temperatureNormalized > 1f ? 1f : temperatureNormalized;
        }

        public IReadOnlyDayNightState DayNight { get; }
        public IReadOnlyResourceCensus Resources { get; }
        public IReadOnlyList<IReadOnlyEnvironmentalEvent> ActiveEvents { get; }
        public float TemperatureNormalized { get; }
    }
}
