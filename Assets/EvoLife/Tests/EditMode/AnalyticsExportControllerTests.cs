using NUnit.Framework;
using EvoLife.Analytics;

namespace EvoLife.Tests
{
    public sealed class AnalyticsExportControllerTests
    {
        [Test]
        public void FailedFlush_RetainsPendingRecords()
        {
            var controller = new AnalyticsExportController();
            controller.EnqueueSnapshot(Snapshot(1f));
            controller.EnqueueLifetimes(new[] { Lifetime("a") });
            controller.ReplaceGenerations(new[] { Generation("herb", 0) });

            var batch = controller.TryBeginFlush();
            Assert.NotNull(batch);
            Assert.AreEqual(1, batch.Snapshots.Count);
            Assert.AreEqual(1, batch.Lifetimes.Count);
            Assert.AreEqual(1, batch.Generations.Count);

            controller.CompleteFlush(false, false, false, 0);

            Assert.AreEqual(1, controller.PendingSnapshotCount);
            Assert.AreEqual(1, controller.PendingLifetimeCount);
            Assert.AreEqual(1, controller.PendingGenerationCount);
            Assert.IsFalse(controller.FlushInFlight);
        }

        [Test]
        public void SuccessfulFlush_DequeuesPendingRecords()
        {
            var controller = new AnalyticsExportController();
            controller.EnqueueSnapshot(Snapshot(1f));
            controller.EnqueueLifetimes(new[] { Lifetime("a"), Lifetime("b") });
            controller.ReplaceGenerations(new[] { Generation("herb", 0) });

            Assert.NotNull(controller.TryBeginFlush());
            controller.CompleteFlush(true, true, true, 0);

            Assert.AreEqual(0, controller.PendingSnapshotCount);
            Assert.AreEqual(0, controller.PendingLifetimeCount);
            Assert.AreEqual(0, controller.PendingGenerationCount);
            Assert.IsFalse(controller.HasPending);
        }

        [Test]
        public void PartialSuccess_OnlyDequeuesSuccessfulStreams()
        {
            var controller = new AnalyticsExportController();
            controller.EnqueueSnapshot(Snapshot(1f));
            controller.EnqueueLifetimes(new[] { Lifetime("a") });

            Assert.NotNull(controller.TryBeginFlush());
            controller.CompleteFlush(true, false, true, 0);

            Assert.AreEqual(0, controller.PendingSnapshotCount);
            Assert.AreEqual(1, controller.PendingLifetimeCount);
        }

        [Test]
        public void BoundedQueue_DropsOldestOnOverflow()
        {
            var controller = new AnalyticsExportController(maxPendingSnapshots: 2, maxPendingLifetimes: 2);
            controller.EnqueueSnapshot(Snapshot(1f));
            controller.EnqueueSnapshot(Snapshot(2f));
            controller.EnqueueSnapshot(Snapshot(3f));

            Assert.AreEqual(2, controller.PendingSnapshotCount);
            Assert.AreEqual(1, controller.OverflowDropped);

            var batch = controller.TryBeginFlush();
            Assert.AreEqual(2f, batch.Snapshots[0].SimulationTime);
            Assert.AreEqual(3f, batch.Snapshots[1].SimulationTime);
        }

        [Test]
        public void InFlightGuard_RejectsSecondFlush()
        {
            var controller = new AnalyticsExportController();
            controller.EnqueueSnapshot(Snapshot(1f));
            Assert.NotNull(controller.TryBeginFlush());
            Assert.IsNull(controller.TryBeginFlush());
            controller.CompleteFlush(false, true, true, 0);
            Assert.NotNull(controller.TryBeginFlush());
        }

        [Test]
        public void RecordsAddedDuringFlush_AreNotDequeuedOnSuccess()
        {
            var controller = new AnalyticsExportController();
            controller.EnqueueSnapshot(Snapshot(1f));
            Assert.NotNull(controller.TryBeginFlush());
            controller.EnqueueSnapshot(Snapshot(2f));
            controller.CompleteFlush(true, true, true, 0);
            Assert.AreEqual(1, controller.PendingSnapshotCount);
        }

        [Test]
        public void NewerGenerationsDuringFlush_AreKeptWhenEpochChanges()
        {
            var controller = new AnalyticsExportController();
            controller.ReplaceGenerations(new[] { Generation("herb", 0) });
            Assert.NotNull(controller.TryBeginFlush());
            controller.ReplaceGenerations(new[] { Generation("herb", 1) });
            controller.CompleteFlush(true, true, true, 0);
            Assert.AreEqual(1, controller.PendingGenerationCount);
        }

        [Test]
        public void V1PartialSuccess_DequeuesOnlyPostedPrefix()
        {
            var controller = new AnalyticsExportController();
            controller.EnqueueV1Snapshot(new SimulationStatsSnapshot { simulationTimeSeconds = 1f });
            controller.EnqueueV1Snapshot(new SimulationStatsSnapshot { simulationTimeSeconds = 2f });
            Assert.NotNull(controller.TryBeginFlush());
            controller.CompleteFlush(true, true, true, 1);
            Assert.AreEqual(1, controller.PendingV1SnapshotCount);
        }

        static SnapshotCreateDto Snapshot(float time) =>
            new SnapshotCreateDto { SimulationTime = time, HerbivorePopulation = 1 };

        static CreatureLifeRecordDto Lifetime(string id) =>
            new CreatureLifeRecordDto { CreatureId = id, Species = "herb" };

        static GenerationSummaryDto Generation(string species, int generation) =>
            new GenerationSummaryDto { Species = species, Generation = generation, PopulationCount = 1 };
    }
}
