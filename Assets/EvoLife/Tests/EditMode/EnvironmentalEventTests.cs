using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using EvoLife.Common;
using EvoLife.Creatures;
using EvoLife.Environment;
using EvoLife.Simulation;

namespace EvoLife.Tests
{
    public sealed class EnvironmentalEventTests
    {
        [Test]
        public void Drought_ReducesConfiguredRegeneration()
        {
            using (var fixture = new EventFixture())
            {
                var plant = fixture.AddPlant(capacity: 20f, remaining: 0f, regen: 4f);
                fixture.Events.Trigger(new EnvironmentalEventDefinition
                {
                    Kind = EnvironmentalEventKind.Drought,
                    DurationSeconds = 10f,
                    PlantRegenMultiplier = 0.25f
                });

                plant.Tick(1f);
                Assert.AreEqual(1f, plant.AvailableAmount, 0.0001f);
            }
        }

        [Test]
        public void FoodBoom_IncreasesAvailability()
        {
            using (var fixture = new EventFixture())
            {
                var plant = fixture.AddPlant(capacity: 20f, remaining: 2f, regen: 0f);
                fixture.Events.Trigger(new EnvironmentalEventDefinition
                {
                    Kind = EnvironmentalEventKind.FoodBoom,
                    DurationSeconds = 8f,
                    PlantRegenMultiplier = 2f,
                    PlantAvailabilityBoost = 11f
                });

                Assert.AreEqual(13f, plant.AvailableAmount, 0.0001f);
            }
        }

        [Test]
        public void EventEnd_RestoresRegenModifiers()
        {
            using (var fixture = new EventFixture())
            {
                var plant = fixture.AddPlant(capacity: 20f, remaining: 0f, regen: 2f);
                fixture.Events.Trigger(new EnvironmentalEventDefinition
                {
                    Kind = EnvironmentalEventKind.Drought,
                    DurationSeconds = 2f,
                    PlantRegenMultiplier = 0.5f
                });

                Assert.AreEqual(1f, plant.EffectiveRegenPerSecond, 0.0001f);
                fixture.Events.Tick(2f);
                Assert.IsFalse(fixture.Events.HasActiveEvent(EnvironmentalEventKind.Drought));
                Assert.AreEqual(2f, plant.EffectiveRegenPerSecond, 0.0001f);
            }
        }

        [Test]
        public void Wildfire_DoesNotDoubleApplyDeath()
        {
            var rates = new MetabolicRates(
                maxHealth: 10f,
                maxEnergy: 100f,
                maxAge: 1000f,
                hungerIncreaseRate: 0f,
                thirstIncreaseRate: 0f,
                passiveEnergyConsumption: 0f,
                walkingEnergyConsumption: 0f,
                sprintingEnergyConsumption: 0f,
                attackEnergyConsumption: 0f,
                restingRecovery: 0f,
                starvationDamage: 0f,
                dehydrationDamage: 0f,
                hungerCapacity: 100f,
                thirstCapacity: 100f,
                starvationThreshold: 100f,
                dehydrationThreshold: 100f);
            var biology = new CreatureBiology(rates);
            var vitals = new RecordingVitals(biology);

            using (var fixture = new EventFixture())
            {
                fixture.Events.Bind(fixture.Resources, vitals, fixture.Population);
                fixture.Events.Trigger(new EnvironmentalEventDefinition
                {
                    Kind = EnvironmentalEventKind.Wildfire,
                    DurationSeconds = 4f,
                    DamagePulse = 50f,
                    DamagePerSecond = 20f,
                    PlantDepletionFraction = 0.5f
                });

                Assert.AreEqual(1, vitals.DeathCount);
                Assert.IsFalse(biology.IsAlive);

                fixture.Events.Tick(1f);
                fixture.Events.Tick(1f);
                Assert.AreEqual(1, vitals.DeathCount);
                Assert.GreaterOrEqual(vitals.Calls, 1);
            }
        }

