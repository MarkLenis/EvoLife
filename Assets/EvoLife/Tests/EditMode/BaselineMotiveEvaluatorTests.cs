using NUnit.Framework;
using UnityEngine;
using EvoLife.AI;
using EvoLife.Common;

namespace EvoLife.Tests
{
    public sealed class BaselineMotiveEvaluatorTests
    {
        [Test]
        public void Herbivore_SevereThirst_ChoosesWater()
        {
            var decision = EvaluateHerbivore(BaselineTestObservations.Herbivore(
                thirst: 0.92f,
                hunger: 0.20f,
                energy: 0.80f,
                water: true,
                waterDirX: 0.2f,
                waterDirZ: 0.98f,
                food: true));

            Assert.AreEqual(BaselineMotive.SeekWater, decision.Motive);
            Assert.Greater(decision.MoveZ, 0.5f);
            Assert.IsFalse(decision.TryEat);
        }

        [Test]
        public void Herbivore_SevereHunger_ChoosesFood()
        {
            var decision = EvaluateHerbivore(BaselineTestObservations.Herbivore(
                hunger: 0.90f,
                thirst: 0.20f,
                energy: 0.80f,
                food: true,
                foodDirX: -1f,
                foodDirZ: 0f,
                water: true));

            Assert.AreEqual(BaselineMotive.SeekFood, decision.Motive);
            Assert.Less(decision.MoveX, -0.5f);
            Assert.IsFalse(decision.TryDrink);
        }

        [Test]
        public void Herbivore_PredatorThreat_OverridesFoodSeeking()
        {
            var decision = EvaluateHerbivore(BaselineTestObservations.Herbivore(
                hunger: 0.90f,
                thirst: 0.20f,
                energy: 0.80f,
                food: true,
                foodDirZ: 1f,
                nearby: true,
                nearbyRole: 1f,
                nearbyDirX: 1f,
                nearbyDistance: 0.25f));

            Assert.AreEqual(BaselineMotive.Flee, decision.Motive);
            Assert.Less(decision.MoveX, 0f);
        }

        [Test]
        public void Herbivore_LowEnergy_ChoosesRestWhenNoUrgentNeed()
        {
            var decision = EvaluateHerbivore(BaselineTestObservations.Herbivore(
                hunger: 0.20f,
                thirst: 0.20f,
                energy: 0.10f));

            Assert.AreEqual(BaselineMotive.Rest, decision.Motive);
            Assert.AreEqual(0f, decision.MoveX, 0.0001f);
            Assert.AreEqual(0f, decision.MoveZ, 0.0001f);
            Assert.IsTrue(decision.Rest);
        }

        [Test]
        public void Herbivore_NoNeeds_Explores()
        {
            var decision = EvaluateHerbivore(BaselineTestObservations.Herbivore(
                hunger: 0.10f,
                thirst: 0.10f,
                energy: 0.85f));

            Assert.AreEqual(BaselineMotive.Wander, decision.Motive);
            Assert.Greater(MoveMagnitude(decision), 0.1f);
        }

        [Test]
        public void Herbivore_FoodAndWater_DoesNotOscillateEveryStep()
        {
            var evaluator = new BaselineMotiveEvaluator(seed: 3);
            var memory = new BaselineMemory();
            var settings = ScriptedBaselineSettings.HerbivoreDefaults();
            settings.MinMotiveHoldSeconds = 0.4f;
            settings.MotiveStickiness = 0.2f;

            var waterFirst = BaselineSensedWorld.FromObservations(BaselineTestObservations.Herbivore(
                hunger: 0.80f,
                thirst: 0.90f,
                food: true,
                water: true,
                waterDirZ: 1f,
                foodDirX: 1f));
            var foodSlightlyHigher = BaselineSensedWorld.FromObservations(BaselineTestObservations.Herbivore(
                hunger: 0.91f,
                thirst: 0.88f,
                food: true,
                water: true,
                waterDirZ: 1f,
                foodDirX: 1f));

            var first = evaluator.Evaluate(waterFirst, memory, settings, CreatureRole.Herbivore, 0.02f);
            Assert.AreEqual(BaselineMotive.SeekWater, first.Motive);

            var second = evaluator.Evaluate(foodSlightlyHigher, memory, settings, CreatureRole.Herbivore, 0.02f);
            Assert.AreEqual(BaselineMotive.SeekWater, second.Motive);
        }

