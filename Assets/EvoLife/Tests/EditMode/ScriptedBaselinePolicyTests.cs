using NUnit.Framework;
using EvoLife.AI;

namespace EvoLife.Tests
{
    public sealed class ScriptedBaselinePolicyTests
    {
        [Test]
        public void Step_UsesSchemaHungerAndThirstIndices()
        {
            var vitals = new StubVitalState
            {
                Health = 100f,
                Hunger = 80f,
                MaxHunger = 100f,
                Thirst = 90f,
                MaxThirst = 100f,
                Energy = 50f
            };
            var source = new CompositeObservationSource(vitals);
            var executor = new RecordingExecutor();

            new ScriptedBaselinePolicy().Step(source, executor, null, vitals);

            Assert.AreEqual(2, executor.Last.Length);
            Assert.Greater(executor.Last[CreatureActionSchema.IndexMoveX], 0f);
            Assert.Greater(executor.Last[CreatureActionSchema.IndexMoveZ], 0f);
        }

        sealed class RecordingExecutor : IActionExecutor
        {
            public float[] Last { get; private set; } = new float[0];
            public int ActionSize => CreatureActionSchema.ContinuousCount;
            public void ApplyActions(float[] actions) => Last = (float[])actions.Clone();
        }
    }
}
