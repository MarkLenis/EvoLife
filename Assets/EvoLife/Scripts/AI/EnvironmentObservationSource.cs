using EvoLife.Common;

namespace EvoLife.AI
{
    /// <summary>
    /// Optional contextual environment scalars for experiments.
    /// This source is NOT part of CreatureObservationSchema v2 (size 31).
    /// Compose it deliberately before any PPO layout change; do not append it in
    /// CompositeObservationSource without bumping the schema version and Training YAML.
    /// </summary>
    public sealed class EnvironmentObservationSource : IObservationSource
    {
        public const int Size = 2;
        public const int IndexTimeOfDay = 0;
        public const int IndexTemperature = 1;

        readonly IReadOnlyDayNightState dayNight;
        readonly IReadOnlyEnvironmentState environment;

        public EnvironmentObservationSource(
            IReadOnlyDayNightState dayNight = null,
            IReadOnlyEnvironmentState environment = null)
        {
            this.dayNight = dayNight;
            this.environment = environment;
        }

        public int ObservationSize => Size;

        public void WriteObservations(float[] buffer)
        {
            if (buffer == null || buffer.Length < Size)
            {
                return;
            }

            var time = dayNight != null
                ? dayNight.NormalizedTimeOfDay
                : environment?.DayNight != null ? environment.DayNight.NormalizedTimeOfDay : 0f;
            var temperature = environment != null ? environment.TemperatureNormalized : 0f;

            buffer[IndexTimeOfDay] = Clamp01(time);
            buffer[IndexTemperature] = Clamp01(temperature);
        }

        static float Clamp01(float value) =>
            value < 0f ? 0f : value > 1f ? 1f : value;
    }
}