        [Test]
        public void Herbivore_DepletedFood_DropsSeekFood()
        {
            var evaluator = new BaselineMotiveEvaluator(seed: 4);
            var memory = new BaselineMemory();
            var settings = ScriptedBaselineSettings.HerbivoreDefaults();

            var withFood = BaselineSensedWorld.FromObservations(BaselineTestObservations.Herbivore(
                hunger: 0.90f,
                food: true,
                foodDirZ: 1f));
            evaluator.Evaluate(withFood, memory, settings, CreatureRole.Herbivore, 0.02f);
            Assert.AreEqual(BaselineMotive.SeekFood, memory.CurrentMotive);

            var depleted = BaselineSensedWorld.FromObservations(BaselineTestObservations.Herbivore(
                hunger: 0.90f,
                food: false,
                energy: 0.80f));
            var after = evaluator.Evaluate(depleted, memory, settings, CreatureRole.Herbivore, 0.02f);
            Assert.AreNotEqual(BaselineMotive.SeekFood, after.Motive);
        }

        [Test]
        public void Predator_UrgentThirst_OverridesHunting()
        {
            var decision = EvaluatePredator(BaselineTestObservations.Predator(
                hunger: 0.85f,
                thirst: 0.90f,
                energy: 0.80f,
                water: true,
                waterDirZ: 1f,
                nearby: true,
                nearbyRole: 0f,
                nearbyDirX: 1f,
                nearbyDistance: 0.30f));

            Assert.AreEqual(BaselineMotive.SeekWater, decision.Motive);
            Assert.Greater(decision.MoveZ, 0.5f);
        }

        [Test]
        public void Predator_Hungry_SelectsVisiblePrey()
        {
            var decision = EvaluatePredator(BaselineTestObservations.Predator(
                hunger: 0.80f,
                thirst: 0.20f,
                energy: 0.80f,
                nearby: true,
                nearbyRole: 0f,
                nearbyDirX: 0f,
                nearbyDirZ: 1f,
                nearbyDistance: 0.40f));

            Assert.AreEqual(BaselineMotive.Hunt, decision.Motive);
            Assert.Greater(decision.MoveZ, 0.5f);
        }

        [Test]
        public void Predator_NoPrey_Explores()
        {
            var decision = EvaluatePredator(BaselineTestObservations.Predator(
                hunger: 0.80f,
                thirst: 0.20f,
                energy: 0.80f));

            Assert.AreEqual(BaselineMotive.Wander, decision.Motive);
            Assert.Greater(MoveMagnitude(decision), 0.1f);
        }

        [Test]
        public void Predator_InvalidOrDeadTarget_IsDropped()
        {
            var evaluator = new BaselineMotiveEvaluator(seed: 9);
            var memory = new BaselineMemory();
            var settings = ScriptedBaselineSettings.PredatorDefaults();

            var hunting = BaselineSensedWorld.FromObservations(BaselineTestObservations.Predator(
                hunger: 0.80f,
                nearby: true,
                nearbyRole: 0f,
                nearbyDirZ: 1f,
                nearbyDistance: 0.30f));
            evaluator.Evaluate(hunting, memory, settings, CreatureRole.Predator, 0.02f);
            Assert.AreEqual(BaselineMotive.Hunt, memory.CurrentMotive);

            var gone = BaselineSensedWorld.FromObservations(BaselineTestObservations.Predator(
                hunger: 0.80f,
                nearby: false,
                energy: 0.80f));
            var after = evaluator.Evaluate(gone, memory, settings, CreatureRole.Predator, 0.02f);
            Assert.AreNotEqual(BaselineMotive.Hunt, after.Motive);
            Assert.AreEqual(BaselineMotive.Wander, after.Motive);
        }

        [Test]
        public void Predator_DoesNotHuntNearbyPredator()
        {
            var decision = EvaluatePredator(BaselineTestObservations.Predator(
                hunger: 0.90f,
                nearby: true,
                nearbyRole: 1f,
                nearbyDirZ: 1f,
                nearbyDistance: 0.20f));

            Assert.AreNotEqual(BaselineMotive.Hunt, decision.Motive);
        }

