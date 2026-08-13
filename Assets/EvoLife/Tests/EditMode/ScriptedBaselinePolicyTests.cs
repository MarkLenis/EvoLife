using System;
using NUnit.Framework;
using EvoLife.AI;
using EvoLife.Common;

namespace EvoLife.Tests
{
    public sealed class ScriptedBaselinePolicyTests
    {
        [Test]
        public void Step_HerbivoreThirst_MovesTowardWater()
        {
            var policy = new ScriptedBaselinePolicy(
                ScriptedBaselineSettings.HerbivoreDefaults(),
                CreatureRole.Herbivore,
                seed: 1)
            {
                DeltaTimeOverride = 0.02f
            };
            var executor = new RecordingExecutor();
            var vitals = ComfortableVitals(hunger: 20f, thirst: 90f, energy: 80f);

            policy.Step(
                new ArrayObservationSource(BaselineTestObservations.Herbivore(
                    hunger: 0.20f,
                    thirst: 0.90f,
                    energy: 0.80f,
                    water: true,
                    waterDirX: 1f,
                    waterDirZ: 0f,
                    waterDistance: 0.4f)),
                executor,
                null,
                vitals);

            Assert.AreEqual(BaselineMotive.SeekWater, policy.LastMotive);
            Assert.AreEqual(2, executor.Last.Length);
            Assert.Greater(executor.Last[CreatureActionSchema.IndexMoveX], 0.5f);
        }

        [Test]
        public void Step_ActionValuesRemainLegal()
        {
            var settings = ScriptedBaselineSettings.HerbivoreDefaults();
            settings.SeekMoveScale = 8f;
            settings.WanderMoveScale = 5f;
            var policy = new ScriptedBaselinePolicy(settings, CreatureRole.Herbivore, seed: 2)
            {
                DeltaTimeOverride = 0.02f
            };
            var executor = new RecordingExecutor();

            policy.Step(
                new ArrayObservationSource(BaselineTestObservations.Herbivore(
                    hunger: 0.10f,
                    thirst: 0.10f,
                    energy: 0.90f)),
                executor,
                null,
                ComfortableVitals());

            AssertLegalActions(executor.Last);
        }

        [Test]
        public void Step_SameInputs_AreDeterministic()
        {
            var obs = BaselineTestObservations.Herbivore(hunger: 0.15f, thirst: 0.15f, energy: 0.9f);
            var a = RunOnce(obs, seed: 17);
            var b = RunOnce(obs, seed: 17);
            Assert.AreEqual(a[0], b[0], 0.0001f);
            Assert.AreEqual(a[1], b[1], 0.0001f);
        }

        [Test]
        public void Step_MissingTargetsAndNulls_DoNotThrow()
        {
            var policy = new ScriptedBaselinePolicy();
            Assert.DoesNotThrow(() => policy.Step(null, new RecordingExecutor(), null, null));
            Assert.DoesNotThrow(() => policy.Step(
                new ArrayObservationSource(new float[0]),
                new RecordingExecutor(),
                null,
                ComfortableVitals()));
            Assert.DoesNotThrow(() => policy.Step(
                new ArrayObservationSource(BaselineTestObservations.Herbivore(hunger: 0.9f, food: false)),
                new RecordingExecutor(),
                null,
                ComfortableVitals()));
        }

        [Test]
        public void Step_DoesNotMutateVitalStateDirectly()
        {
            var vitals = ComfortableVitals(hunger: 90f, thirst: 90f, energy: 80f);
            var hunger = vitals.Hunger;
            var thirst = vitals.Thirst;
            var energy = vitals.Energy;
            var health = vitals.Health;
            var interactor = new RecordingInteractor();
            var policy = new ScriptedBaselinePolicy(
                ScriptedBaselineSettings.HerbivoreDefaults(),
                CreatureRole.Herbivore,
                seed: 1,
                interactor)
            {
                DeltaTimeOverride = 0.02f
            };

            policy.Step(
                new ArrayObservationSource(BaselineTestObservations.Herbivore(
                    hunger: 0.90f,
                    thirst: 0.90f,
                    energy: 0.80f,
                    food: true,
                    foodDistance: 0.5f,
                    water: true,
                    waterDistance: 0.5f)),
                new RecordingExecutor(),
                null,
                vitals);

            Assert.AreEqual(hunger, vitals.Hunger);
            Assert.AreEqual(thirst, vitals.Thirst);
            Assert.AreEqual(energy, vitals.Energy);
            Assert.AreEqual(health, vitals.Health);
            Assert.IsFalse(interactor.Ate || interactor.Drank || interactor.Attacked);
        }

