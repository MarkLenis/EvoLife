using NUnit.Framework;
using EvoLife.Common;
using EvoLife.Creatures;

namespace EvoLife.Tests
{
    public sealed class CreatureBiologyTests
    {
        static MetabolicRates CreateTestRates() =>
            new MetabolicRates(
                maxHealth: 100f,
                maxEnergy: 100f,
                maxAge: 10f,
                hungerIncreaseRate: 10f,
                thirstIncreaseRate: 10f,
                passiveEnergyConsumption: 1f,
                walkingEnergyConsumption: 2f,
                sprintingEnergyConsumption: 5f,
                attackEnergyConsumption: 7f,
                restingRecovery: 4f,
                starvationDamage: 20f,
                dehydrationDamage: 25f,
                hungerCapacity: 100f,
                thirstCapacity: 100f,
                starvationThreshold: 80f,
                dehydrationThreshold: 80f);

        [Test]
        public void StarvationEventuallyCausesDamageAndDeath()
        {
            var biology = new CreatureBiology(CreateTestRates().With(maxAge: 1000f));
            var deathCount = 0;
            DeathCause? cause = null;

            biology.Died += args =>
            {
                deathCount++;
                cause = args.Cause;
            };

            for (var i = 0; i < 100 && biology.IsAlive; i++)
            {
                biology.Tick(1f);
            }

            Assert.IsFalse(biology.IsAlive);
            Assert.AreEqual(1, deathCount);
            Assert.AreEqual(DeathCause.Starvation, cause);
            Assert.AreEqual(0f, biology.Snapshot.Health);
        }

        [Test]
        public void DehydrationCausesDamageAndDeath()
        {
            var rates = CreateTestRates().With(hungerIncreaseRate: 0f, starvationDamage: 0f, maxAge: 1000f);
            var biology = new CreatureBiology(rates);
            DeathCause? cause = null;
            biology.Died += args => cause = args.Cause;

            for (var i = 0; i < 100 && biology.IsAlive; i++)
            {
                biology.Tick(1f);
            }

            Assert.IsFalse(biology.IsAlive);
            Assert.AreEqual(DeathCause.Dehydration, cause);
        }

        [Test]
        public void EatingReducesHungerCorrectly()
        {
            var biology = new CreatureBiology(CreateTestRates());
            biology.Tick(2f);
            var hungerBeforeEat = biology.Snapshot.Hunger;

            biology.Eat(15f);

            Assert.AreEqual(hungerBeforeEat - 15f, biology.Snapshot.Hunger);
        }

        [Test]
        public void DrinkingReducesThirstCorrectly()
        {
            var biology = new CreatureBiology(CreateTestRates());
            biology.Tick(2f);
            var thirstBeforeDrink = biology.Snapshot.Thirst;

            biology.Drink(12f);

            Assert.AreEqual(thirstBeforeDrink - 12f, biology.Snapshot.Thirst);
        }

        [Test]
        public void EnergyCannotExceedValidBounds()
        {
            var biology = new CreatureBiology(CreateTestRates());
            biology.ConsumeEnergy(40f);
            biology.GainEnergy(1000f);

            Assert.LessOrEqual(biology.Snapshot.Energy, biology.Snapshot.MaxEnergy);
            Assert.GreaterOrEqual(biology.Snapshot.Energy, 0f);
            Assert.AreEqual(biology.Snapshot.MaxEnergy, biology.Snapshot.Energy);
        }

        [Test]
        public void HealthCannotExceedValidBounds()
        {
            var biology = new CreatureBiology(CreateTestRates());
            biology.TakeDamage(30f);
            biology.Heal(1000f);

            Assert.AreEqual(biology.Snapshot.MaxHealth, biology.Snapshot.Health);
            Assert.GreaterOrEqual(biology.Snapshot.Health, 0f);
        }

