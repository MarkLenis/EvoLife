using NUnit.Framework;
using EvoLife.AI;
using EvoLife.Common;

namespace EvoLife.Tests
{
    public sealed class VitalObservationSourceTests
    {
        [Test]
        public void WriteObservations_UsesPerCreatureHungerAndThirstCapacities()
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
                MaxAge = 10f,
                IsAlive = true
            };

            var source = new VitalObservationSource(vitals);
            var buffer = new float[source.ObservationSize];
            source.WriteObservations(buffer);

            Assert.AreEqual(5, source.ObservationSize);
            Assert.AreEqual(0.5f, buffer[0], 0.0001f);
            Assert.AreEqual(0.5f, buffer[1], 0.0001f);
            Assert.AreEqual(0.2f, buffer[2], 0.0001f);
            Assert.AreEqual(0.5f, buffer[3], 0.0001f);
            Assert.AreEqual(0.5f, buffer[4], 0.0001f);
        }

        [Test]
        public void WriteObservations_DoesNotAssumeHundredPointHungerScale()
        {
            var vitals = new StubVitalState
            {
                Hunger = 100f,
                MaxHunger = 250f,
                Thirst = 100f,
                MaxThirst = 400f,
                MaxHealth = 1f,
                MaxEnergy = 1f,
                MaxAge = 1f,
                IsAlive = true
            };

            var buffer = new float[5];
            new VitalObservationSource(vitals).WriteObservations(buffer);

            Assert.AreEqual(0.4f, buffer[1], 0.0001f);
            Assert.AreEqual(0.25f, buffer[2], 0.0001f);
        }

        [Test]
        public void SurvivalReward_UsesVitalCapacitiesNotHardCodedHundred()
        {
            var highCapacity = new StubVitalState
            {
                Hunger = 50f,
                MaxHunger = 200f,
                Thirst = 50f,
                MaxThirst = 200f,
                Energy = 100f,
                MaxEnergy = 100f,
                IsAlive = true
            };
            var assumedHundred = new StubVitalState
            {
                Hunger = 50f,
                MaxHunger = 100f,
                Thirst = 50f,
                MaxThirst = 100f,
                Energy = 100f,
                MaxEnergy = 100f,
                IsAlive = true
            };

            var calculator = new SurvivalRewardCalculator();
            var high = calculator.CalculateReward(highCapacity, episodeEnded: true);
            var baseline = calculator.CalculateReward(assumedHundred, episodeEnded: true);

            Assert.Greater(high, baseline);
        }

        sealed class StubVitalState : IReadOnlyVitalState
        {
            public float Health { get; set; }
            public float MaxHealth { get; set; }
            public float Hunger { get; set; }
            public float MaxHunger { get; set; }
            public float Thirst { get; set; }
            public float MaxThirst { get; set; }
            public float Energy { get; set; }
            public float MaxEnergy { get; set; }
            public float Age { get; set; }
            public float MaxAge { get; set; }
            public bool IsAlive { get; set; }
            public DeathCause? CauseOfDeath { get; set; }
        }
    }
}
