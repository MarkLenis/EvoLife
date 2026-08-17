using System;
using System.Collections.Generic;
using UnityEngine;
using EvoLife.Common;

namespace EvoLife.Environment
{
    /// <summary>
    /// Configurable ecological events. Mutates resources through Environment APIs and
    /// creatures only through injected Simulation ports — never hidden biology state.
    /// </summary>
    public sealed class EnvironmentalEventManager : MonoBehaviour, ISimulationTickable, IEnvironmentalEventCommands
    {
        [SerializeField] EnvironmentalEventConfig config;
        [SerializeField] ResourceManager resourceManager;
        [SerializeField] MonoBehaviour vitalEffectsBehaviour;
        [SerializeField] MonoBehaviour populationCommandsBehaviour;

        readonly List<ActiveEnvironmentalEvent> active = new List<ActiveEnvironmentalEvent>();
        readonly List<EnvironmentalEventKind> dueBuffer = new List<EnvironmentalEventKind>(8);

        IEnvironmentEffectHost environment;
        IEnvironmentalVitalEffects vitals;
        IEnvironmentalPopulationCommands population;
        ISimulationClock clock;
        EnvironmentalEventScheduler scheduler;
        float simulationTime;
        int nextEventId = 1;
        bool started;

        public event Action<IReadOnlyEnvironmentalEvent> EventStarted;
        public event Action<IReadOnlyEnvironmentalEvent> EventEnded;

        public float SimulationTimeSeconds => clock != null ? clock.SimulationTimeSeconds : simulationTime;

        public IReadOnlyList<IReadOnlyEnvironmentalEvent> ActiveEvents
        {
            get
            {
                var copy = new List<IReadOnlyEnvironmentalEvent>(active.Count);
                for (var i = 0; i < active.Count; i++)
                {
                    if (active[i] != null && active[i].IsActive)
                    {
                        copy.Add(active[i]);
                    }
                }

                return copy;
            }
        }

        public bool HasActiveEvent(EnvironmentalEventKind kind)
        {
            for (var i = 0; i < active.Count; i++)
            {
                if (active[i] != null && active[i].IsActive && active[i].Kind == kind)
                {
                    return true;
                }
            }

            return false;
        }

        public void Bind(
            IEnvironmentEffectHost effectHost,
            IEnvironmentalVitalEffects vitalEffects,
            IEnvironmentalPopulationCommands populationCommands,
            EnvironmentalEventConfig eventConfig = null,
            ISimulationClock simulationClock = null)
        {
            environment = effectHost;
            vitals = vitalEffects;
            population = populationCommands;
            clock = simulationClock;
            if (eventConfig != null)
            {
                config = eventConfig;
            }

            if (effectHost is ResourceManager manager)
            {
                resourceManager = manager;
            }

            RebuildScheduler();
        }

        public void SetConfig(EnvironmentalEventConfig eventConfig)
        {
            config = eventConfig;
            RebuildScheduler();
        }

        void Awake()
        {
            if (environment == null)
            {
                environment = resourceManager != null
                    ? resourceManager
                    : GetComponent<ResourceManager>();
            }

            if (vitals == null)
            {
                vitals = vitalEffectsBehaviour as IEnvironmentalVitalEffects;
            }

            if (population == null)
            {
                population = populationCommandsBehaviour as IEnvironmentalPopulationCommands;
            }
        }

        public void Tick(float deltaTimeSeconds)
        {
            EnsureStarted();
            var previous = SimulationTimeSeconds;
            if (clock == null && deltaTimeSeconds > 0f)
            {
                simulationTime += deltaTimeSeconds;
            }

            var now = SimulationTimeSeconds;
            FireDue(previous, now);
            TickActive(deltaTimeSeconds, now);
        }

        void IEnvironmentalEventCommands.Trigger(EnvironmentalEventKind kind)
        {
            Trigger(kind);
        }

        public ActiveEnvironmentalEvent Trigger(EnvironmentalEventKind kind)
        {
            var definition = config != null
                ? config.Resolve(kind)
                : EnvironmentalEventDefinition.Defaults(kind);
            return Trigger(definition);
        }

        public ActiveEnvironmentalEvent Trigger(EnvironmentalEventDefinition definition)
        {
            EnsureStarted();
            if (definition == null)
            {
                return null;
            }

            var occurrence = new ActiveEnvironmentalEvent(nextEventId++, definition.Clone(), SimulationTimeSeconds);
            ApplyStart(occurrence);
            EventStarted?.Invoke(occurrence);

            if (occurrence.Definition.DurationSeconds <= 0f)
            {
                ApplyEnd(occurrence);
                EventEnded?.Invoke(occurrence);
                return occurrence;
            }

            active.Add(occurrence);
            return occurrence;
        }

