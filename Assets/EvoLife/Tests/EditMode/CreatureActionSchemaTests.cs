using NUnit.Framework;
using UnityEngine;
using EvoLife.AI;

namespace EvoLife.Tests
{
    public sealed class CreatureActionSchemaTests
    {
        [Test]
        public void ContinuousCount_MatchesNames()
        {
            Assert.AreEqual(2, CreatureActionSchema.Version);
            Assert.AreEqual(3, CreatureActionSchema.ContinuousCount);
            Assert.AreEqual(1, CreatureActionSchema.DiscreteBranchCount);
            Assert.AreEqual(6, CreatureActionSchema.InteractionBranchSize);
            Assert.AreEqual(CreatureActionSchema.ContinuousCount, CreatureActionSchema.Names.Length);
            Assert.AreEqual("forward", CreatureActionSchema.Names[0]);
            Assert.AreEqual("turn", CreatureActionSchema.Names[1]);
            Assert.AreEqual("sprint_or_effort", CreatureActionSchema.Names[2]);
        }

        [Test]
        public void InteractionBranch_HasStableMapping()
        {
            Assert.AreEqual(6, CreatureActionSchema.InteractionNames.Length);
            Assert.AreEqual("none", CreatureActionSchema.InteractionNames[CreatureActionSchema.InteractionNone]);
            Assert.AreEqual("eat", CreatureActionSchema.InteractionNames[CreatureActionSchema.InteractionEat]);
            Assert.AreEqual("drink", CreatureActionSchema.InteractionNames[CreatureActionSchema.InteractionDrink]);
            Assert.AreEqual("attack", CreatureActionSchema.InteractionNames[CreatureActionSchema.InteractionAttack]);
            Assert.AreEqual("rest", CreatureActionSchema.InteractionNames[CreatureActionSchema.InteractionRest]);
            Assert.AreEqual("reproduce_request", CreatureActionSchema.InteractionNames[CreatureActionSchema.InteractionReproduceRequest]);
        }

        [Test]
        public void ClampTo_ClampsOutOfRangeValues()
        {
            var destination = new float[3];
            CreatureActionSchema.ClampTo(new[] { 4f, -2.5f, 2f }, destination);
            Assert.AreEqual(1f, destination[0], 0.0001f);
            Assert.AreEqual(-1f, destination[1], 0.0001f);
            Assert.AreEqual(1f, destination[2], 0.0001f);
        }

        [Test]
        public void ClampTo_SprintIsClampedToUnitRange()
        {
            var destination = new float[3];
            CreatureActionSchema.ClampTo(new[] { 0f, 0f, -0.4f }, destination);
            Assert.AreEqual(0f, destination[2], 0.0001f);
        }

        [Test]
        public void ClampTo_NullOrShortSource_WritesZeros()
        {
            var destination = new[] { 9f, 9f, 9f };
            CreatureActionSchema.ClampTo(null, destination);
            Assert.AreEqual(0f, destination[0], 0.0001f);
            Assert.AreEqual(0f, destination[1], 0.0001f);
            Assert.AreEqual(0f, destination[2], 0.0001f);

            CreatureActionSchema.ClampTo(new[] { 0.5f }, destination);
            Assert.AreEqual(0f, destination[0], 0.0001f);
            Assert.AreEqual(0f, destination[1], 0.0001f);
            Assert.AreEqual(0f, destination[2], 0.0001f);
        }

        [Test]
        public void ClampTo_InPlace_DoesNotZeroValidValues()
        {
            var actions = new[] { 0.4f, -0.6f, 0.25f };
            CreatureActionSchema.ClampTo(actions, actions);
            Assert.AreEqual(0.4f, actions[0], 0.0001f);
            Assert.AreEqual(-0.6f, actions[1], 0.0001f);
            Assert.AreEqual(0.25f, actions[2], 0.0001f);
        }

