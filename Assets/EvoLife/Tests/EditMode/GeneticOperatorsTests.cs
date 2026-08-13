using NUnit.Framework;
using EvoLife.Genetics;

namespace EvoLife.Tests
{
    public sealed class GeneticOperatorsTests
    {
        [Test]
        public void Crossover_PreservesGeneCount()
        {
            var ops = new DefaultGeneticOperators();
            var random = new System.Random(7);
            var a = ops.CreateRandom(4, random);
            var b = ops.CreateRandom(4, random);

            var child = ops.Crossover(a, b, random);

            Assert.AreEqual(4, child.Length);
        }

        [Test]
        public void Mutate_KeepsGenesInUnitInterval()
        {
            var ops = new DefaultGeneticOperators();
            var random = new System.Random(11);
            var source = ops.CreateRandom(4, random);

            var mutated = ops.Mutate(source, mutationRate: 1f, mutationStrength: 0.5f, random);

            for (var i = 0; i < mutated.Length; i++)
            {
                Assert.GreaterOrEqual(mutated.GetGene(i), 0f);
                Assert.LessOrEqual(mutated.GetGene(i), 1f);
            }
        }

        [Test]
        public void LinearDecoder_MapsMidGenesToNearNeutralPhenotype()
        {
            var genome = new Genome(new[] { 0.5f, 0.5f, 0.5f, 0.5f });
            var decoder = new LinearGenomeDecoder();

            var phenotype = decoder.Decode(genome);

            Assert.AreEqual(1f, phenotype.MaxSpeedMultiplier, 0.0001f);
            Assert.AreEqual(1f, phenotype.MetabolismMultiplier, 0.0001f);
            Assert.AreEqual(1f, phenotype.SensoryRangeMultiplier, 0.0001f);
            Assert.AreEqual(1f, phenotype.ReproductionThresholdMultiplier, 0.0001f);
        }
    }
}
