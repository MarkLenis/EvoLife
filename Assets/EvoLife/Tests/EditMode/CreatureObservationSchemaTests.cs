using NUnit.Framework;
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
            Assert.AreEqual(28, CreatureObservationSchema.Size);
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
            Assert.AreEqual(23, CreatureObservationSchema.IndexNearbyCreature);
            Assert.AreEqual("health", CreatureObservationSchema.Names[0]);
            Assert.AreEqual("nearby_present", CreatureObservationSchema.Names[CreatureObservationSchema.Size - 1]);
        }

        [Test]
        public void BehaviorNames_AreStableForTrainingYaml()
        {
            Assert.AreEqual("EvoLifeHerbivore", MlAgentsBehaviorNames.Herbivore);
            Assert.AreEqual("EvoLifePredator", MlAgentsBehaviorNames.Predator);
            Assert.AreEqual(MlAgentsBehaviorNames.Herbivore, MlAgentsBehaviorNames.ForRole(CreatureRole.Herbivore));
            Assert.AreEqual(MlAgentsBehaviorNames.Predator, MlAgentsBehaviorNames.ForRole(CreatureRole.Predator));
        }
    }
}
