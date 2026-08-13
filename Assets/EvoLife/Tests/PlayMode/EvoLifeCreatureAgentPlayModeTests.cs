using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using EvoLife.AI;

namespace EvoLife.Tests
{
    public sealed class EvoLifeCreatureAgentPlayModeTests
    {
        [UnityTest]
        public IEnumerator CompositeObservationSource_WritesSchemaSizedZeroVectorWhenUnbound()
        {
            var source = new CompositeObservationSource(null);
            var buffer = new float[source.ObservationSize];
            source.WriteObservations(buffer);

            Assert.AreEqual(CreatureObservationSchema.Size, source.ObservationSize);
            Assert.AreEqual(0f, buffer[0]);
            Assert.AreEqual(0f, buffer[buffer.Length - 1]);
            yield return null;
        }

#if EVOLIFE_MLAGENTS
        [UnityTest]
        public IEnumerator EvoLifeCreatureAgent_ExposesSchemaSizesWithoutTrainedModel()
        {
            var go = new GameObject("EvoLifeCreatureAgentTest");
            EvoLifeCreatureAgent agent = null;
            try
            {
                agent = go.AddComponent<EvoLifeCreatureAgent>();
                yield return null;
                Assert.NotNull(agent);
                Assert.AreEqual(CreatureObservationSchema.Size, agent.ObservationSize);
                Assert.AreEqual(31, agent.ObservationSize);
                Assert.AreEqual(CreatureActionSchema.ContinuousCount, agent.ActionSize);
                Assert.AreEqual(3, agent.ActionSize);
                Assert.AreEqual(6, agent.DiscreteBranchSize);
                Assert.AreEqual(1, agent.DiscreteBranchCount);
            }
            finally
            {
                Object.Destroy(go);
            }
        }
#endif
    }
}
