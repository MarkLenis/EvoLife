using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using EvoLife.AI;
using EvoLife.Common;
using EvoLife.Creatures;
using EvoLife.Genetics;
using EvoLife.Simulation;

namespace EvoLife.Tests
{
    public sealed class ReproductionTests
    {
        ReproductionFixture fixture;

        [SetUp]
        public void SetUp() => fixture = new ReproductionFixture();

        [TearDown]
        public void TearDown() => fixture.Dispose();

        [Test]
        public void EligiblePair_CanReproduce()
        {
            var a = fixture.SpawnAdult("herbivore", CreatureRole.Herbivore, Vector3.zero);
            var b = fixture.SpawnAdult("herbivore", CreatureRole.Herbivore, new Vector3(1f, 0f, 0f));
            var birthsBefore = fixture.Tracker.Births;

            var result = fixture.RequestFrom(a);

            Assert.IsTrue(result.Succeeded, result.Failure.ToString());
            Assert.AreEqual(1, fixture.Reproduction.LastRequestOffspringCount);
            Assert.AreEqual(birthsBefore + 1, fixture.Tracker.Births);
            Assert.AreEqual(3, fixture.Tracker.TotalAlive);
            Assert.IsNotNull(result.Offspring);
            fixture.Track(result.Offspring);
        }

        [Test]
        public void UnderageCreatures_CannotReproduce()
        {
            var a = fixture.SpawnAdult("herbivore", CreatureRole.Herbivore, Vector3.zero, age: 0f);
            fixture.SpawnAdult("herbivore", CreatureRole.Herbivore, new Vector3(1f, 0f, 0f), age: 0f);

            var result = fixture.RequestFrom(a);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(ReproductionFailureReason.RequesterIneligible, result.Failure);
            Assert.AreEqual(2, fixture.Tracker.TotalAlive);
        }

        [Test]
        public void LowEnergyCreatures_CannotReproduce()
        {
            var a = fixture.SpawnAdult("herbivore", CreatureRole.Herbivore, Vector3.zero);
            fixture.SpawnAdult("herbivore", CreatureRole.Herbivore, new Vector3(1f, 0f, 0f));
            a.GetComponent<CreatureVitals>().ConsumeEnergy(80f);

            var result = fixture.RequestFrom(a);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(ReproductionFailureReason.RequesterIneligible, result.Failure);
        }

        [Test]
        public void DeadCreatures_CannotReproduce()
        {
            var a = fixture.SpawnAdult("herbivore", CreatureRole.Herbivore, Vector3.zero);
            fixture.SpawnAdult("herbivore", CreatureRole.Herbivore, new Vector3(1f, 0f, 0f));
            a.GetComponent<CreatureVitals>().Die(DeathCause.OldAge);

            var result = fixture.RequestFrom(a);

            Assert.IsFalse(result.Succeeded);
            Assert.IsTrue(
                result.Failure == ReproductionFailureReason.RequesterIneligible
                || result.Failure == ReproductionFailureReason.RequesterMissing,
                result.Failure.ToString());
        }

        [Test]
        public void IncompatibleSpecies_CannotReproduce()
        {
            var a = fixture.SpawnAdult("herbivore", CreatureRole.Herbivore, Vector3.zero);
            fixture.SpawnAdult("other-grazer", CreatureRole.Herbivore, new Vector3(1f, 0f, 0f));

            var result = fixture.RequestFrom(a);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(ReproductionFailureReason.NoCompatibleMate, result.Failure);
            Assert.AreEqual(2, fixture.Tracker.TotalAlive);
        }