        [Test]
        public void Predator_AbandonsInefficientFarChase()
        {
            var evaluator = new BaselineMotiveEvaluator(seed: 5);
            var memory = new BaselineMemory();
            var settings = ScriptedBaselineSettings.PredatorDefaults();
            settings.ChaseAbandonSeconds = 0.05f;
            settings.ChaseAbandonDistance = 0.90f;
            settings.HuntRetryCooldownSeconds = 1f;

            var farPrey = BaselineSensedWorld.FromObservations(BaselineTestObservations.Predator(
                hunger: 0.80f,
                nearby: true,
                nearbyRole: 0f,
                nearbyDirZ: 1f,
                nearbyDistance: 0.95f));

            evaluator.Evaluate(farPrey, memory, settings, CreatureRole.Predator, 0.02f);
            evaluator.Evaluate(farPrey, memory, settings, CreatureRole.Predator, 0.02f);
            evaluator.Evaluate(farPrey, memory, settings, CreatureRole.Predator, 0.02f);
            var after = evaluator.Evaluate(farPrey, memory, settings, CreatureRole.Predator, 0.02f);

            Assert.AreNotEqual(BaselineMotive.Hunt, after.Motive);
            Assert.AreEqual(BaselineMotive.Wander, after.Motive);
        }

        [Test]
        public void SameInputs_AreDeterministic()
        {
            var obs = BaselineTestObservations.Herbivore(hunger: 0.2f, thirst: 0.2f, energy: 0.9f);
            var world = BaselineSensedWorld.FromObservations(obs);
            var settings = ScriptedBaselineSettings.HerbivoreDefaults();

            var a = new BaselineMotiveEvaluator(11).Evaluate(
                world, new BaselineMemory(), settings, CreatureRole.Herbivore, 0.02f);
            var b = new BaselineMotiveEvaluator(11).Evaluate(
                world, new BaselineMemory(), settings, CreatureRole.Herbivore, 0.02f);

            Assert.AreEqual(a.Motive, b.Motive);
            Assert.AreEqual(a.MoveX, b.MoveX, 0.0001f);
            Assert.AreEqual(a.MoveZ, b.MoveZ, 0.0001f);
        }

        [Test]
        public void StandingOnResource_DoesNotSpin()
        {
            var decision = EvaluateHerbivore(BaselineTestObservations.Herbivore(
                thirst: 0.90f,
                water: true,
                waterDirX: 0f,
                waterDirZ: 0f,
                waterDistance: 0f));

            Assert.AreEqual(BaselineMotive.SeekWater, decision.Motive);
            Assert.AreEqual(0f, decision.MoveX, 0.0001f);
            Assert.AreEqual(0f, decision.MoveZ, 0.0001f);
            Assert.IsTrue(decision.TryDrink);
        }

        [Test]
        public void ShortObservationBuffer_DoesNotThrow()
        {
            var world = BaselineSensedWorld.FromObservations(new[] { 1f, 0.9f, 0.9f });
            Assert.DoesNotThrow(() =>
                new BaselineMotiveEvaluator().Evaluate(
                    world,
                    new BaselineMemory(),
                    ScriptedBaselineSettings.HerbivoreDefaults(),
                    CreatureRole.Herbivore,
                    0.02f));
        }

        static BaselineDecision EvaluateHerbivore(float[] observations)
        {
            return new BaselineMotiveEvaluator(seed: 1).Evaluate(
                BaselineSensedWorld.FromObservations(observations),
                new BaselineMemory(),
                ScriptedBaselineSettings.HerbivoreDefaults(),
                CreatureRole.Herbivore,
                0.02f);
        }

        static BaselineDecision EvaluatePredator(float[] observations)
        {
            return new BaselineMotiveEvaluator(seed: 1).Evaluate(
                BaselineSensedWorld.FromObservations(observations),
                new BaselineMemory(),
                ScriptedBaselineSettings.PredatorDefaults(),
                CreatureRole.Predator,
                0.02f);
        }

        static float MoveMagnitude(BaselineDecision decision) =>
            Mathf.Sqrt(decision.MoveX * decision.MoveX + decision.MoveZ * decision.MoveZ);
    }
}