        public EnvironmentStateSnapshot CaptureState(IReadOnlyDayNightState dayNight, IReadOnlyResourceCensus census)
        {
            var temperature = environment != null ? environment.TemperatureNormalized : 0f;
            IReadOnlyResourceCensus resources = census;
            if (resources == null && environment != null)
            {
                resources = environment.CaptureCensus();
            }

            var copy = new List<IReadOnlyEnvironmentalEvent>(active.Count);
            for (var i = 0; i < active.Count; i++)
            {
                if (active[i] != null && active[i].IsActive)
                {
                    copy.Add(active[i]);
                }
            }

            return new EnvironmentStateSnapshot(dayNight, resources, copy, temperature);
        }

        void EnsureStarted()
        {
            if (started)
            {
                return;
            }

            if (environment == null)
            {
                environment = resourceManager as IEnvironmentEffectHost;
            }

            RebuildScheduler();
            started = true;
        }

        void RebuildScheduler()
        {
            IReadOnlyList<ScheduledEnvironmentalEvent> entries = config != null
                ? config.Schedule
                : Array.Empty<ScheduledEnvironmentalEvent>();
            scheduler = new EnvironmentalEventScheduler(entries);
        }

        void FireDue(float previousTime, float currentTime)
        {
            if (scheduler == null)
            {
                return;
            }

            dueBuffer.Clear();
            scheduler.CollectDue(previousTime, currentTime, dueBuffer);
            for (var i = 0; i < dueBuffer.Count; i++)
            {
                Trigger(dueBuffer[i]);
            }
        }

        void TickActive(float deltaTimeSeconds, float now)
        {
            for (var i = active.Count - 1; i >= 0; i--)
            {
                var occurrence = active[i];
                if (occurrence == null || !occurrence.IsActive)
                {
                    active.RemoveAt(i);
                    continue;
                }

                if (now <= occurrence.StartedAtSimulationTime)
                {
                    if (now >= occurrence.EndsAtSimulationTime)
                    {
                        ApplyEnd(occurrence);
                        EventEnded?.Invoke(occurrence);
                        active.RemoveAt(i);
                    }

                    continue;
                }

                ApplyTick(occurrence, deltaTimeSeconds);
                if (now >= occurrence.EndsAtSimulationTime)
                {
                    ApplyEnd(occurrence);
                    EventEnded?.Invoke(occurrence);
                    active.RemoveAt(i);
                }
            }
        }

        void ApplyStart(ActiveEnvironmentalEvent occurrence)
        {
            var definition = occurrence.Definition;
            environment?.PushEventModifiers(
                occurrence.EventId,
                definition.PlantRegenMultiplier,
                definition.TemperatureDelta,
                definition.WaterRechargeMultiplier,
                definition.AffectedBiomes);

            if (definition.PlantAvailabilityBoost > 0f)
            {
                environment?.BoostPlantAvailability(definition.PlantAvailabilityBoost, definition.AffectedBiomes);
            }

            if (definition.PlantDepletionFraction > 0f)
            {
                environment?.DepletePlants(definition.PlantDepletionFraction, definition.AffectedBiomes);
            }

            if (definition.DamagePulse > 0f)
            {
                vitals?.ApplyEnvironmentalDamage(definition.DamagePulse, DeathCause.Environmental);
            }

            if (definition.PredatorSpawnCount > 0)
            {
                population?.SpawnRole(CreatureRole.Predator, definition.PredatorSpawnCount);
            }

            if (definition.PredatorRemoveCount > 0)
            {
                population?.RemoveRole(CreatureRole.Predator, definition.PredatorRemoveCount);
            }
        }

        void ApplyTick(ActiveEnvironmentalEvent occurrence, float deltaTimeSeconds)
        {
            if (deltaTimeSeconds <= 0f)
            {
                return;
            }

            var dps = occurrence.Definition.DamagePerSecond;
            if (dps > 0f)
            {
                vitals?.ApplyEnvironmentalDamage(dps * deltaTimeSeconds, DeathCause.Environmental);
            }
        }

        void ApplyEnd(ActiveEnvironmentalEvent occurrence)
        {
            environment?.RemoveEventModifiers(occurrence.EventId);
            occurrence.MarkEnded();
        }
    }
}
