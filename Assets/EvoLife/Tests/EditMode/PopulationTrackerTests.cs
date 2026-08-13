using NUnit.Framework;
using EvoLife.Common;
using EvoLife.Simulation;
using UnityEngine;

namespace EvoLife.Tests
{
    public sealed class PopulationTrackerTests
    {
        [Test]
        public void RegisterAndUnregister_UpdatesCounts()
        {
            var go = new GameObject("PopulationTrackerTest");
            var tracker = go.AddComponent<PopulationTracker>();

            tracker.Register(new CreatureId(1), CreatureRole.Herbivore);
            tracker.Register(new CreatureId(2), CreatureRole.Predator);
            tracker.Register(new CreatureId(3), CreatureRole.Herbivore);

            Assert.AreEqual(2, tracker.HerbivoreCount);
            Assert.AreEqual(1, tracker.PredatorCount);
            Assert.AreEqual(3, tracker.TotalAlive);

            tracker.Unregister(new CreatureId(1));

            Assert.AreEqual(1, tracker.HerbivoreCount);
            Assert.AreEqual(2, tracker.TotalAlive);
            Assert.AreEqual(3, tracker.Births);
            Assert.AreEqual(1, tracker.Deaths);

            tracker.Unregister(new CreatureId(1));
            Assert.AreEqual(1, tracker.Deaths);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void EmptyTracker_HasZeroCounts()
        {
            var go = new GameObject("PopulationTrackerEmpty");
            var tracker = go.AddComponent<PopulationTracker>();

            Assert.AreEqual(0, tracker.HerbivoreCount);
            Assert.AreEqual(0, tracker.PredatorCount);
            Assert.AreEqual(0, tracker.TotalAlive);
            Assert.AreEqual(0, tracker.Births);
            Assert.AreEqual(0, tracker.Deaths);

            Object.DestroyImmediate(go);
        }
    }
}
