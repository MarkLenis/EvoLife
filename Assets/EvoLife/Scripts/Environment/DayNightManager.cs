using UnityEngine;
using EvoLife.Common;

namespace EvoLife.Environment
{
    /// <summary>
    /// Environment-owned day/night driver. Advances from simulation ticks, never wall-clock time.
    /// Lighting hooks are optional.
    /// </summary>
    public sealed class DayNightManager : MonoBehaviour, ISimulationTickable, IReadOnlyDayNightState
    {
        [SerializeField] float dayDurationSeconds = 120f;
        [SerializeField] float nightStartNormalized = 0.5f;
        [SerializeField] MonoBehaviour[] lightingHooks;

        readonly DayNightCycle cycle = new DayNightCycle();
        bool configured;

        public DayNightCycle Cycle
        {
            get
            {
                EnsureConfigured();
                return cycle;
            }
        }

        public float NormalizedTimeOfDay => Cycle.NormalizedTimeOfDay;
        public float DayDurationSeconds => Cycle.DayDurationSeconds;
        public bool IsDay => Cycle.IsDay;
        public bool IsNight => Cycle.IsNight;
        public DayNightPhase Phase => Cycle.Phase;

        public void Configure(float durationSeconds, float nightStart = 0.5f)
        {
            dayDurationSeconds = durationSeconds;
            nightStartNormalized = nightStart;
            cycle.Configure(durationSeconds, nightStart);
            configured = true;
            NotifyLighting();
        }

        public void Tick(float deltaTimeSeconds)
        {
            EnsureConfigured();
            if (deltaTimeSeconds <= 0f)
            {
                return;
            }

            cycle.Tick(deltaTimeSeconds);
            NotifyLighting();
        }

        void EnsureConfigured()
        {
            if (configured)
            {
                return;
            }

            cycle.Configure(dayDurationSeconds, nightStartNormalized);
            configured = true;
        }

        void NotifyLighting()
        {
            if (lightingHooks == null)
            {
                return;
            }

            for (var i = 0; i < lightingHooks.Length; i++)
            {
                if (lightingHooks[i] is IDayNightLightingHook hook)
                {
                    hook.OnDayNightUpdated(cycle);
                }
            }
        }
    }
}
