using NUnit.Framework;
using UnityEngine;
using EvoLife.Common;
using EvoLife.Environment;

namespace EvoLife.Tests
{
    public sealed class DayNightCycleTests
    {
        [Test]
        public void Tick_AdvancesNormalizedTimeFromSimulationDelta()
        {
            var cycle = new DayNightCycle(dayDurationSeconds: 10f, nightStartNormalized: 0.5f);
            cycle.Tick(4f);

            Assert.AreEqual(0.4f, cycle.NormalizedTimeOfDay, 0.0001f);
            Assert.IsTrue(cycle.IsDay);
            Assert.AreEqual(DayNightPhase.Day, cycle.Phase);
        }

        [Test]
        public void Tick_CrossesIntoNightAndWrapsDeterministically()
        {
            var cycle = new DayNightCycle(dayDurationSeconds: 10f, nightStartNormalized: 0.5f);
            cycle.Tick(6f);
            Assert.IsTrue(cycle.IsNight);
            Assert.AreEqual(0.6f, cycle.NormalizedTimeOfDay, 0.0001f);

            cycle.Tick(10f);
            Assert.AreEqual(0.6f, cycle.NormalizedTimeOfDay, 0.0001f);
        }

        [Test]
        public void Tick_IgnoresNonPositiveDelta()
        {
            var cycle = new DayNightCycle(dayDurationSeconds: 8f);
            cycle.Tick(2f);
            cycle.Tick(0f);
            cycle.Tick(-3f);

            Assert.AreEqual(0.25f, cycle.NormalizedTimeOfDay, 0.0001f);
        }

        [Test]
        public void DayNightManager_MatchesCycleAndDoesNotUseWallClock()
        {
            var go = new GameObject("DayNight");
            try
            {
                var manager = go.AddComponent<DayNightManager>();
                manager.Configure(20f, nightStart: 0.75f);
                manager.Tick(5f);
                Assert.AreEqual(0.25f, manager.NormalizedTimeOfDay, 0.0001f);
                Assert.IsTrue(manager.IsDay);

                manager.Tick(12f);
                Assert.IsTrue(manager.IsNight);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }

    public sealed class BiomeMapTests
    {
        [Test]
        public void Resolve_UsesFirstContainingZoneThenDefault()
        {
            var map = new BiomeMap();
            map.ConfigureDefaults(BiomeKind.Grassland, regenMultiplier: 1f);
            map.AddZone(BiomeZone.Create(BiomeKind.Forest, Vector3.zero, 5f, density: 0.08f, regen: 1.25f));
            map.AddZone(BiomeZone.Create(BiomeKind.Rocky, new Vector3(20f, 0f, 0f), 4f, density: 0.01f, regen: 0.4f));

            Assert.AreEqual(BiomeKind.Forest, map.ResolveKind(Vector3.zero));
            Assert.AreEqual(BiomeKind.Rocky, map.ResolveKind(new Vector3(20f, 0f, 0f)));
            Assert.AreEqual(BiomeKind.Grassland, map.ResolveKind(new Vector3(100f, 0f, 0f)));
            Assert.AreEqual(1.25f, map.RegenMultiplierAt(Vector3.zero));
        }
    }

    public sealed class ResourceManagerTests
    {
        [Test]
        public void SeededPlacement_IsDeterministicAndDoesNotRespawnOnTick()
        {
            var objects = new System.Collections.Generic.List<GameObject>();
            try
            {
                var a = CreateManager(objects, seed: 7, density: 0.03f, radius: 8f);
                var b = CreateManager(objects, seed: 7, density: 0.03f, radius: 8f);
                a.PlaceResources();
                b.PlaceResources();

                Assert.Greater(a.Plants.Count, 0);
                Assert.AreEqual(a.Plants.Count, b.Plants.Count);
                Assert.AreEqual(a.Plants[0].Position, b.Plants[0].Position);

                var count = a.Plants.Count;
                a.Tick(1f);
                a.EnsurePlaced();
                Assert.AreEqual(count, a.Plants.Count);
            }
            finally
            {
                Cleanup(objects);
            }
        }

        [Test]
        public void Census_ReportsPlantCountDensityAndAbundance()
        {
            var objects = new System.Collections.Generic.List<GameObject>();
            try
            {
                var manager = CreateManager(objects, seed: 3, density: 0.02f, radius: 10f);
                manager.PlaceResources();
                var census = manager.CaptureCensus();

                Assert.AreEqual(manager.Plants.Count, census.PlantCount);
                Assert.Greater(census.WorldArea, 0f);
                Assert.AreEqual(census.PlantCount / census.WorldArea, census.PlantDensity, 0.0001f);
                Assert.Greater(census.PlantAbundance, 0f);
            }
            finally
            {
                Cleanup(objects);
            }
        }

        [Test]
        public void CensusProperties_ShareFrameCache_CaptureCensusIsFresh()
        {
            var objects = new System.Collections.Generic.List<GameObject>();
            try
            {
                var manager = CreateManager(objects, seed: 5, density: 0.03f, radius: 8f);
                manager.PlaceResources();
                Assert.Greater(manager.Plants.Count, 0);

                var first = manager.TotalPlantFoodRemaining;
                var second = manager.TotalPlantFoodRemaining;
                Assert.AreEqual(first, second, 0.0001f);

                var taken = manager.Plants[0].TryConsume(5f);
                Assert.Greater(taken, 0f);
                var cached = manager.TotalPlantFoodRemaining;
                var live = manager.CaptureCensus().TotalPlantFoodRemaining;
                Assert.AreEqual(first, cached, 0.0001f);
                Assert.AreEqual(first - taken, live, 0.0001f);
            }
            finally
            {
                Cleanup(objects);
            }
        }

        static ResourceManager CreateManager(
            System.Collections.Generic.List<GameObject> objects,
            int seed,
            float density,
            float radius)
        {
            var go = new GameObject("Resources");
            objects.Add(go);
            var registry = go.AddComponent<ResourceRegistry>();
            var manager = go.AddComponent<ResourceManager>();
            var settings = new PlantSpawnSettings
            {
                Seed = seed,
                WorldRadius = radius,
                DefaultDensity = density,
                MinSeparation = 0.5f,
                DefaultCapacity = 10f,
                DefaultRemaining = 10f,
                DefaultRegenPerSecond = 1f
            };
            manager.Configure(registry, settings, waterCount: 1);
            return manager;
        }

        static void Cleanup(System.Collections.Generic.List<GameObject> objects)
        {
            for (var i = 0; i < objects.Count; i++)
            {
                if (objects[i] != null)
                {
                    Object.DestroyImmediate(objects[i]);
                }
            }
        }
    }
}
