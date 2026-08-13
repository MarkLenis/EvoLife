using NUnit.Framework;
using UnityEngine;
using EvoLife.AI;
using EvoLife.Common;
using EvoLife.Environment;
using EvoLife.Genetics;

namespace EvoLife.Tests
{
    public sealed class CompositeObservationSourceTests
    {
        [Test]
        public void ObservationSize_IsSchemaSize()
        {
            var source = new CompositeObservationSource(new StubVitalState());
            Assert.AreEqual(CreatureObservationSchema.Size, source.ObservationSize);
        }

        [Test]
        public void WriteObservations_UsesDeterministicVitalAndRoleOrder()
        {
            var vitals = new StubVitalState
            {
                Health = 50f,
                MaxHealth = 100f,
                Hunger = 25f,
                MaxHunger = 50f,
                Thirst = 40f,
                MaxThirst = 200f,
                Energy = 10f,
                MaxEnergy = 20f,
                Age = 5f,
                MaxAge = 10f
            };
            var identity = new StubIdentity { Role = CreatureRole.Predator };
            var source = new CompositeObservationSource(vitals, identity);
            var buffer = new float[source.ObservationSize];
            source.WriteObservations(buffer);

            Assert.AreEqual(0.5f, buffer[CreatureObservationSchema.IndexHealth], 0.0001f);
            Assert.AreEqual(0.5f, buffer[CreatureObservationSchema.IndexHunger], 0.0001f);
            Assert.AreEqual(0.2f, buffer[CreatureObservationSchema.IndexThirst], 0.0001f);
            Assert.AreEqual(0.5f, buffer[CreatureObservationSchema.IndexEnergy], 0.0001f);
            Assert.AreEqual(0.5f, buffer[CreatureObservationSchema.IndexAge], 0.0001f);
            Assert.AreEqual(1f, buffer[CreatureObservationSchema.IndexRole], 0.0001f);
        }

        [Test]
        public void WriteObservations_NormalizesVitalsIntoUnitRange()
        {
            var vitals = new StubVitalState
            {
                Health = 150f,
                MaxHealth = 100f,
                Hunger = -10f,
                MaxHunger = 50f,
                Thirst = 40f,
                MaxThirst = 200f,
                Energy = 0f,
                MaxEnergy = 0f,
                Age = 5f,
                MaxAge = 10f
            };
            var buffer = new float[CreatureObservationSchema.Size];
            new CompositeObservationSource(vitals).WriteObservations(buffer);

            Assert.AreEqual(1f, buffer[CreatureObservationSchema.IndexHealth], 0.0001f);
            Assert.AreEqual(0f, buffer[CreatureObservationSchema.IndexHunger], 0.0001f);
            Assert.AreEqual(0.2f, buffer[CreatureObservationSchema.IndexThirst], 0.0001f);
            Assert.AreEqual(0f, buffer[CreatureObservationSchema.IndexEnergy], 0.0001f);
        }

        [Test]
        public void WriteObservations_CopiesGeneticsInCanonicalOrder()
        {
            var genome = Genome.CreateDefault();
            var expected = GeneticObservationProvider.GetObservationVector(genome);
            var buffer = new float[CreatureObservationSchema.Size];
            new CompositeObservationSource(new StubVitalState(), genome: genome).WriteObservations(buffer);

            for (var i = 0; i < expected.Length; i++)
            {
                Assert.AreEqual(expected[i], buffer[CreatureObservationSchema.IndexGenetics + i], 0.0001f);
            }
        }

        [Test]
        public void WriteObservations_NullOptionalSensors_WriteZeros()
        {
            var buffer = new float[CreatureObservationSchema.Size];
            for (var i = 0; i < buffer.Length; i++)
            {
                buffer[i] = 99f;
            }

            new CompositeObservationSource(null).WriteObservations(buffer);

            for (var i = 0; i < CreatureObservationSchema.Size; i++)
            {
                Assert.AreEqual(0f, buffer[i], 0.0001f);
            }
        }