        [Test]
        public void EventManager_DoesNotOwnCreatureBiology()
        {
            using (var fixture = new EventFixture())
            {
                var vitals = new RecordingVitals(biology: null);
                fixture.Events.Bind(fixture.Resources, vitals, fixture.Population);
                fixture.Events.Trigger(EnvironmentalEventKind.DiseasePressure);
                fixture.Events.Tick(1f);

                Assert.Greater(vitals.Calls, 0);
                Assert.AreEqual(0, vitals.DeathCount);
                Assert.IsNull(fixture.Events.GetComponent<CreatureVitals>());
            }
        }

        [Test]
        public void PredatorEvents_GoThroughPopulationPorts()
        {
            using (var fixture = new EventFixture())
            {
                fixture.Events.Trigger(EnvironmentalEventKind.PredatorIntroduction);
                fixture.Events.Trigger(EnvironmentalEventKind.PredatorRemoval);

                Assert.AreEqual(1, fixture.Population.Spawns.Count);
                Assert.AreEqual(CreatureRole.Predator, fixture.Population.Spawns[0].role);
                Assert.AreEqual(2, fixture.Population.Spawns[0].count);
                Assert.AreEqual(1, fixture.Population.Removals.Count);
                Assert.AreEqual(CreatureRole.Predator, fixture.Population.Removals[0].role);
            }
        }

        [Test]
        public void Schedule_IsDeterministicForTheSameConfig()
        {
            var schedule = new[]
            {
                new ScheduledEnvironmentalEvent { AtSimulationTime = 3f, Kind = EnvironmentalEventKind.Drought },
                new ScheduledEnvironmentalEvent { AtSimulationTime = 8f, Kind = EnvironmentalEventKind.FoodBoom }
            };

            var first = CollectKinds(schedule);
            var second = CollectKinds(schedule);

            CollectionAssert.AreEqual(first, second);
            Assert.AreEqual(EnvironmentalEventKind.Drought, first[0]);
            Assert.AreEqual(EnvironmentalEventKind.FoodBoom, first[1]);
        }

        [Test]
        public void RemovalAndSpawn_UseLifecycleApis()
        {
            var objects = new List<GameObject>();
            var vitalsDef = ScriptableObject.CreateInstance<SpeciesVitalsDefinition>();
            var config = ScriptableObject.CreateInstance<SimulationConfig>();
            try
            {
                var root = Create(objects, "LifecycleRoot");
                var tracker = root.AddComponent<PopulationTracker>();
                var hub = root.AddComponent<CreatureLifecycleHub>();
                hub.Bind(tracker);
                var spawner = root.AddComponent<CreatureSpawner>();
                spawner.Configure(tracker, hub, vitals: vitalsDef);
                var prefab = Create(objects, "PredatorPrefab");
                prefab.AddComponent<CreatureIdentity>();
                prefab.AddComponent<CreatureVitals>();
                prefab.AddComponent<CreatureGenome>();

                var spawned = spawner.Spawn(
                    prefab,
                    Vector3.zero,
                    "predator",
                    CreatureRole.Predator);
                objects.Add(spawned);
                Assert.AreEqual(1, tracker.PredatorCount);

                var bridge = root.AddComponent<EnvironmentalCreatureBridge>();
                bridge.Configure(spawner, hub, tracker, config, prefab, prefab, root.transform, randomSeed: 11);

                var events = root.AddComponent<EnvironmentalEventManager>();
                events.Bind(null, bridge, bridge);

                events.Trigger(new EnvironmentalEventDefinition
                {
                    Kind = EnvironmentalEventKind.PredatorRemoval,
                    DurationSeconds = 0f,
                    PredatorRemoveCount = 1
                });
                Assert.AreEqual(0, tracker.PredatorCount);
                Assert.AreEqual(1, tracker.Deaths);

                events.Trigger(new EnvironmentalEventDefinition
                {
                    Kind = EnvironmentalEventKind.PredatorIntroduction,
                    DurationSeconds = 0f,
                    PredatorSpawnCount = 2
                });
                Assert.AreEqual(2, tracker.PredatorCount);
                Assert.AreEqual(3, tracker.Births);
            }
            finally
            {
                var identities = Object.FindObjectsOfType<CreatureIdentity>();
                for (var i = 0; i < identities.Length; i++)
                {
                    if (identities[i] != null)
                    {
                        Object.DestroyImmediate(identities[i].gameObject);
                    }
                }

                for (var i = 0; i < objects.Count; i++)
                {
                    if (objects[i] != null)
                    {
                        Object.DestroyImmediate(objects[i]);
                    }
                }

                if (vitalsDef != null)
                {
                    Object.DestroyImmediate(vitalsDef);
                }

                if (config != null)
                {
                    Object.DestroyImmediate(config);
                }
            }
        }