        [Test]
        public void Cooldown_BlocksRepeatedReproduction()
        {
            var a = fixture.SpawnAdult("herbivore", CreatureRole.Herbivore, Vector3.zero);
            fixture.SpawnAdult("herbivore", CreatureRole.Herbivore, new Vector3(1f, 0f, 0f));

            var first = fixture.RequestFrom(a);
            Assert.IsTrue(first.Succeeded, first.Failure.ToString());
            fixture.Track(first.Offspring);

            var blocked = fixture.RequestFrom(a);
            Assert.IsFalse(blocked.Succeeded);
            Assert.AreEqual(ReproductionFailureReason.RequesterIneligible, blocked.Failure);
            Assert.AreEqual(3, fixture.Tracker.TotalAlive);

            fixture.Clock.Advance(fixture.Settings.CooldownSeconds);
            var again = fixture.RequestFrom(a);
            Assert.IsTrue(again.Succeeded, again.Failure.ToString());
            fixture.Track(again.Offspring);
            Assert.AreEqual(4, fixture.Tracker.TotalAlive);
        }

        [Test]
        public void OffspringGeneration_IncrementsFromOldestParent()
        {
            var a = fixture.SpawnAdult("herbivore", CreatureRole.Herbivore, Vector3.zero, generation: 2);
            fixture.SpawnAdult("herbivore", CreatureRole.Herbivore, new Vector3(1f, 0f, 0f), generation: 4);

            var result = fixture.RequestFrom(a);
            Assert.IsTrue(result.Succeeded, result.Failure.ToString());
            fixture.Track(result.Offspring);

            var child = result.Offspring.GetComponent<CreatureIdentity>();
            Assert.AreEqual(5, child.Generation);
        }

        [Test]
        public void ParentIds_ArePreservedOnOffspring()
        {
            var a = fixture.SpawnAdult("herbivore", CreatureRole.Herbivore, Vector3.zero);
            var b = fixture.SpawnAdult("herbivore", CreatureRole.Herbivore, new Vector3(1f, 0f, 0f));

            var result = fixture.RequestFrom(a);
            Assert.IsTrue(result.Succeeded, result.Failure.ToString());
            fixture.Track(result.Offspring);

            var child = result.Offspring.GetComponent<CreatureIdentity>();
            var parentA = a.GetComponent<CreatureIdentity>();
            var parentB = b.GetComponent<CreatureIdentity>();
            Assert.AreEqual(parentA.Id, child.ParentA);
            Assert.AreEqual(parentB.Id, child.ParentB);
            Assert.AreEqual(parentA.SpeciesId, child.SpeciesId);
            Assert.AreEqual(parentA.Role, child.Role);
        }

        [Test]
        public void ChildGenome_UsesCrossover()
        {
            var low = Genome.CreateDefault();
            low.Set(TraitId.BodySize, 0.5f);
            var high = Genome.CreateDefault();
            high.Set(TraitId.BodySize, 3f);

            var a = fixture.SpawnAdult("herbivore", CreatureRole.Herbivore, Vector3.zero, genome: low);
            fixture.SpawnAdult("herbivore", CreatureRole.Herbivore, new Vector3(1f, 0f, 0f), genome: high);

            var result = fixture.RequestFrom(a);
            Assert.IsTrue(result.Succeeded, result.Failure.ToString());
            fixture.Track(result.Offspring);

            Assert.AreEqual(1.75f, result.OffspringGenome.Get(TraitId.BodySize), 0.0001f);
        }

        [Test]
        public void ZeroMutation_IsStableForIdenticalParents()
        {
            var genome = Genome.CreateDefault();
            var a = fixture.SpawnAdult("herbivore", CreatureRole.Herbivore, Vector3.zero, genome: genome);
            fixture.SpawnAdult("herbivore", CreatureRole.Herbivore, new Vector3(1f, 0f, 0f), genome: genome);

            var result = fixture.RequestFrom(a);
            Assert.IsTrue(result.Succeeded, result.Failure.ToString());
            fixture.Track(result.Offspring);
            CollectionAssert.AreEqual(genome.ToArray(), result.OffspringGenome.ToArray());
        }