        [Test]
        public void ClampCopy_PreservesInRangeValues()
        {
            var copy = CreatureActionSchema.ClampCopy(new[] { -0.25f, 0.8f, 0.5f, 99f });
            Assert.AreEqual(3, copy.Length);
            Assert.AreEqual(-0.25f, copy[0], 0.0001f);
            Assert.AreEqual(0.8f, copy[1], 0.0001f);
            Assert.AreEqual(0.5f, copy[2], 0.0001f);
        }

        [Test]
        public void ClampInteraction_InvalidValuesAreNone()
        {
            Assert.AreEqual(CreatureActionSchema.InteractionNone, CreatureActionSchema.ClampInteraction(-1));
            Assert.AreEqual(CreatureActionSchema.InteractionNone, CreatureActionSchema.ClampInteraction(99));
            Assert.AreEqual(CreatureActionSchema.InteractionEat, CreatureActionSchema.ClampInteraction(1));
        }

        [Test]
        public void PpoFallback_AppliesIdleActions()
        {
            var executor = new RecordingExecutor();
            new PpoPolicyAdapter().Step(null, executor, null, null);
            Assert.AreEqual(3, executor.Last.Length);
            Assert.AreEqual(0f, executor.Last[0], 0.0001f);
            Assert.AreEqual(0f, executor.Last[1], 0.0001f);
            Assert.AreEqual(0f, executor.Last[2], 0.0001f);
            Assert.AreEqual(CreatureActionSchema.InteractionNone, executor.LastInteraction);
        }

        [Test]
        public void LocalLocomotion_MapsForwardToLocalZOnly()
        {
            LocalLocomotionMath.Evaluate(1f, 0f, 0f, 4f, 8f, 180f, out var speed, out var yaw);
            Assert.AreEqual(4f, speed, 0.0001f);
            Assert.AreEqual(0f, yaw, 0.0001f);
            var local = LocalLocomotionMath.LocalDisplacementPerSecond(speed);
            Assert.AreEqual(0f, local.x, 0.0001f);
            Assert.AreEqual(0f, local.y, 0.0001f);
            Assert.AreEqual(4f, local.z, 0.0001f);
        }

        [Test]
        public void LocalLocomotion_TurnIsYawAndSprintInterpolatesSpeed()
        {
            LocalLocomotionMath.Evaluate(1f, -1f, 1f, 4f, 8f, 180f, out var speed, out var yaw);
            Assert.AreEqual(8f, speed, 0.0001f);
            Assert.AreEqual(-180f, yaw, 0.0001f);
        }

        [Test]
        public void InvalidInteraction_IsSafeNoOp()
        {
            var interactor = new RecordingInteractor();
            Assert.IsFalse(CreatureActionExecution.TryApplyInteraction(interactor, 99));
            Assert.IsFalse(interactor.Ate);
            Assert.IsFalse(interactor.Drank);
            Assert.IsFalse(interactor.Attacked);
            Assert.IsFalse(interactor.Rested);
            Assert.IsFalse(interactor.Reproduced);
            Assert.IsFalse(CreatureActionExecution.TryApplyInteraction(null, CreatureActionSchema.InteractionEat));
        }

        [Test]
        public void ReproduceRequest_WithoutHandler_IsNoOp()
        {
            var interactor = new LocalCreatureInteractor(null, null, null, null, null, null);
            Assert.DoesNotThrow(interactor.RequestReproduce);
            Assert.IsTrue(CreatureActionExecution.TryApplyInteraction(interactor, CreatureActionSchema.InteractionReproduceRequest));
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

        sealed class RecordingInteractor : ICreatureInteractor
        {
            public bool Ate { get; private set; }
            public bool Drank { get; private set; }
            public bool Attacked { get; private set; }
            public bool Rested { get; private set; }
            public bool Reproduced { get; private set; }

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

            public void RequestReproduce() => Reproduced = true;
        }
    }
}
