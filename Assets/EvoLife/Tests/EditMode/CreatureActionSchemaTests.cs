using NUnit.Framework;
using EvoLife.AI;

namespace EvoLife.Tests
{
    public sealed class CreatureActionSchemaTests
    {
        [Test]
        public void ContinuousCount_MatchesNames()
        {
            Assert.AreEqual(2, CreatureActionSchema.ContinuousCount);
            Assert.AreEqual(CreatureActionSchema.ContinuousCount, CreatureActionSchema.Names.Length);
            Assert.AreEqual("move_x", CreatureActionSchema.Names[0]);
            Assert.AreEqual("move_z", CreatureActionSchema.Names[1]);
        }

        [Test]
        public void ClampTo_ClampsOutOfRangeValues()
        {
            var destination = new float[2];
            CreatureActionSchema.ClampTo(new[] { 4f, -2.5f }, destination);
            Assert.AreEqual(1f, destination[0], 0.0001f);
            Assert.AreEqual(-1f, destination[1], 0.0001f);
        }

        [Test]
        public void ClampTo_NullOrShortSource_WritesZeros()
        {
            var destination = new[] { 9f, 9f };
            CreatureActionSchema.ClampTo(null, destination);
            Assert.AreEqual(0f, destination[0], 0.0001f);
            Assert.AreEqual(0f, destination[1], 0.0001f);

            CreatureActionSchema.ClampTo(new[] { 0.5f }, destination);
            Assert.AreEqual(0f, destination[0], 0.0001f);
            Assert.AreEqual(0f, destination[1], 0.0001f);
        }

        [Test]
        public void ClampTo_InPlace_DoesNotZeroValidValues()
        {
            var actions = new[] { 0.4f, -0.6f };
            CreatureActionSchema.ClampTo(actions, actions);
            Assert.AreEqual(0.4f, actions[0], 0.0001f);
            Assert.AreEqual(-0.6f, actions[1], 0.0001f);
        }

        [Test]
        public void ClampCopy_PreservesInRangeValues()
        {
            var copy = CreatureActionSchema.ClampCopy(new[] { -0.25f, 0.8f, 99f });
            Assert.AreEqual(2, copy.Length);
            Assert.AreEqual(-0.25f, copy[0], 0.0001f);
            Assert.AreEqual(0.8f, copy[1], 0.0001f);
        }

        [Test]
        public void PpoFallback_AppliesIdleActions()
        {
            var executor = new RecordingExecutor();
            new PpoPolicyAdapter().Step(null, executor, null, null);
            Assert.AreEqual(2, executor.Last.Length);
            Assert.AreEqual(0f, executor.Last[0], 0.0001f);
            Assert.AreEqual(0f, executor.Last[1], 0.0001f);
        }

        sealed class RecordingExecutor : IActionExecutor
        {
            public float[] Last { get; private set; } = new float[0];
            public int ActionSize => 2;

            public void ApplyActions(float[] actions) => Last = (float[])actions.Clone();
        }
    }
}