        [Test]
        public void Step_RequestsEatThroughInteractor_NotVitalFields()
        {
            var interactor = new RecordingInteractor();
            var vitals = ComfortableVitals(hunger: 90f);
            var policy = new ScriptedBaselinePolicy(
                ScriptedBaselineSettings.HerbivoreDefaults(),
                CreatureRole.Herbivore,
                seed: 1,
                interactor)
            {
                DeltaTimeOverride = 0.02f
            };

            policy.Step(
                new ArrayObservationSource(BaselineTestObservations.Herbivore(
                    hunger: 0.90f,
                    food: true,
                    foodDistance: 0.02f)),
                new RecordingExecutor(),
                null,
                vitals);

            Assert.IsTrue(interactor.Ate);
            Assert.AreEqual(90f, vitals.Hunger);
        }

        [Test]
        public void Step_NullInteractor_DoesNotThrowWhenInRange()
        {
            var policy = new ScriptedBaselinePolicy(
                ScriptedBaselineSettings.PredatorDefaults(),
                CreatureRole.Predator,
                seed: 1)
            {
                DeltaTimeOverride = 0.02f
            };

            Assert.DoesNotThrow(() => policy.Step(
                new ArrayObservationSource(BaselineTestObservations.Predator(
                    hunger: 0.90f,
                    nearby: true,
                    nearbyRole: 0f,
                    nearbyDistance: 0.02f)),
                new RecordingExecutor(),
                null,
                ComfortableVitals(hunger: 90f)));
        }

        [Test]
        public void LocalInteractor_NullWorld_DoesNotThrow()
        {
            var interactor = new LocalCreatureInteractor(null, null, null, null, null, null);
            Assert.IsFalse(interactor.TryEat());
            Assert.IsFalse(interactor.TryDrink());
            Assert.IsFalse(interactor.TryAttack());
            Assert.DoesNotThrow(interactor.SetResting);
        }

        static float[] RunOnce(float[] observations, int seed)
        {
            var policy = new ScriptedBaselinePolicy(
                ScriptedBaselineSettings.HerbivoreDefaults(),
                CreatureRole.Herbivore,
                seed)
            {
                DeltaTimeOverride = 0.02f
            };
            var executor = new RecordingExecutor();
            policy.Step(new ArrayObservationSource(observations), executor, null, ComfortableVitals());
            return executor.Last;
        }

        static StubVitalState ComfortableVitals(float hunger = 10f, float thirst = 10f, float energy = 80f) =>
            new StubVitalState
            {
                Health = 100f,
                Hunger = hunger,
                Thirst = thirst,
                Energy = energy,
                MaxHealth = 100f,
                MaxHunger = 100f,
                MaxThirst = 100f,
                MaxEnergy = 100f,
                IsAlive = true
            };

        static void AssertLegalActions(float[] actions)
        {
            Assert.IsTrue(CreatureActionSchema.IsValid(actions));
            Assert.GreaterOrEqual(actions[0], -1f);
            Assert.LessOrEqual(actions[0], 1f);
            Assert.GreaterOrEqual(actions[1], -1f);
            Assert.LessOrEqual(actions[1], 1f);
        }

        sealed class RecordingExecutor : IActionExecutor
        {
            public float[] Last { get; private set; } = new float[0];
            public int ActionSize => CreatureActionSchema.ContinuousCount;
            public void ApplyActions(float[] actions) => Last = (float[])actions.Clone();
        }

        sealed class RecordingInteractor : ICreatureInteractor
        {
            public bool Ate { get; private set; }
            public bool Drank { get; private set; }
            public bool Attacked { get; private set; }
            public bool Rested { get; private set; }

            public bool TryEat()
            {
                Ate = true;
                return true;
            }

            public bool TryDrink()
            {
                Drank = true;
                return true;
            }

            public bool TryAttack()
            {
                Attacked = true;
                return true;
            }

            public void SetResting() => Rested = true;
        }

        sealed class ArrayObservationSource : IObservationSource
        {
            readonly float[] data;

            public ArrayObservationSource(float[] data) => this.data = data ?? new float[0];

            public int ObservationSize => data.Length;

            public void WriteObservations(float[] buffer)
            {
                if (buffer == null || data.Length == 0)
                {
                    return;
                }

                var count = Math.Min(buffer.Length, data.Length);
                Array.Copy(data, buffer, count);
            }
        }
    }