        [Test]
        public void Mutation_RemainsInCanonicalBounds()
        {
            var settings = ReproductionSettings.ForTests(mutationProbability: 1f, mutationMagnitudeScale: 80f);
            var ops = new DefaultGeneticOperators(settings.ToGeneticsConfig());
            var parentA = Genome.CreateDefault();
            var parentB = Genome.CreateDefault();
            parentB.Set(TraitId.SprintSpeed, 8f);

            for (var seed = 1; seed <= 20; seed++)
            {
                var child = ops.CreateOffspring(parentA, parentB, new System.Random(seed));
                foreach (var trait in CanonicalGenomeSchema.All())
                {
                    var value = child.Get(trait.Id);
                    Assert.GreaterOrEqual(value, trait.HardMin, trait.CanonicalName);
                    Assert.LessOrEqual(value, trait.HardMax, trait.CanonicalName);
                }
            }
        }

        [Test]
        public void SeededReproduction_IsDeterministic()
        {
            var parentA = Genome.FromTraitValues((TraitId.VisionRange, 8f), (TraitId.Aggression, 0.2f));
            var parentB = Genome.FromTraitValues((TraitId.VisionRange, 20f), (TraitId.Aggression, 0.8f));
            var ops = new DefaultGeneticOperators(ReproductionSettings.ForTests(
                crossoverMode: CrossoverMode.RandomParent,
                mutationProbability: 0.5f,
                mutationMagnitudeScale: 1f).ToGeneticsConfig());

            var first = OffspringComposer.Compose(
                parentA, parentB, new CreatureId(1), new CreatureId(2), 0, 0,
                "herbivore", CreatureRole.Herbivore, Vector3.zero, AgentPolicyKind.ScriptedBaseline,
                ops, new System.Random(99));
            var second = OffspringComposer.Compose(
                parentA, parentB, new CreatureId(1), new CreatureId(2), 0, 0,
                "herbivore", CreatureRole.Herbivore, Vector3.zero, AgentPolicyKind.ScriptedBaseline,
                ops, new System.Random(99));

            CollectionAssert.AreEqual(first.Genome.ToArray(), second.Genome.ToArray());
            Assert.AreEqual(1, first.Generation);
            Assert.AreEqual(new CreatureId(1), first.ParentA);
            Assert.AreEqual(new CreatureId(2), first.ParentB);
        }

        [Test]
        public void PopulationTracker_UpdatesOnBirth()
        {
            fixture.SpawnAdult("herbivore", CreatureRole.Herbivore, Vector3.zero);
            fixture.SpawnAdult("herbivore", CreatureRole.Herbivore, new Vector3(1f, 0f, 0f));
            Assert.AreEqual(2, fixture.Tracker.Births);
            Assert.AreEqual(2, fixture.Tracker.HerbivoreCount);

            var result = fixture.RequestFrom(fixture.FirstAdult);
            Assert.IsTrue(result.Succeeded, result.Failure.ToString());
            fixture.Track(result.Offspring);

            Assert.AreEqual(3, fixture.Tracker.Births);
            Assert.AreEqual(3, fixture.Tracker.HerbivoreCount);
            Assert.AreEqual(0, fixture.Tracker.Deaths);
        }

        [Test]
        public void ReproductionRequest_NoOpsWithoutValidMate()
        {
            var a = fixture.SpawnAdult("herbivore", CreatureRole.Herbivore, Vector3.zero);
            fixture.SpawnAdult("herbivore", CreatureRole.Herbivore, new Vector3(100f, 0f, 0f));

            Assert.DoesNotThrow(() => fixture.RequestFrom(a));
            var result = fixture.Reproduction.LastResult;
            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(ReproductionFailureReason.NoCompatibleMate, result.Failure);
            Assert.AreEqual(0, fixture.Reproduction.LastRequestOffspringCount);
            Assert.AreEqual(2, fixture.Tracker.TotalAlive);
        }

        [Test]
        public void OneRequest_DoesNotCreateDuplicateBirths()
        {
            var a = fixture.SpawnAdult("herbivore", CreatureRole.Herbivore, Vector3.zero);
            fixture.SpawnAdult("herbivore", CreatureRole.Herbivore, new Vector3(0.5f, 0f, 0f));
            fixture.SpawnAdult("herbivore", CreatureRole.Herbivore, new Vector3(1f, 0f, 0f));
            var birthsBefore = fixture.Tracker.Births;

            var result = fixture.RequestFrom(a);
            Assert.IsTrue(result.Succeeded, result.Failure.ToString());
            fixture.Track(result.Offspring);

            Assert.AreEqual(1, fixture.Reproduction.LastRequestOffspringCount);
            Assert.AreEqual(birthsBefore + 1, fixture.Tracker.Births);
            Assert.AreEqual(4, fixture.Tracker.TotalAlive);
        }

