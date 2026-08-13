using System;
using System.Reflection;
using NUnit.Framework;
using EvoLife.AI;
using EvoLife.Common;

namespace EvoLife.Tests
{
    public sealed class ScriptedBaselinePolicyTests
    {
        [Test]
        public void Step_HerbivoreThirst_TurnsTowardLocalWater()
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
            Assert.AreEqual(3, executor.Last.Length);
            Assert.Greater(executor.Last[CreatureActionSchema.IndexTurn], 0.5f);
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
            Assert.AreEqual(a[2], b[2], 0.0001f);
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
            var executor = new RecordingExecutor();
            var policy = new ScriptedBaselinePolicy(
                ScriptedBaselineSettings.HerbivoreDefaults(),
                CreatureRole.Herbivore,
                seed: 1)
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
                executor,
                null,
                vitals);

            Assert.AreEqual(hunger, vitals.Hunger);
            Assert.AreEqual(thirst, vitals.Thirst);
            Assert.AreEqual(energy, vitals.Energy);
            Assert.AreEqual(health, vitals.Health);
            Assert.AreEqual(CreatureActionSchema.InteractionNone, executor.LastInteraction);
        }

        [Test]
        public void Step_RequestsEatThroughCanonicalActionPath()
        {
            var vitals = ComfortableVitals(hunger: 90f);
            var executor = new RecordingExecutor();
            var policy = new ScriptedBaselinePolicy(
                ScriptedBaselineSettings.HerbivoreDefaults(),
                CreatureRole.Herbivore,
                seed: 1)
            {
                DeltaTimeOverride = 0.02f
            };

            policy.Step(
                new ArrayObservationSource(BaselineTestObservations.Herbivore(
                    hunger: 0.90f,
                    food: true,
                    foodDistance: 0.02f)),
                executor,
                null,
                vitals);

            Assert.AreEqual(CreatureActionSchema.InteractionEat, executor.LastInteraction);
            Assert.AreEqual(CreatureActionSchema.InteractionEat, policy.LastInteraction);
            Assert.AreEqual(90f, vitals.Hunger);
        }

        [Test]
        public void Step_RequestsDrinkAttackAndRestThroughCanonicalActionPath()
        {
            var drinkExecutor = new RecordingExecutor();
            new ScriptedBaselinePolicy(ScriptedBaselineSettings.HerbivoreDefaults(), CreatureRole.Herbivore, 1)
            {
                DeltaTimeOverride = 0.02f
            }.Step(
                new ArrayObservationSource(BaselineTestObservations.Herbivore(
                    thirst: 0.90f,
                    water: true,
                    waterDistance: 0.02f)),
                drinkExecutor,
                null,
                ComfortableVitals(thirst: 90f));
            Assert.AreEqual(CreatureActionSchema.InteractionDrink, drinkExecutor.LastInteraction);

            var attackExecutor = new RecordingExecutor();
            new ScriptedBaselinePolicy(ScriptedBaselineSettings.PredatorDefaults(), CreatureRole.Predator, 1)
            {
                DeltaTimeOverride = 0.02f
            }.Step(
                new ArrayObservationSource(BaselineTestObservations.Predator(
                    hunger: 0.90f,
                    nearby: true,
                    nearbyRole: 0f,
                    nearbyDistance: 0.02f)),
                attackExecutor,
                null,
                ComfortableVitals(hunger: 90f));
            Assert.AreEqual(CreatureActionSchema.InteractionAttack, attackExecutor.LastInteraction);

            var restExecutor = new RecordingExecutor();
            new ScriptedBaselinePolicy(ScriptedBaselineSettings.HerbivoreDefaults(), CreatureRole.Herbivore, 1)
            {
                DeltaTimeOverride = 0.02f
            }.Step(
                new ArrayObservationSource(BaselineTestObservations.Herbivore(energy: 0.05f)),
                restExecutor,
                null,
                ComfortableVitals(energy: 5f));
            Assert.AreEqual(CreatureActionSchema.InteractionRest, restExecutor.LastInteraction);
        }

        [Test]
        public void Policy_HasNoPrivilegedInteractorField()
        {
            var fields = typeof(ScriptedBaselinePolicy).GetFields(
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            for (var i = 0; i < fields.Length; i++)
            {
                Assert.IsFalse(
                    typeof(ICreatureInteractor).IsAssignableFrom(fields[i].FieldType),
                    "ScriptedBaselinePolicy must not keep a privileged ICreatureInteractor.");
            }
        }

        [Test]
        public void Step_NullExecutorTargets_DoNotThrowWhenInRange()
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
            Assert.DoesNotThrow(interactor.RequestReproduce);
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
            Assert.GreaterOrEqual(actions[2], 0f);
            Assert.LessOrEqual(actions[2], 1f);
        }

        sealed class RecordingExecutor : IActionExecutor
        {
            public float[] Last { get; private set; } = new float[0];
            public int LastInteraction { get; private set; }
            public int ActionSize => CreatureActionSchema.ContinuousCount;

            public void ApplyActions(float[] actions) =>
                ApplyActions(actions, CreatureActionSchema.InteractionNone);

            public void ApplyActions(float[] continuousActions, int interaction)
            {
                Last = continuousActions != null ? (float[])continuousActions.Clone() : new float[0];
                LastInteraction = interaction;
            }
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

        public static float[] BothRoles(
            float hunger,
            float energy,
            bool herbivore,
            float herbivoreDirX,
            float herbivoreDirZ,
            float herbivoreDistance,
            bool predator,
            float predatorDirX,
            float predatorDirZ,
            float predatorDistance)
        {
            var buffer = Create(
                1f, hunger, 0.1f, energy, 0.1f, 0f,
                false, 0f, 1f, 0.4f,
                false, 0f, 1f, 0.4f,
                false, 0f, 1f, 0.4f, 1f);
            if (herbivore)
            {
                WriteResource(
                    buffer,
                    CreatureObservationSchema.IndexHerbivore,
                    true,
                    herbivoreDirX,
                    herbivoreDirZ,
                    herbivoreDistance);
            }

            if (predator)
            {
                WriteResource(
                    buffer,
                    CreatureObservationSchema.IndexPredator,
                    true,
                    predatorDirX,
                    predatorDirZ,
                    predatorDistance);
            }

            return buffer;
        }

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
                var offset = nearbyRole >= 0.5f
                    ? CreatureObservationSchema.IndexPredator
                    : CreatureObservationSchema.IndexHerbivore;
                WriteResource(buffer, offset, true, nearbyDirX, nearbyDirZ, nearbyDistance);
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