        [Test]
        public void DeathOccursOnce()
        {
            var biology = new CreatureBiology(CreateTestRates());
            var deathCount = 0;
            biology.Died += _ => deathCount++;

            biology.Die(DeathCause.Predation);
            biology.Die(DeathCause.Starvation);
            biology.TakeDamage(50f);
            biology.Tick(5f);

            Assert.AreEqual(1, deathCount);
            Assert.AreEqual(DeathCause.Predation, biology.CauseOfDeath);
        }

        [Test]
        public void OldAgeCanCauseDeath()
        {
            var rates = CreateTestRates().With(maxAge: 5f, hungerIncreaseRate: 0f, thirstIncreaseRate: 0f);
            var biology = new CreatureBiology(rates, startingAge: 4.9f);
            DeathCause? cause = null;
            biology.Died += args => cause = args.Cause;

            biology.Tick(1f);

            Assert.IsFalse(biology.IsAlive);
            Assert.AreEqual(DeathCause.OldAge, cause);
        }

        [Test]
        public void StateValuesRemainClamped()
        {
            var biology = new CreatureBiology(CreateTestRates());
            biology.Eat(1000f);
            biology.Drink(1000f);

            Assert.GreaterOrEqual(biology.Snapshot.Hunger, 0f);
            Assert.GreaterOrEqual(biology.Snapshot.Thirst, 0f);
            Assert.LessOrEqual(biology.Snapshot.Hunger, 100f);
            Assert.LessOrEqual(biology.Snapshot.Thirst, 100f);
        }

        [Test]
        public void MetabolicModifiersBehavePredictably()
        {
            var rates = CreateTestRates().With(hungerIncreaseRate: 2f, thirstIncreaseRate: 0f);
            var baseline = new CreatureBiology(rates);
            var modified = new CreatureBiology(
                rates,
                new MetabolicModifiers(hungerRateMultiplier: 2f, maxEnergyMultiplier: 1.5f));

            baseline.Tick(1f);
            modified.Tick(1f);

            Assert.Greater(modified.Snapshot.Hunger, baseline.Snapshot.Hunger);
            Assert.AreEqual(rates.MaxEnergy * 1.5f, modified.Snapshot.MaxEnergy);
        }

        [Test]
        public void TickAppliesActivitySpecificEnergyConsumption()
        {
            var rates = CreateTestRates().With(
                hungerIncreaseRate: 0f,
                thirstIncreaseRate: 0f,
                passiveEnergyConsumption: 1f,
                walkingEnergyConsumption: 2f,
                sprintingEnergyConsumption: 5f);

            var idle = new CreatureBiology(rates);
            var walking = new CreatureBiology(rates);
            var sprinting = new CreatureBiology(rates);

            idle.Tick(1f, ActivityLevel.Idle);
            walking.Tick(1f, ActivityLevel.Walking);
            sprinting.Tick(1f, ActivityLevel.Sprinting);

            Assert.AreEqual(99f, idle.Snapshot.Energy);
            Assert.AreEqual(97f, walking.Snapshot.Energy);
            Assert.AreEqual(94f, sprinting.Snapshot.Energy);
        }

        [Test]
        public void RestingDuringTickRecoversEnergy()
        {
            var biology = new CreatureBiology(CreateTestRates().With(hungerIncreaseRate: 0f, thirstIncreaseRate: 0f));
            biology.ConsumeEnergy(20f);

            biology.Tick(2f, ActivityLevel.Resting);

            Assert.AreEqual(88f, biology.Snapshot.Energy);
        }

        [Test]
        public void BiologySimulationClockUsesFixedSubsteps()
        {
            var biology = new CreatureBiology(CreateTestRates().With(hungerIncreaseRate: 1f, thirstIncreaseRate: 0f));
            var clock = new BiologySimulationClock(fixedDeltaTime: 0.5f);
            var debt = 0f;

            clock.AccumulateAndStep(biology, ref debt, deltaTime: 1.25f, ActivityLevel.Idle);

            Assert.AreEqual(1f, biology.Snapshot.Hunger, 0.001f);
            Assert.AreEqual(0.25f, debt, 0.001f);
        }
    }
}