        [Test]
        public void ActionSchemaRequest_ForwardsToSimulationHandler()
        {
            var a = fixture.SpawnAdult("herbivore", CreatureRole.Herbivore, Vector3.zero);
            fixture.SpawnAdult("herbivore", CreatureRole.Herbivore, new Vector3(1f, 0f, 0f));
            var vitals = a.GetComponent<CreatureVitals>();
            var identity = a.GetComponent<CreatureIdentity>();
            var bridge = a.GetComponent<CreatureReproductionBridge>();
            var interactor = new LocalCreatureInteractor(
                vitals,
                a.transform,
                null,
                identity,
                () => 12f,
                ScriptedBaselineSettings.HerbivoreDefaults(),
                bridge);

            Assert.IsTrue(CreatureActionExecution.TryApplyInteraction(
                interactor,
                CreatureActionSchema.InteractionReproduceRequest));
            Assert.IsTrue(fixture.Reproduction.LastResult.Succeeded);
            fixture.Track(fixture.Reproduction.LastResult.Offspring);
        }

        [Test]
        public void Eligibility_UsesGenomeReproductionThreshold()
        {
            var input = new ReproductionEligibilityInput(
                isAlive: true,
                age: 30f,
                maxAge: 100f,
                energy: 50f,
                maxEnergy: 100f,
                health: 80f,
                maxHealth: 100f,
                reproductionThreshold: 0.6f,
                hasReproduced: false,
                lastReproductionTime: 0f);

            Assert.IsFalse(ReproductionEligibility.IsEligible(input, ReproductionSettings.ForTests(), 0f));

            var highEnergy = new ReproductionEligibilityInput(
                true, 30f, 100f, 70f, 100f, 80f, 100f, 0.6f, false, 0f);
            Assert.IsTrue(ReproductionEligibility.IsEligible(highEnergy, ReproductionSettings.ForTests(), 0f));
        }

        [Test]
        public void MissingSpawner_DoesNotChargeParentsOrStartCooldown()
        {
            fixture.Settings.HealthCost = 4f;
            var a = fixture.SpawnAdult("herbivore", CreatureRole.Herbivore, Vector3.zero);
            var b = fixture.SpawnAdult("herbivore", CreatureRole.Herbivore, new Vector3(1f, 0f, 0f));
            var before = fixture.CaptureCosts(a, b);
            fixture.Reproduction.Configure(
                null,
                fixture.Tracker,
                fixture.Hub,
                fixture.Clock,
                fixture.Settings,
                new EcosystemSettings { MaxHerbivores = 80, MaxPredators = 24 },
                herbivore: fixture.Prefab,
                predator: fixture.Prefab);

            var result = fixture.RequestFrom(a);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(ReproductionFailureReason.SpawnFailed, result.Failure);
            fixture.AssertParentsUnchanged(a, b, before);
            Assert.AreEqual(0, fixture.Reproduction.LastRequestOffspringCount);
            Assert.AreEqual(before.Births, fixture.Tracker.Births);
        }

        [Test]
        public void MissingPrefab_DoesNotChargeParentsOrStartCooldown()
        {
            fixture.Settings.HealthCost = 4f;
            var a = fixture.SpawnAdult("herbivore", CreatureRole.Herbivore, Vector3.zero);
            var b = fixture.SpawnAdult("herbivore", CreatureRole.Herbivore, new Vector3(1f, 0f, 0f));
            var before = fixture.CaptureCosts(a, b);
            fixture.Reproduction.SetPrefabs(null, null);

            var result = fixture.RequestFrom(a);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(ReproductionFailureReason.SpawnFailed, result.Failure);
            fixture.AssertParentsUnchanged(a, b, before);
            Assert.AreEqual(before.Births, fixture.Tracker.Births);
        }

