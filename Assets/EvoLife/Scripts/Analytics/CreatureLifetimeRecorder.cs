using System.Collections.Generic;
using UnityEngine;
using EvoLife.Common;
using EvoLife.Simulation;

namespace EvoLife.Analytics
{
    /// <summary>
    /// Observes spawn/death events and produces creature lifetime records.
    /// Does not call into CreatureBiology or issue HTTP requests.
    /// </summary>
    public sealed class CreatureLifetimeRecorder : MonoBehaviour
    {
        [SerializeField] CreatureLifecycleHub lifecycleHub;
        [SerializeField] SimulationClock clock;

        readonly Dictionary<int, TrackedCreature> live = new Dictionary<int, TrackedCreature>();
        readonly List<CreatureLifetimeRecord> completed = new List<CreatureLifetimeRecord>();
        readonly List<IAnalyticsCreatureView> liveViews = new List<IAnalyticsCreatureView>();

        public IReadOnlyList<CreatureLifetimeRecord> Completed => completed;
        public IEnumerable<IAnalyticsCreatureView> LiveViews => liveViews;

        void OnEnable()
        {
            if (lifecycleHub == null)
            {
                lifecycleHub = FindFirstObjectByType<CreatureLifecycleHub>();
            }

            if (lifecycleHub != null)
            {
                lifecycleHub.Spawned += OnSpawned;
                lifecycleHub.Died += OnDied;
            }
        }

        void OnDisable()
        {
            if (lifecycleHub != null)
            {
                lifecycleHub.Spawned -= OnSpawned;
                lifecycleHub.Died -= OnDied;
            }
        }

        public void Bind(CreatureLifecycleHub hub, SimulationClock simulationClock)
        {
            if (lifecycleHub != null)
            {
                lifecycleHub.Spawned -= OnSpawned;
                lifecycleHub.Died -= OnDied;
            }

            lifecycleHub = hub;
            clock = simulationClock;
            if (isActiveAndEnabled && lifecycleHub != null)
            {
                lifecycleHub.Spawned += OnSpawned;
                lifecycleHub.Died += OnDied;
            }
        }

        public List<CreatureLifetimeRecord> DrainCompleted()
        {
            var copy = new List<CreatureLifetimeRecord>(completed);
            completed.Clear();
            return copy;
        }

        public List<CreatureTraitSample> CaptureTraitSamples()
        {
            var samples = new List<CreatureTraitSample>();
            foreach (var tracked in live.Values)
            {
                var lifespan = tracked.View != null && tracked.View.Vitals != null
                    ? tracked.View.Vitals.Age
                    : Now() - tracked.BirthTime;
                var sample = GenerationAggregator.FromView(tracked.View, lifespan);
                if (sample != null)
                {
                    samples.Add(sample);
                }
            }

            for (var i = 0; i < completed.Count; i++)
            {
                var sample = GenerationAggregator.FromLifetime(completed[i]);
                if (sample != null)
                {
                    samples.Add(sample);
                }
            }

            return samples;
        }

        void OnSpawned(IAnalyticsCreatureView view)
        {
            if (view?.Identity == null)
            {
                return;
            }

            var id = view.Identity.Id.Value;
            live[id] = new TrackedCreature
            {
                View = view,
                BirthTime = Now(),
                OffspringCount = 0
            };
            RebuildLiveViews();

            IncrementParent(view.Lineage != null ? view.Lineage.ParentA : null);
            IncrementParent(view.Lineage != null ? view.Lineage.ParentB : null);
        }

        void OnDied(CreatureDeathNotice notice, IAnalyticsCreatureView view)
        {
            var id = notice.Id.Value;
            live.TryGetValue(id, out var tracked);
            var birthTime = tracked != null ? tracked.BirthTime : Now();
            var offspring = tracked != null ? tracked.OffspringCount : 0;
            var record = CreatureLifetimeFactory.Create(view, notice, birthTime, Now(), offspring);
            completed.Add(record);
            live.Remove(id);
            RebuildLiveViews();
        }

        void IncrementParent(CreatureId? parentId)
        {
            if (!parentId.HasValue)
            {
                return;
            }

            var id = parentId.Value.Value;
            if (live.TryGetValue(id, out var tracked))
            {
                tracked.OffspringCount++;
                return;
            }

            for (var i = completed.Count - 1; i >= 0; i--)
            {
                if (completed[i].CreatureId == id.ToString())
                {
                    completed[i].OffspringCount++;
                    return;
                }
            }
        }

        void RebuildLiveViews()
        {
            liveViews.Clear();
            foreach (var tracked in live.Values)
            {
                if (tracked.View != null)
                {
                    liveViews.Add(tracked.View);
                }
            }
        }

        float Now() => clock != null ? clock.SimulationTimeSeconds : 0f;

        sealed class TrackedCreature
        {
            public IAnalyticsCreatureView View;
            public float BirthTime;
            public int OffspringCount;
        }
    }
}
