using System.Collections.Generic;

namespace EvoLife.Common
{
    /// <summary>
    /// Aggregated environment snapshot for Analytics and optional AI context.
    /// Does not include policy observations; PPO layout stays on CreatureObservationSchema.
    /// </summary>
    public interface IReadOnlyEnvironmentState
    {
        IReadOnlyDayNightState DayNight { get; }

        IReadOnlyResourceCensus Resources { get; }

        IReadOnlyList<IReadOnlyEnvironmentalEvent> ActiveEvents { get; }

        /// <summary>Normalized climate pressure in [0, 1]. Raised by heat events and dry biomes.</summary>
        float TemperatureNormalized { get; }
    }
}