        [Test]
        public void SimulatedSpawnFailure_DoesNotChargeParentsOrStartCooldown()
        {
            fixture.Settings.HealthCost = 4f;
            var a = fixture.SpawnAdult("herbivore", CreatureRole.Herbivore, Vector3.zero);
            var b = fixture.SpawnAdult("herbivore", CreatureRole.Herbivore, new Vector3(1f, 0f, 0f));
            var before = fixture.CaptureCosts(a, b);
            fixture.Reproduction.ForceNextSpawnFailure();

            var result = fixture.RequestFrom(a);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(ReproductionFailureReason.SpawnFailed, result.Failure);
            fixture.AssertParentsUnchanged(a, b, before);
            Assert.AreEqual(0, fixture.Reproduction.LastRequestOffspringCount);
            Assert.AreEqual(before.Births, fixture.Tracker.Births);

            var retry = fixture.RequestFrom(a);
            Assert.IsTrue(retry.Succeeded, retry.Failure.ToString());
            fixture.Track(retry.Offspring);
            Assert.AreEqual(before.Births + 1, fixture.Tracker.Births);
        }

        [Test]
        public void SuccessfulSpawn_ChargesOnceAndStartsCooldownForBothParents()
        {
            fixture.Settings.HealthCost = 3f;
            var a = fixture.SpawnAdult("herbivore", CreatureRole.Herbivore, Vector3.zero);
            var b = fixture.SpawnAdult("herbivore", CreatureRole.Herbivore, new Vector3(1f, 0f, 0f));
            var before = fixture.CaptureCosts(a, b);

            var result = fixture.RequestFrom(a);

            Assert.IsTrue(result.Succeeded, result.Failure.ToString());
            fixture.Track(result.Offspring);
            Assert.AreEqual(1, fixture.Reproduction.LastRequestOffspringCount);
            Assert.AreEqual(before.Births + 1, fixture.Tracker.Births);
            Assert.AreEqual(before.EnergyA - fixture.Settings.EnergyCost, a.GetComponent<CreatureVitals>().Energy, 0.0001f);
            Assert.AreEqual(before.EnergyB - fixture.Settings.EnergyCost, b.GetComponent<CreatureVitals>().Energy, 0.0001f);
            Assert.AreEqual(before.HealthA - fixture.Settings.HealthCost, a.GetComponent<CreatureVitals>().Health, 0.0001f);
            Assert.AreEqual(before.HealthB - fixture.Settings.HealthCost, b.GetComponent<CreatureVitals>().Health, 0.0001f);
            Assert.IsTrue(fixture.Reproduction.HasReproductionTimestamp(a.GetComponent<CreatureIdentity>().Id));
            Assert.IsTrue(fixture.Reproduction.HasReproductionTimestamp(b.GetComponent<CreatureIdentity>().Id));

            var blockedA = fixture.RequestFrom(a);
            Assert.IsFalse(blockedA.Succeeded);
            Assert.AreEqual(ReproductionFailureReason.RequesterIneligible, blockedA.Failure);
            var blockedB = fixture.RequestFrom(b);
            Assert.IsFalse(blockedB.Succeeded);
            Assert.AreEqual(ReproductionFailureReason.RequesterIneligible, blockedB.Failure);
            Assert.AreEqual(1, fixture.Reproduction.LastRequestOffspringCount);
            Assert.AreEqual(before.Births + 1, fixture.Tracker.Births);
        }

