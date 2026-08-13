using System;
using System.Collections.Generic;

namespace EvoLife.Analytics
{
    /// <summary>
    /// Bounded pending-upload queues for experiment analytics.
    /// Records stay until a flush is confirmed successful. Oldest items are dropped when
    /// a queue exceeds its configured maximum (FIFO overflow). One flush may be in flight
    /// at a time. Pure and testable; does not perform HTTP.
    /// </summary>
    public sealed class AnalyticsExportController
    {
        readonly int maxPendingSnapshots;
        readonly int maxPendingLifetimes;
        readonly int maxPendingV1Snapshots;
        readonly List<SnapshotCreateDto> pendingSnapshots = new List<SnapshotCreateDto>();
        readonly List<CreatureLifeRecordDto> pendingLifetimes = new List<CreatureLifeRecordDto>();
        readonly List<SimulationStatsSnapshot> pendingV1Snapshots = new List<SimulationStatsSnapshot>();
        List<GenerationSummaryDto> pendingGenerations;
        int generationEpoch;
        int flushedSnapshotCount;
        int flushedLifetimeCount;
        int flushedV1Count;
        int flushedGenerationEpoch;
        bool flushInFlight;

        public AnalyticsExportController(
            int maxPendingSnapshots = 64,
            int maxPendingLifetimes = 256,
            int maxPendingV1Snapshots = 64)
        {
            this.maxPendingSnapshots = Math.Max(1, maxPendingSnapshots);
            this.maxPendingLifetimes = Math.Max(1, maxPendingLifetimes);
            this.maxPendingV1Snapshots = Math.Max(1, maxPendingV1Snapshots);
        }

        public int PendingSnapshotCount => pendingSnapshots.Count;
        public int PendingLifetimeCount => pendingLifetimes.Count;
        public int PendingV1SnapshotCount => pendingV1Snapshots.Count;
        public int PendingGenerationCount => pendingGenerations != null ? pendingGenerations.Count : 0;
        public int OverflowDropped { get; private set; }
        public bool FlushInFlight => flushInFlight;
        public bool HasPending =>
            pendingSnapshots.Count > 0
            || pendingLifetimes.Count > 0
            || pendingV1Snapshots.Count > 0
            || PendingGenerationCount > 0;

        public void EnqueueSnapshot(SnapshotCreateDto snapshot)
        {
            if (snapshot == null)
            {
                return;
            }

            OverflowDropped += EnqueueBounded(pendingSnapshots, snapshot, maxPendingSnapshots);
        }

        public void EnqueueLifetimes(IList<CreatureLifeRecordDto> records)
        {
            if (records == null)
            {
                return;
            }

            for (var i = 0; i < records.Count; i++)
            {
                if (records[i] == null)
                {
                    continue;
                }

                OverflowDropped += EnqueueBounded(pendingLifetimes, records[i], maxPendingLifetimes);
            }
        }

        public void EnqueueV1Snapshot(SimulationStatsSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return;
            }

            OverflowDropped += EnqueueBounded(pendingV1Snapshots, snapshot, maxPendingV1Snapshots);
        }

        /// <summary>
        /// Replaces the pending generation upsert payload with the latest summaries.
        /// Failed uploads keep the newest unsent list.
        /// </summary>
        public void ReplaceGenerations(IList<GenerationSummaryDto> summaries)
        {
            if (summaries == null || summaries.Count == 0)
            {
                return;
            }

            pendingGenerations = new List<GenerationSummaryDto>(summaries);
            generationEpoch++;
        }

        public AnalyticsFlushBatch TryBeginFlush()
        {
            if (flushInFlight || !HasPending)
            {
                return null;
            }

            flushInFlight = true;
            flushedSnapshotCount = pendingSnapshots.Count;
            flushedLifetimeCount = pendingLifetimes.Count;
            flushedV1Count = pendingV1Snapshots.Count;
            flushedGenerationEpoch = generationEpoch;

            return new AnalyticsFlushBatch(
                pendingSnapshots.Count > 0 ? new List<SnapshotCreateDto>(pendingSnapshots) : null,
                pendingLifetimes.Count > 0 ? new List<CreatureLifeRecordDto>(pendingLifetimes) : null,
                pendingGenerations != null && pendingGenerations.Count > 0
                    ? new List<GenerationSummaryDto>(pendingGenerations)
                    : null,
                pendingV1Snapshots.Count > 0 ? new List<SimulationStatsSnapshot>(pendingV1Snapshots) : null);
        }

        public void CompleteFlush(
            bool snapshotsSucceeded,
            bool lifetimesSucceeded,
            bool generationsSucceeded,
            int v1SuccessCount)
        {
            if (!flushInFlight)
            {
                return;
            }

            if (snapshotsSucceeded && flushedSnapshotCount > 0)
            {
                DequeueFront(pendingSnapshots, flushedSnapshotCount);
            }

            if (lifetimesSucceeded && flushedLifetimeCount > 0)
            {
                DequeueFront(pendingLifetimes, flushedLifetimeCount);
            }

            if (v1SuccessCount > 0)
            {
                DequeueFront(pendingV1Snapshots, Math.Min(v1SuccessCount, flushedV1Count));
            }

            if (generationsSucceeded && flushedGenerationEpoch == generationEpoch)
            {
                pendingGenerations = null;
            }

            flushedSnapshotCount = 0;
            flushedLifetimeCount = 0;
            flushedV1Count = 0;
            flushInFlight = false;
        }

        static int EnqueueBounded<T>(List<T> queue, T item, int max)
        {
            var dropped = 0;
            while (queue.Count >= max)
            {
                queue.RemoveAt(0);
                dropped++;
            }

            queue.Add(item);
            return dropped;
        }

        static void DequeueFront<T>(List<T> queue, int count)
        {
            var remove = Math.Min(count, queue.Count);
            if (remove > 0)
            {
                queue.RemoveRange(0, remove);
            }
        }
    }

    public sealed class AnalyticsFlushBatch
    {
        public AnalyticsFlushBatch(
            IList<SnapshotCreateDto> snapshots,
            IList<CreatureLifeRecordDto> lifetimes,
            IList<GenerationSummaryDto> generations,
            IList<SimulationStatsSnapshot> v1Snapshots)
        {
            Snapshots = snapshots;
            Lifetimes = lifetimes;
            Generations = generations;
            V1Snapshots = v1Snapshots;
        }

        public IList<SnapshotCreateDto> Snapshots { get; }
        public IList<CreatureLifeRecordDto> Lifetimes { get; }
        public IList<GenerationSummaryDto> Generations { get; }
        public IList<SimulationStatsSnapshot> V1Snapshots { get; }
    }
}
