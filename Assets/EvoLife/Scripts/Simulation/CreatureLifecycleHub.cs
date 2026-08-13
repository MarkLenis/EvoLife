using System;
using System.Collections.Generic;
using UnityEngine;
using EvoLife.Common;

namespace EvoLife.Simulation
{
    /// <summary>
    /// Simulation-owned spawn/death fan-out. Unregisters population on death.
    /// Analytics may subscribe; this class does not upload or score fitness.
    /// </summary>
    public sealed class CreatureLifecycleHub : MonoBehaviour, ICreatureLifecycleEvents
    {
        [SerializeField] PopulationTracker populationTracker;

        readonly Dictionary<int, LiveCreature> live = new Dictionary<int, LiveCreature>();

        public event Action<IAnalyticsCreatureView> Spawned;
        public event Action<CreatureDeathNotice, IAnalyticsCreatureView> Died;

        public void Bind(PopulationTracker tracker) => populationTracker = tracker;

        public void RegisterSpawned(GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            var view = new CreatureObservationView(instance);
            var id = view.Identity != null ? view.Identity.Id.Value : instance.GetInstanceID();

            var deathSource = instance.GetComponent<ICreatureDeathObservable>();
            if (deathSource != null)
            {
                deathSource.DeathObserved += OnDeathObserved;
            }

            var relay = instance.GetComponent<CreatureLifecycleRelay>();
            if (relay == null)
            {
                relay = instance.AddComponent<CreatureLifecycleRelay>();
            }

            relay.Bind(this, id);
            live[id] = new LiveCreature(instance, view, relay, deathSource);
            Spawned?.Invoke(view);
        }

        /// <summary>
        /// Copies currently live instances. Callers must not mutate creature state through this list
        /// except via Creatures / spawn APIs.
        /// </summary>
        public void CopyLiveInstances(List<GameObject> destination)
        {
            if (destination == null)
            {
                return;
            }

            destination.Clear();
            foreach (var entry in live.Values)
            {
                if (entry.Instance != null)
                {
                    destination.Add(entry.Instance);
                }
            }
        }

        public void NotifyRemoved(int creatureIdValue)
        {
            Complete(creatureIdValue, null);
        }

        void OnDeathObserved(CreatureDeathNotice notice)
        {
            Complete(notice.Id.Value, notice);
        }

        void Complete(int creatureIdValue, CreatureDeathNotice? notice)
        {
            if (!live.TryGetValue(creatureIdValue, out var entry))
            {
                return;
            }

            live.Remove(creatureIdValue);
            entry.Relay?.MarkNotified();
            if (entry.DeathSource != null)
            {
                entry.DeathSource.DeathObserved -= OnDeathObserved;
            }

            populationTracker?.Unregister(new CreatureId(creatureIdValue));

            var resolved = notice ?? BuildNotice(creatureIdValue, entry.View);
            Died?.Invoke(resolved, entry.View);
        }

        static CreatureDeathNotice BuildNotice(int creatureIdValue, IAnalyticsCreatureView view)
        {
            var cause = view?.Vitals != null && view.Vitals.CauseOfDeath.HasValue
                ? view.Vitals.CauseOfDeath.Value
                : DeathCause.Unknown;
            var age = view?.Vitals != null ? view.Vitals.Age : 0f;
            var maxAge = view?.Vitals != null ? view.Vitals.MaxAge : 0f;
            return new CreatureDeathNotice(new CreatureId(creatureIdValue), cause, age, maxAge);
        }

        readonly struct LiveCreature
        {
            public LiveCreature(
                GameObject instance,
                CreatureObservationView view,
                CreatureLifecycleRelay relay,
                ICreatureDeathObservable deathSource)
            {
                Instance = instance;
                View = view;
                Relay = relay;
                DeathSource = deathSource;
            }

            public GameObject Instance { get; }
            public CreatureObservationView View { get; }
            public CreatureLifecycleRelay Relay { get; }
            public ICreatureDeathObservable DeathSource { get; }
        }
    }

    /// <summary>
    /// Relays GameObject destruction when biology did not publish a death event.
    /// </summary>
    public sealed class CreatureLifecycleRelay : MonoBehaviour
    {
        CreatureLifecycleHub hub;
        int creatureIdValue;
        bool notified;

        public void Bind(CreatureLifecycleHub owner, int id)
        {
            hub = owner;
            creatureIdValue = id;
            notified = false;
        }

        public void MarkNotified() => notified = true;

        void OnDestroy()
        {
            if (notified || hub == null)
            {
                return;
            }

            notified = true;
            hub.NotifyRemoved(creatureIdValue);
        }
    }
}