        [Test]
        public void LethalReproductionCost_DoesNotRollbackSuccessfulOffspring()
        {
            fixture.Settings.HealthCost = 1000f;
            var a = fixture.SpawnAdult("herbivore", CreatureRole.Herbivore, Vector3.zero);
            var b = fixture.SpawnAdult("herbivore", CreatureRole.Herbivore, new Vector3(1f, 0f, 0f));
            var birthsBefore = fixture.Tracker.Births;

            var result = fixture.RequestFrom(a);

            Assert.IsTrue(result.Succeeded, result.Failure.ToString());
            Assert.IsNotNull(result.Offspring);
            fixture.Track(result.Offspring);
            Assert.AreEqual(1, fixture.Reproduction.LastRequestOffspringCount);
            Assert.AreEqual(birthsBefore + 1, fixture.Tracker.Births);
            Assert.IsFalse(a.GetComponent<CreatureVitals>().IsAlive);
            Assert.IsFalse(b.GetComponent<CreatureVitals>().IsAlive);
            Assert.IsTrue(result.Offspring.GetComponent<CreatureVitals>().IsAlive);
        }

        sealed class ReproductionFixture
        {
            readonly List<GameObject> objects = new List<GameObject>();
            public readonly SpeciesVitalsDefinition VitalsDef;
            public readonly PopulationTracker Tracker;
            public readonly CreatureLifecycleHub Hub;
            public readonly CreatureSpawner Spawner;
            public readonly ReproductionSystem Reproduction;
            public readonly SimulationClock Clock;
            public readonly ReproductionSettings Settings;
            public readonly GameObject Prefab;
            public GameObject FirstAdult { get; private set; }

            public ReproductionFixture()
            {
                var root = NewObject("ReproductionRoot");
                Tracker = root.AddComponent<PopulationTracker>();
                Hub = root.AddComponent<CreatureLifecycleHub>();
                Hub.Bind(Tracker);
                Clock = root.AddComponent<SimulationClock>();
                Spawner = root.AddComponent<CreatureSpawner>();
                Reproduction = root.AddComponent<ReproductionSystem>();
                VitalsDef = ScriptableObject.CreateInstance<SpeciesVitalsDefinition>();
                Settings = ReproductionSettings.ForTests();
                Prefab = CreatePrefab();
                Spawner.Configure(Tracker, Hub, Reproduction, VitalsDef);
                Reproduction.Configure(
                    Spawner,
                    Tracker,
                    Hub,
                    Clock,
                    Settings,
                    new EcosystemSettings { MaxHerbivores = 80, MaxPredators = 24 },
                    herbivore: Prefab,
                    predator: Prefab);
                Reproduction.SetSeed(11);
            }

            public GameObject SpawnAdult(
                string species,
                CreatureRole role,
                Vector3 position,
                float age = 30f,
                int generation = 0,
                Genome genome = null)
            {
                var instance = Spawner.Spawn(
                    Prefab,
                    position,
                    species,
                    role,
                    genome ?? Genome.CreateDefault(),
                    AgentPolicyKind.ScriptedBaseline,
                    generation);
                Track(instance);
                if (FirstAdult == null)
                {
                    FirstAdult = instance;
                }

                var vitals = instance.GetComponent<CreatureVitals>();
                vitals.Initialize(VitalsDef, age);
                return instance;
            }

            public ReproductionResult RequestFrom(GameObject requester)
            {
                var identity = requester.GetComponent<CreatureIdentity>();
                return Reproduction.TryReproduce(identity.Id);
            }

            public ParentCostSnapshot CaptureCosts(GameObject a, GameObject b)
            {
                var vitalsA = a.GetComponent<CreatureVitals>();
                var vitalsB = b.GetComponent<CreatureVitals>();
                return new ParentCostSnapshot(
                    vitalsA.Energy,
                    vitalsB.Energy,
                    vitalsA.Health,
                    vitalsB.Health,
                    Tracker.Births);
            }

            public void AssertParentsUnchanged(GameObject a, GameObject b, ParentCostSnapshot before)
            {
                var vitalsA = a.GetComponent<CreatureVitals>();
                var vitalsB = b.GetComponent<CreatureVitals>();
                Assert.AreEqual(before.EnergyA, vitalsA.Energy, 0.0001f);
                Assert.AreEqual(before.EnergyB, vitalsB.Energy, 0.0001f);
                Assert.AreEqual(before.HealthA, vitalsA.Health, 0.0001f);
                Assert.AreEqual(before.HealthB, vitalsB.Health, 0.0001f);
                Assert.IsFalse(Reproduction.HasReproductionTimestamp(a.GetComponent<CreatureIdentity>().Id));
                Assert.IsFalse(Reproduction.HasReproductionTimestamp(b.GetComponent<CreatureIdentity>().Id));
            }

