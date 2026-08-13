using NUnit.Framework;
using UnityEngine;
using EvoLife.AI;
using EvoLife.Common;
using EvoLife.Genetics;

namespace EvoLife.Tests
{
    public sealed class CreatureObservationSchemaTests
    {
        [Test]
        public void Size_MatchesNamesAndDeclaredBlocks()
        {
            Assert.AreEqual(31, CreatureObservationSchema.Size);
            Assert.AreEqual(2, CreatureObservationSchema.Version);
            Assert.AreEqual(CreatureObservationSchema.Size, CreatureObservationSchema.Names.Length);
            Assert.AreEqual(
                CreatureObservationSchema.VitalCount
                + CreatureObservationSchema.RoleCount
                + CreatureObservationSchema.GeneticCount
                + CreatureObservationSchema.ResourceCount
                + CreatureObservationSchema.NearbyCreatureCount,
                CreatureObservationSchema.Size);
        }

        [Test]
        public void GeneticBlock_MatchesCanonicalGenomeSchema()
        {
            Assert.AreEqual(CanonicalGenomeSchema.TraitCount, CreatureObservationSchema.GeneticCount);
            Assert.AreEqual(GeneticObservationProvider.ObservationSize, CreatureObservationSchema.GeneticCount);
            CreatureObservationSchema.ValidateAgainstGenetics();
        }

        [Test]
        public void Indices_AreContiguousAndStable()
        {
            Assert.AreEqual(0, CreatureObservationSchema.IndexHealth);
            Assert.AreEqual(1, CreatureObservationSchema.IndexHunger);
            Assert.AreEqual(2, CreatureObservationSchema.IndexThirst);
            Assert.AreEqual(3, CreatureObservationSchema.IndexEnergy);
            Assert.AreEqual(4, CreatureObservationSchema.IndexAge);
            Assert.AreEqual(5, CreatureObservationSchema.IndexRole);
            Assert.AreEqual(6, CreatureObservationSchema.IndexGenetics);
            Assert.AreEqual(15, CreatureObservationSchema.IndexFood);
            Assert.AreEqual(19, CreatureObservationSchema.IndexWater);
            Assert.AreEqual(23, CreatureObservationSchema.IndexHerbivore);
            Assert.AreEqual(27, CreatureObservationSchema.IndexPredator);
            Assert.AreEqual("health", CreatureObservationSchema.Names[0]);
            Assert.AreEqual("own_role", CreatureObservationSchema.Names[5]);
            Assert.AreEqual("nearest_predator_present", CreatureObservationSchema.Names[CreatureObservationSchema.Size - 1]);
        }

        [Test]
        public void BehaviorNames_AreStableForTrainingYaml()
        {
            Assert.AreEqual("EvoLifeHerbivore", MlAgentsBehaviorNames.Herbivore);
            Assert.AreEqual("EvoLifePredator", MlAgentsBehaviorNames.Predator);
            Assert.AreEqual(MlAgentsBehaviorNames.Herbivore, MlAgentsBehaviorNames.ForRole(CreatureRole.Herbivore));
            Assert.AreEqual(MlAgentsBehaviorNames.Predator, MlAgentsBehaviorNames.ForRole(CreatureRole.Predator));
        }

        [Test]
        public void EvoLifeCreatureAgent_ExposesV2SizesWithoutTrainedModel()
        {
            var go = new GameObject("EvoLifeCreatureAgentEditMode");
            try
            {
                var agent = go.AddComponent<EvoLifeCreatureAgent>();
                Assert.AreEqual(31, agent.ObservationSize);
                Assert.AreEqual(3, agent.ActionSize);
                Assert.AreEqual(6, agent.DiscreteBranchSize);
                Assert.AreEqual(1, agent.DiscreteBranchCount);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void EnvironmentObservationSource_IsNotPartOfPpoSchema()
        {
            var source = new EnvironmentObservationSource();
            Assert.AreEqual(2, source.ObservationSize);
            Assert.AreEqual(31, CreatureObservationSchema.Size);
            Assert.AreNotEqual(CreatureObservationSchema.Size, source.ObservationSize);

            var buffer = new float[source.ObservationSize];
            source.WriteObservations(buffer);
            Assert.AreEqual(0f, buffer[EnvironmentObservationSource.IndexTimeOfDay]);
            Assert.AreEqual(0f, buffer[EnvironmentObservationSource.IndexTemperature]);
        }
    }
}