        static List<EnvironmentalEventKind> CollectKinds(IReadOnlyList<ScheduledEnvironmentalEvent> schedule)
        {
            var kinds = new List<EnvironmentalEventKind>();
            var scheduler = new EnvironmentalEventScheduler(schedule);
            scheduler.CollectDue(0f, 2f, kinds);
            Assert.AreEqual(0, kinds.Count);
            scheduler.CollectDue(2f, 4f, kinds);
            scheduler.CollectDue(4f, 10f, kinds);
            return kinds;
        }

        static GameObject Create(List<GameObject> objects, string name)
        {
            var go = new GameObject(name);
            objects.Add(go);
            return go;
        }

        sealed class RecordingVitals : IEnvironmentalVitalEffects
        {
            public RecordingVitals(CreatureBiology biology)
            {
                Biology = biology;
                if (Biology != null)
                {
                    Biology.Died += _ => DeathCount++;
                }
            }

            public CreatureBiology Biology { get; }
            public int Calls { get; private set; }
            public int DeathCount { get; private set; }

            public int ApplyEnvironmentalDamage(float amount, DeathCause cause)
            {
                Calls++;
                if (Biology == null)
                {
                    return 0;
                }

                var living = Biology.IsAlive;
                Biology.TakeDamage(amount, cause);
                return living ? 1 : 0;
            }
        }

        sealed class RecordingPopulation : IEnvironmentalPopulationCommands
        {
            public readonly List<(CreatureRole role, int count)> Spawns = new List<(CreatureRole role, int count)>();
            public readonly List<(CreatureRole role, int count)> Removals = new List<(CreatureRole role, int count)>();

            public int SpawnRole(CreatureRole role, int count)
            {
                Spawns.Add((role, count));
                return count;
            }

            public int RemoveRole(CreatureRole role, int count)
            {
                Removals.Add((role, count));
                return count;
            }
        }

        sealed class EventFixture : System.IDisposable
        {
            readonly List<GameObject> objects = new List<GameObject>();

            public EventFixture()
            {
                Root = new GameObject("EventRoot");
                objects.Add(Root);
                Registry = Root.AddComponent<ResourceRegistry>();
                Resources = Root.AddComponent<ResourceManager>();
                Resources.Configure(Registry, new PlantSpawnSettings { DefaultDensity = 0f, WorldRadius = 1f }, waterCount: 0);
                Population = new RecordingPopulation();
                Events = Root.AddComponent<EnvironmentalEventManager>();
                Events.Bind(Resources, new RecordingVitals(null), Population);
            }

            public GameObject Root { get; }
            public ResourceRegistry Registry { get; }
            public ResourceManager Resources { get; }
            public EnvironmentalEventManager Events { get; }
            public RecordingPopulation Population { get; }

            public PlantResource AddPlant(float capacity, float remaining, float regen)
            {
                var go = new GameObject("Plant");
                objects.Add(go);
                var plant = go.AddComponent<PlantResource>();
                plant.Configure(capacity, remaining, regen, regenDelay: 0f, Registry);
                Resources.TrackPlant(plant);
                return plant;
            }

            public void Dispose()
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
}