            public void Track(GameObject instance)
            {
                if (instance != null && !objects.Contains(instance))
                {
                    objects.Add(instance);
                }
            }

            public void Dispose()
            {
                Track(Reproduction != null ? Reproduction.LastResult.Offspring : null);
                for (var i = 0; i < objects.Count; i++)
                {
                    if (objects[i] != null)
                    {
                        UnityEngine.Object.DestroyImmediate(objects[i]);
                    }
                }

                if (VitalsDef != null)
                {
                    UnityEngine.Object.DestroyImmediate(VitalsDef);
                }
            }

            GameObject NewObject(string name)
            {
                var go = new GameObject(name);
                objects.Add(go);
                return go;
            }

            GameObject CreatePrefab()
            {
                var prefab = NewObject("CreaturePrefab");
                prefab.AddComponent<CreatureIdentity>();
                prefab.AddComponent<CreatureVitals>();
                prefab.AddComponent<CreatureGenome>();
                prefab.AddComponent<CreatureCapabilityMotor>();
                prefab.AddComponent<CreatureReproductionBridge>();
                return prefab;
            }
        }

        public readonly struct ParentCostSnapshot
        {
            public ParentCostSnapshot(float energyA, float energyB, float healthA, float healthB, int births)
            {
                EnergyA = energyA;
                EnergyB = energyB;
                HealthA = healthA;
                HealthB = healthB;
                Births = births;
            }

            public float EnergyA { get; }
            public float EnergyB { get; }
            public float HealthA { get; }
            public float HealthB { get; }
            public int Births { get; }
        }
    }

    public sealed class EcosystemLifecycleTests
    {
        [Test]
        public void ExtinctionEvaluator_ReportsRoleAndTotalExtinction()
        {
            Assert.AreEqual(ExtinctionState.EcosystemExtinct, ExtinctionEvaluator.Evaluate(null));
            Assert.AreEqual(
                ExtinctionState.None,
                ExtinctionEvaluator.Evaluate(new StubPopulation { HerbivoreCount = 2, PredatorCount = 1 }));
            Assert.AreEqual(
                ExtinctionState.HerbivoresExtinct,
                ExtinctionEvaluator.Evaluate(new StubPopulation { HerbivoreCount = 0, PredatorCount = 3 }));
            Assert.AreEqual(
                ExtinctionState.PredatorsExtinct,
                ExtinctionEvaluator.Evaluate(new StubPopulation { HerbivoreCount = 4, PredatorCount = 0 }));
            Assert.AreEqual(
                ExtinctionState.EcosystemExtinct,
                ExtinctionEvaluator.Evaluate(new StubPopulation { HerbivoreCount = 0, PredatorCount = 0 }));
        }

        [Test]
        public void TrainingRespawn_SpawnsFoundersWhenEnabled()
        {
            var objects = new List<GameObject>();
            var vitals = ScriptableObject.CreateInstance<SpeciesVitalsDefinition>();
            var config = ScriptableObject.CreateInstance<SimulationConfig>();
            try
            {
                var root = Create(objects, "RespawnRoot");
                var tracker = root.AddComponent<PopulationTracker>();
                var hub = root.AddComponent<CreatureLifecycleHub>();
                hub.Bind(tracker);
                var spawner = root.AddComponent<CreatureSpawner>();
                config.Ecosystem.Mode = EcosystemMode.TrainingSupport;
                config.Ecosystem.TrainingRespawnEnabled = true;
                config.Ecosystem.MinHerbivores = 1;
                config.Ecosystem.MinPredators = 0;
                var prefab = Create(objects, "RespawnPrefab");
                prefab.AddComponent<CreatureIdentity>();
                prefab.AddComponent<CreatureVitals>();
                prefab.AddComponent<CreatureGenome>();
                spawner.Configure(tracker, hub, vitals: vitals);
                var respawn = root.AddComponent<TrainingRespawnController>();
                respawn.Configure(spawner, tracker, config, prefab, prefab, seed: 3);

                respawn.Tick(1f);

                Assert.AreEqual(1, tracker.HerbivoreCount);
                Assert.AreEqual(1, tracker.Births);
                Assert.AreEqual(0, tracker.PredatorCount);
            }
            finally
            {
                Cleanup(objects, vitals, config);
            }
        }

