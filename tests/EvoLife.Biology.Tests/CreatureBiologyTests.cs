using System;
using EvoLife.Biology;
using NUnit.Framework;

namespace EvoLife.Biology.Tests
{
    [TestFixture]
    public class CreatureBiologyTests
    {
        private static MetabolicRates CreateTestRates() =>
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
            var biology = new CreatureBiology(CreateTestRates());
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

            Assert.That(biology.IsAlive, Is.False);
            Assert.That(deathCount, Is.EqualTo(1));
            Assert.That(cause, Is.EqualTo(DeathCause.Starvation));
            Assert.That(biology.Snapshot.Health, Is.EqualTo(0f));
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

            Assert.That(biology.IsAlive, Is.False);
            Assert.That(cause, Is.EqualTo(DeathCause.Dehydration));
        }

        [Test]
        public void EatingReducesHungerCorrectly()
        {
            var biology = new CreatureBiology(CreateTestRates());
            biology.Tick(2f);
            var hungerBeforeEat = biology.Snapshot.Hunger;

            biology.Eat(15f);

            Assert.That(biology.Snapshot.Hunger, Is.EqualTo(hungerBeforeEat - 15f));
        }

        [Test]
        public void DrinkingReducesThirstCorrectly()
        {
            var biology = new CreatureBiology(CreateTestRates());
            biology.Tick(2f);
            var thirstBeforeDrink = biology.Snapshot.Thirst;

            biology.Drink(12f);

            Assert.That(biology.Snapshot.Thirst, Is.EqualTo(thirstBeforeDrink - 12f));
        }

        [Test]
        public void EnergyCannotExceedValidBounds()
        {
            var biology = new CreatureBiology(CreateTestRates());
            biology.ConsumeEnergy(40f);
            biology.Rest(100f);

            Assert.That(biology.Snapshot.Energy, Is.LessThanOrEqualTo(biology.Snapshot.MaxEnergy));
            Assert.That(biology.Snapshot.Energy, Is.GreaterThanOrEqualTo(0f));

            biology.Rest(1000f);
            Assert.That(biology.Snapshot.Energy, Is.EqualTo(biology.Snapshot.MaxEnergy));
        }

        [Test]
        public void HealthCannotExceedValidBounds()
        {
            var biology = new CreatureBiology(CreateTestRates());
            biology.TakeDamage(30f);
            biology.Heal(1000f);

            Assert.That(biology.Snapshot.Health, Is.EqualTo(biology.Snapshot.MaxHealth));
            Assert.That(biology.Snapshot.Health, Is.GreaterThanOrEqualTo(0f));
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

            Assert.That(deathCount, Is.EqualTo(1));
            Assert.That(biology.CauseOfDeath, Is.EqualTo(DeathCause.Predation));
        }

        [Test]
        public void OldAgeCanCauseDeath()
        {
            var rates = CreateTestRates().With(maxAge: 5f, hungerIncreaseRate: 0f, thirstIncreaseRate: 0f);
            var biology = new CreatureBiology(rates, startingAge: 4.9f);
            DeathCause? cause = null;
            biology.Died += args => cause = args.Cause;

            biology.Tick(1f);

            Assert.That(biology.IsAlive, Is.False);
            Assert.That(cause, Is.EqualTo(DeathCause.OldAge));
        }

        [Test]
        public void StateValuesRemainClamped()
        {
            var biology = new CreatureBiology(CreateTestRates());
            biology.Eat(1000f);
            biology.Drink(1000f);

            Assert.That(biology.Snapshot.Hunger, Is.GreaterThanOrEqualTo(0f));
            Assert.That(biology.Snapshot.Thirst, Is.GreaterThanOrEqualTo(0f));
            Assert.That(biology.Snapshot.Hunger, Is.LessThanOrEqualTo(100f));
            Assert.That(biology.Snapshot.Thirst, Is.LessThanOrEqualTo(100f));
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

            Assert.That(modified.Snapshot.Hunger, Is.GreaterThan(baseline.Snapshot.Hunger));
            Assert.That(modified.Snapshot.MaxEnergy, Is.EqualTo(rates.MaxEnergy * 1.5f));
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

            Assert.That(idle.Snapshot.Energy, Is.EqualTo(99f));
            Assert.That(walking.Snapshot.Energy, Is.EqualTo(97f));
            Assert.That(sprinting.Snapshot.Energy, Is.EqualTo(94f));
        }

        [Test]
        public void RestingDuringTickRecoversEnergy()
        {
            var biology = new CreatureBiology(CreateTestRates().With(hungerIncreaseRate: 0f, thirstIncreaseRate: 0f));
            biology.ConsumeEnergy(20f);

            biology.Tick(2f, ActivityLevel.Resting);

            Assert.That(biology.Snapshot.Energy, Is.EqualTo(88f));
        }

        [Test]
        public void HealthChangedEventFiresOnDamageAndHeal()
        {
            var biology = new CreatureBiology(CreateTestRates());
            var eventCount = 0;
            biology.HealthChanged += args =>
            {
                eventCount++;
                Assert.That(args.MaxHealth, Is.EqualTo(100f));
            };

            biology.TakeDamage(10f);
            biology.Heal(5f);

            Assert.That(eventCount, Is.EqualTo(2));
        }

        [Test]
        public void ICreatureStateViewProvidesReadOnlySnapshot()
        {
            ICreatureStateView view = new CreatureBiology(CreateTestRates());
            var snapshot = view.Snapshot;

            Assert.That(snapshot.IsAlive, Is.True);
            Assert.That(snapshot.Health, Is.EqualTo(snapshot.MaxHealth));
        }

        [Test]
        public void BiologySimulationClockUsesFixedSubsteps()
        {
            var biology = new CreatureBiology(CreateTestRates().With(hungerIncreaseRate: 1f, thirstIncreaseRate: 0f));
            var clock = new BiologySimulationClock(fixedDeltaTime: 0.5f);
            var debt = 0f;

            clock.AccumulateAndStep(biology, ref debt, deltaTime: 1.25f, ActivityLevel.Idle);

            Assert.That(biology.Snapshot.Hunger, Is.EqualTo(1f).Within(0.001f));
            Assert.That(debt, Is.EqualTo(0.25f).Within(0.001f));
        }
    }
}
