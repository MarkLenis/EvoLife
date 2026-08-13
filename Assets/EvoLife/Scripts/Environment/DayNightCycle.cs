using EvoLife.Common;

namespace EvoLife.Environment
{
    /// <summary>
    /// Simulation-time day/night cycle. Advances only from tick deltas.
    /// </summary>
    public sealed class DayNightCycle : IReadOnlyDayNightState, ISimulationTickable
    {
        float dayDurationSeconds = 120f;
        float nightStartNormalized = 0.5f;
        float elapsedSeconds;

        public DayNightCycle(float dayDurationSeconds = 120f, float nightStartNormalized = 0.5f, float elapsedSeconds = 0f)
        {
            Configure(dayDurationSeconds, nightStartNormalized);
            this.elapsedSeconds = elapsedSeconds < 0f ? 0f : elapsedSeconds;
        }

        public float DayDurationSeconds => dayDurationSeconds;
        public float NightStartNormalized => nightStartNormalized;
        public float ElapsedSeconds => elapsedSeconds;

        public float NormalizedTimeOfDay
        {
            get
            {
                if (dayDurationSeconds <= 0f)
                {
                    return 0f;
                }

                var wrapped = elapsedSeconds % dayDurationSeconds;
                if (wrapped < 0f)
                {
                    wrapped += dayDurationSeconds;
                }

                return wrapped / dayDurationSeconds;
            }
        }

        public bool IsNight => NormalizedTimeOfDay >= nightStartNormalized;
        public bool IsDay => !IsNight;
        public DayNightPhase Phase => IsNight ? DayNightPhase.Night : DayNightPhase.Day;

        public void Configure(float durationSeconds, float nightStart)
        {
            dayDurationSeconds = durationSeconds < 0.0001f ? 0.0001f : durationSeconds;
            nightStartNormalized = nightStart < 0f ? 0f : nightStart > 1f ? 1f : nightStart;
        }

        public void Tick(float deltaTimeSeconds)
        {
            if (deltaTimeSeconds <= 0f)
            {
                return;
            }

            elapsedSeconds += deltaTimeSeconds;
        }
    }
}