        [Test]
        public void PersistentEcosystem_DoesNotRespawn()
        {
            var objects = new List<GameObject>();
            var vitals = ScriptableObject.CreateInstance<SpeciesVitalsDefinition>();
            var config = ScriptableObject.CreateInstance<SimulationConfig>();
            try
            {
                var root = Create(objects, "PersistentRoot");
                var tracker = root.AddComponent<PopulationTracker>();
                var hub = root.AddComponent<CreatureLifecycleHub>();
                hub.Bind(tracker);
                var spawner = root.AddComponent<CreatureSpawner>();
                config.Ecosystem.Mode = EcosystemMode.Persistent;
                config.Ecosystem.TrainingRespawnEnabled = true;
                config.Ecosystem.MinHerbivores = 4;
                var prefab = Create(objects, "PersistentPrefab");
                prefab.AddComponent<CreatureIdentity>();
                prefab.AddComponent<CreatureVitals>();
                spawner.Configure(tracker, hub, vitals: vitals);
                var respawn = root.AddComponent<TrainingRespawnController>();
                respawn.Configure(spawner, tracker, config, prefab, prefab);

                respawn.Tick(1f);

                Assert.AreEqual(0, tracker.TotalAlive);
                Assert.AreEqual(0, tracker.Births);
            }
            finally
            {
                Cleanup(objects, vitals, config);
            }
        }

        [Test]
        public void FounderSpawn_UsesConfiguredCountsAndGenerationZero()
        {
            var objects = new List<GameObject>();
            var vitals = ScriptableObject.CreateInstance<SpeciesVitalsDefinition>();
            var config = ScriptableObject.CreateInstance<SimulationConfig>();
            try
            {
                var root = Create(objects, "FounderRoot");
                var tracker = root.AddComponent<PopulationTracker>();
                var hub = root.AddComponent<CreatureLifecycleHub>();
                hub.Bind(tracker);
                var spawner = root.AddComponent<CreatureSpawner>();
                config.SetInitialPopulation(2, 1);
                var prefab = Create(objects, "FounderPrefab");
                prefab.AddComponent<CreatureIdentity>();
                prefab.AddComponent<CreatureVitals>();
                prefab.AddComponent<CreatureGenome>();
                spawner.Configure(tracker, hub, vitals: vitals);

                InitialPopulationSpawner.SpawnFounders(
                    spawner,
                    config,
                    prefab,
                    prefab,
                    Vector3.zero,
                    new System.Random(4));

                Assert.AreEqual(2, tracker.HerbivoreCount);
                Assert.AreEqual(1, tracker.PredatorCount);
                Assert.AreEqual(3, tracker.Births);
            }
            finally
            {
                Cleanup(objects, vitals, config);
            }
        }

        static GameObject Create(List<GameObject> objects, string name)
        {
            var go = new GameObject(name);
            objects.Add(go);
            return go;
        }

        static void Cleanup(List<GameObject> objects, params UnityEngine.Object[] extras)
        {
            var identities = UnityEngine.Object.FindObjectsOfType<CreatureIdentity>();
            for (var i = 0; i < identities.Length; i++)
            {
                if (identities[i] != null)
                {
                    UnityEngine.Object.DestroyImmediate(identities[i].gameObject);
                }
            }

            for (var i = 0; i < objects.Count; i++)
            {
                if (objects[i] != null)
                {
                    UnityEngine.Object.DestroyImmediate(objects[i]);
                }
            }

            for (var i = 0; i < extras.Length; i++)
            {
                if (extras[i] != null)
                {
                    UnityEngine.Object.DestroyImmediate(extras[i]);
                }
            }
        }
    }
}
