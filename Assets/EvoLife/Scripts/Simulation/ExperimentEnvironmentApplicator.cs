using System.Collections.Generic;
using UnityEngine;
using EvoLife.Common;
using EvoLife.Environment;

namespace EvoLife.Simulation
{
    /// <summary>
    /// Applies experiment environment knobs onto existing Environment types.
    /// Does not own plants, biomes, or event effect logic, and does not place resources.
    /// ExperimentOrchestrator is the experiment-time caller; EcosystemManager may call
    /// this only for standalone/demo scenes where the orchestrator is absent.
    /// </summary>
    public static class ExperimentEnvironmentApplicator
    {
        public const float BaselinePlantDensity = 0.04f;
        public const float BaselinePlantCapacity = 20f;
        public const float BaselineRegenPerSecond = 0.5f;

        public static void Apply(
            ExperimentConfiguration configuration,
            ResourceManager resources,
            DayNightManager dayNight,
            EnvironmentalEventManager events,
            EnvironmentalEventConfig eventConfig = null)
        {
            if (configuration == null)
            {
                return;
            }

            ApplyResources(configuration, resources);
            ApplyDayNight(configuration, dayNight);
            ApplyEvents(configuration, events, eventConfig);
        }

        public static void ApplyResources(ExperimentConfiguration configuration, ResourceManager resources)
        {
            if (configuration == null || resources == null)
            {
                return;
            }

            var settings = resources.SpawnSettings;
            settings.Seed = configuration.Seeds.ResourceSpawn;
            var abundance = Mathf.Max(0f, configuration.ResourceAbundance);
            var regen = Mathf.Max(0f, configuration.PlantRegenerationMultiplier);
            settings.DefaultDensity = BaselinePlantDensity * abundance;
            settings.DefaultCapacity = BaselinePlantCapacity;
            settings.DefaultRemaining = BaselinePlantCapacity * Mathf.Min(abundance, 1f);
            if (abundance > 1f)
            {
                settings.DefaultCapacity = BaselinePlantCapacity * abundance;
                settings.DefaultRemaining = settings.DefaultCapacity;
            }

            settings.DefaultRegenPerSecond = BaselineRegenPerSecond * regen;
            // Placement is owned by ResourceManager.EnsurePlaced / PlaceResources.
            // Applying knobs must not silently re-place an already-initialized world.
        }

        public static void ApplyDayNight(ExperimentConfiguration configuration, DayNightManager dayNight)
        {
            if (configuration == null || dayNight == null)
            {
                return;
            }

            dayNight.Configure(configuration.DayLengthSeconds);
        }

        public static void ApplyEvents(
            ExperimentConfiguration configuration,
            EnvironmentalEventManager events,
            EnvironmentalEventConfig eventConfig)
        {
            if (configuration == null)
            {
                return;
            }

            var built = eventConfig;
            if (built == null)
            {
                if (events == null)
                {
                    return;
                }

                built = ScriptableObject.CreateInstance<EnvironmentalEventConfig>();
            }

            built.Seed = configuration.Seeds.EventSchedule;
            built.SetDefinitions(BuildDefinitions(configuration));
            built.SetSchedule(BuildSchedule(configuration));
            events?.SetConfig(built);
        }

        public static IEnumerable<EnvironmentalEventDefinition> BuildDefinitions(ExperimentConfiguration configuration)
        {
            var enabled = configuration != null ? configuration.ResolveEnabledEvents() : new EnvironmentalEventKind[0];
            var definitions = new List<EnvironmentalEventDefinition>(enabled.Count);
            for (var i = 0; i < enabled.Count; i++)
            {
                definitions.Add(EnvironmentalEventDefinition.Defaults(enabled[i]));
            }

            return definitions;
        }

        public static IEnumerable<ScheduledEnvironmentalEvent> BuildSchedule(ExperimentConfiguration configuration)
        {
            var scheduled = configuration != null
                ? configuration.ScheduledEvents
                : System.Array.Empty<ExperimentScheduledEvent>();
            var entries = new List<ScheduledEnvironmentalEvent>(scheduled.Length);
            for (var i = 0; i < scheduled.Length; i++)
            {
                if (scheduled[i] == null || !scheduled[i].TryGetKind(out var kind))
                {
                    continue;
                }

                entries.Add(new ScheduledEnvironmentalEvent
                {
                    Kind = kind,
                    AtSimulationTime = scheduled[i].AtSimulationTime
                });
            }

            return entries;
        }
    }
}