    static class BaselineTestObservations
    {
        public static float[] Herbivore(
            float health = 1f,
            float hunger = 0.1f,
            float thirst = 0.1f,
            float energy = 0.8f,
            float age = 0.1f,
            bool food = false,
            float foodDirX = 0f,
            float foodDirZ = 1f,
            float foodDistance = 0.4f,
            bool water = false,
            float waterDirX = 0f,
            float waterDirZ = 1f,
            float waterDistance = 0.4f,
            bool nearby = false,
            float nearbyDirX = 0f,
            float nearbyDirZ = 1f,
            float nearbyDistance = 0.4f,
            float nearbyRole = 1f) =>
            Create(
                health,
                hunger,
                thirst,
                energy,
                age,
                role: 0f,
                food,
                foodDirX,
                foodDirZ,
                foodDistance,
                water,
                waterDirX,
                waterDirZ,
                waterDistance,
                nearby,
                nearbyDirX,
                nearbyDirZ,
                nearbyDistance,
                nearbyRole);

        public static float[] Predator(
            float health = 1f,
            float hunger = 0.1f,
            float thirst = 0.1f,
            float energy = 0.8f,
            float age = 0.1f,
            bool food = false,
            float foodDirX = 0f,
            float foodDirZ = 1f,
            float foodDistance = 0.4f,
            bool water = false,
            float waterDirX = 0f,
            float waterDirZ = 1f,
            float waterDistance = 0.4f,
            bool nearby = false,
            float nearbyDirX = 0f,
            float nearbyDirZ = 1f,
            float nearbyDistance = 0.4f,
            float nearbyRole = 0f) =>
            Create(
                health,
                hunger,
                thirst,
                energy,
                age,
                role: 1f,
                food,
                foodDirX,
                foodDirZ,
                foodDistance,
                water,
                waterDirX,
                waterDirZ,
                waterDistance,
                nearby,
                nearbyDirX,
                nearbyDirZ,
                nearbyDistance,
                nearbyRole);

        static float[] Create(
            float health,
            float hunger,
            float thirst,
            float energy,
            float age,
            float role,
            bool food,
            float foodDirX,
            float foodDirZ,
            float foodDistance,
            bool water,
            float waterDirX,
            float waterDirZ,
            float waterDistance,
            bool nearby,
            float nearbyDirX,
            float nearbyDirZ,
            float nearbyDistance,
            float nearbyRole)
        {
            var buffer = new float[CreatureObservationSchema.Size];
            buffer[CreatureObservationSchema.IndexHealth] = health;
            buffer[CreatureObservationSchema.IndexHunger] = hunger;
            buffer[CreatureObservationSchema.IndexThirst] = thirst;
            buffer[CreatureObservationSchema.IndexEnergy] = energy;
            buffer[CreatureObservationSchema.IndexAge] = age;
            buffer[CreatureObservationSchema.IndexRole] = role;
            WriteResource(
                buffer,
                CreatureObservationSchema.IndexFood,
                food,
                foodDirX,
                foodDirZ,
                foodDistance);
            WriteResource(
                buffer,
                CreatureObservationSchema.IndexWater,
                water,
                waterDirX,
                waterDirZ,
                waterDistance);
            if (nearby)
            {
                buffer[CreatureObservationSchema.IndexNearbyCreature + CreatureObservationSchema.OffsetDirX] = nearbyDirX;
                buffer[CreatureObservationSchema.IndexNearbyCreature + CreatureObservationSchema.OffsetDirZ] = nearbyDirZ;
                buffer[CreatureObservationSchema.IndexNearbyCreature + CreatureObservationSchema.OffsetDistance] = nearbyDistance;
                buffer[CreatureObservationSchema.IndexNearbyCreature + CreatureObservationSchema.OffsetCreatureRole] = nearbyRole;
                buffer[CreatureObservationSchema.IndexNearbyCreature + CreatureObservationSchema.OffsetCreaturePresent] = 1f;
            }

            return buffer;
        }

        static void WriteResource(float[] buffer, int index, bool present, float dirX, float dirZ, float distance)
        {
            if (!present)
            {
                return;
            }

            buffer[index + CreatureObservationSchema.OffsetDirX] = dirX;
            buffer[index + CreatureObservationSchema.OffsetDirZ] = dirZ;
            buffer[index + CreatureObservationSchema.OffsetDistance] = distance;
            buffer[index + CreatureObservationSchema.OffsetPresent] = 1f;
        }
    }
}