        [Test]
        public void ResourceSensor_WritesLocalDirectionDistanceAndPresence()
        {
            IResourceNode food = new StubResource
            {
                Kind = ResourceKind.Plant,
                Position = new Vector3(3f, 0f, 0f),
                AvailableAmount = 5f
            };

            var sensor = new ResourceRegistryProximitySensor(
                (origin, kind, range) => kind == ResourceKind.Plant ? food : null,
                () => Vector3.zero,
                () => 10f,
                () => Quaternion.identity);

            var buffer = new float[CreatureObservationSchema.Size];
            new CompositeObservationSource(
                new StubVitalState(),
                resourceSensor: sensor).WriteObservations(buffer);

            Assert.AreEqual(1f, buffer[CreatureObservationSchema.IndexFood + CreatureObservationSchema.OffsetDirX], 0.0001f);
            Assert.AreEqual(0f, buffer[CreatureObservationSchema.IndexFood + CreatureObservationSchema.OffsetDirZ], 0.0001f);
            Assert.AreEqual(0.3f, buffer[CreatureObservationSchema.IndexFood + CreatureObservationSchema.OffsetDistance], 0.0001f);
            Assert.AreEqual(1f, buffer[CreatureObservationSchema.IndexFood + CreatureObservationSchema.OffsetPresent], 0.0001f);
            Assert.AreEqual(0f, buffer[CreatureObservationSchema.IndexWater + CreatureObservationSchema.OffsetPresent], 0.0001f);
        }

        [Test]
        public void MissingResourceQuery_WritesZeroFoodAndWaterBlocks()
        {
            var sensor = new ResourceRegistryProximitySensor(
                findNearest: null,
                origin: () => Vector3.zero,
                senseRange: () => 12f);
            var buffer = new float[CreatureObservationSchema.Size];
            buffer[CreatureObservationSchema.IndexFood] = 7f;
            new CompositeObservationSource(new StubVitalState(), resourceSensor: sensor).WriteObservations(buffer);

            for (var i = 0; i < CreatureObservationSchema.ResourceCount; i++)
            {
                Assert.AreEqual(0f, buffer[CreatureObservationSchema.IndexFood + i], 0.0001f);
            }
        }

        [Test]
        public void NearbyCreatureSensor_AbsentTarget_WritesZeros()
        {
            var sensor = new StaticCreatureProximitySensor(
                () => Vector3.zero,
                () => 12f,
                () => null,
                () => null);
            var buffer = new float[CreatureObservationSchema.Size];
            new CompositeObservationSource(new StubVitalState(), creatureSensor: sensor).WriteObservations(buffer);

            for (var i = 0; i < CreatureObservationSchema.NearbyCreatureCount; i++)
            {
                Assert.AreEqual(0f, buffer[CreatureObservationSchema.IndexNearbyCreature + i], 0.0001f);
            }
        }

        [Test]
        public void ObservationValues_StayInDocumentedRanges()
        {
            var genome = Genome.CreateDefault();
            var sensor = new ResourceRegistryProximitySensor(
                (origin, kind, range) => new StubResource
                {
                    Kind = kind,
                    Position = new Vector3(4f, 1f, -3f)
                },
                () => Vector3.zero,
                () => 10f);
            var creature = new StaticCreatureProximitySensor(
                () => Vector3.zero,
                () => 10f,
                () => new Vector3(-2f, 0f, 2f),
                () => CreatureRole.Herbivore);

            var buffer = new float[CreatureObservationSchema.Size];
            new CompositeObservationSource(
                new StubVitalState { Health = 10f, Hunger = 20f, Thirst = 30f, Energy = 40f, Age = 50f },
                new StubIdentity { Role = CreatureRole.Herbivore },
                genome,
                sensor,
                creature).WriteObservations(buffer);

            AssertUnitRange(buffer, CreatureObservationSchema.IndexHealth, CreatureObservationSchema.IndexRole);
            AssertSignedDirection(buffer[CreatureObservationSchema.IndexFood]);
            AssertSignedDirection(buffer[CreatureObservationSchema.IndexFood + 1]);
            AssertUnitRange(buffer, CreatureObservationSchema.IndexFood + 2, CreatureObservationSchema.IndexFood + 3);
            AssertSignedDirection(buffer[CreatureObservationSchema.IndexNearbyCreature]);
            AssertSignedDirection(buffer[CreatureObservationSchema.IndexNearbyCreature + 1]);
        }

        static void AssertUnitRange(float[] buffer, int start, int end)
        {
            for (var i = start; i <= end; i++)
            {
                Assert.GreaterOrEqual(buffer[i], 0f);
                Assert.LessOrEqual(buffer[i], 1f);
            }
        }

        static void AssertSignedDirection(float value)
        {
            Assert.GreaterOrEqual(value, -1f);
            Assert.LessOrEqual(value, 1f);
        }

        sealed class StubResource : IResourceNode
        {
            public ResourceKind Kind { get; set; }
            public Vector3 Position { get; set; }
            public float AvailableAmount { get; set; } = 1f;
            public bool IsDepleted { get; set; }
            public float TryConsume(float requestedAmount) => 0f;
        }
    }
}
