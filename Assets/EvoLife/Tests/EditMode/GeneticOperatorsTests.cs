using NUnit.Framework;
using EvoLife.Genetics;

namespace EvoLife.Tests
{
    public sealed class GeneticOperatorsTests
    {
        static DefaultGeneticOperators Ops(GeneticsConfig config = null) =>
            new DefaultGeneticOperators(config);

        [Test]
        public void CanonicalSchema_HasNineNamedTraits()
        {
            Assert.AreEqual(1, CanonicalGenomeSchema.Version);
            Assert.AreEqual(9, CanonicalGenomeSchema.TraitCount);
            Assert.AreEqual(9, CanonicalGenomeSchema.Count);

            var names = CanonicalGenomeSchema.CanonicalNames();
            Assert.AreEqual("base_movement_speed", names[0]);
            Assert.AreEqual("sprint_speed", names[1]);
            Assert.AreEqual("vision_range", names[2]);
            Assert.AreEqual("maximum_energy", names[3]);
            Assert.AreEqual("metabolism_rate", names[4]);
            Assert.AreEqual("body_size", names[5]);
            Assert.AreEqual("aggression", names[6]);
            Assert.AreEqual("reproduction_threshold", names[7]);
            Assert.AreEqual("maximum_age", names[8]);
        }

        [Test]
        public void FounderGeneration_IsDeterministicForTheSameSeed()
        {
            var a = Ops().CreateFounder(new System.Random(12345));
            var b = Ops().CreateFounder(new System.Random(12345));

            CollectionAssert.AreEqual(a.ToArray(), b.ToArray());
            Assert.AreEqual(CanonicalGenomeSchema.TraitCount, a.Length);
        }

        [Test]
        public void FounderGeneration_DiffersAcrossSeeds()
        {
            var a = Ops().CreateFounder(new System.Random(1));
            var b = Ops().CreateFounder(new System.Random(2));

            CollectionAssert.AreNotEqual(a.ToArray(), b.ToArray());
        }

        [Test]
        public void FounderGeneration_StaysWithinGenerationRanges()
        {
            var genome = Ops().CreateFounder(new System.Random(99));

            foreach (var trait in CanonicalGenomeSchema.All())
            {
                var value = genome.Get(trait.Id);
                Assert.GreaterOrEqual(value, trait.GenerationMin, trait.CanonicalName);
                Assert.LessOrEqual(value, trait.GenerationMax, trait.CanonicalName);
            }
        }

        [Test]
        public void Crossover_PreservesCanonicalTraitCount()
        {
            var ops = Ops();
            var random = new System.Random(7);
            var child = ops.Crossover(ops.CreateFounder(random), ops.CreateFounder(random), random);

            Assert.AreEqual(CanonicalGenomeSchema.TraitCount, child.Length);
        }

        [Test]
        public void AverageCrossover_BlendsParentTraitsAndClamps()
        {
            var minParent = GenomeAtBounds(useMax: false);
            var maxParent = GenomeAtBounds(useMax: true);
            var ops = Ops(new GeneticsConfig(new CrossoverConfig(CrossoverMode.Average)));

            var child = ops.Crossover(minParent, maxParent, new System.Random(0));

            foreach (var trait in CanonicalGenomeSchema.All())
            {
                var expected = (trait.HardMin + trait.HardMax) * 0.5f;
                Assert.AreEqual(expected, child.Get(trait.Id), 0.0001f, trait.CanonicalName);
            }
        }

        [Test]
        public void RandomParentCrossover_InheritsAParentValue()
        {
            var minParent = GenomeAtBounds(useMax: false);
            var maxParent = GenomeAtBounds(useMax: true);
            var ops = Ops(new GeneticsConfig(new CrossoverConfig(CrossoverMode.RandomParent)));

            for (var seed = 0; seed < 20; seed++)
            {
                var child = ops.Crossover(minParent, maxParent, new System.Random(seed));
                foreach (var trait in CanonicalGenomeSchema.All())
                {
                    var value = child.Get(trait.Id);
                    Assert.IsTrue(
                        value == minParent.Get(trait.Id) || value == maxParent.Get(trait.Id),
                        trait.CanonicalName);
                }
            }
        }

        [Test]
        public void WeightedCrossover_IsDeterministicAndInBounds()
        {
            var ops = Ops(new GeneticsConfig(new CrossoverConfig(CrossoverMode.Weighted, 0.75f)));
            var a = Ops().CreateFounder(new System.Random(1));
            var b = Ops().CreateFounder(new System.Random(2));

            var c1 = ops.Crossover(a, b, new System.Random(999));
            var c2 = ops.Crossover(a, b, new System.Random(999));

            CollectionAssert.AreEqual(c1.ToArray(), c2.ToArray());
            AssertInHardBounds(c1);
        }

        [Test]
        public void Mutate_KeepsTraitsWithinHardBounds()
        {
            var ops = Ops(new GeneticsConfig(
                mutation: new MutationConfig(probability: 1f, magnitudeScale: 50f)));
            var source = GenomeAtBounds(useMax: true);

            for (var seed = 0; seed < 40; seed++)
            {
                AssertInHardBounds(ops.Mutate(source, new System.Random(seed)));
            }
        }

        [Test]
        public void ZeroMutation_NeverChangesTheGenome()
        {
            var ops = new DefaultGeneticOperators(GeneticsConfig.NoMutation());
            var source = Ops().CreateFounder(new System.Random(3));

            for (var seed = 0; seed < 30; seed++)
            {
                CollectionAssert.AreEqual(source.ToArray(), ops.Mutate(source, new System.Random(seed)).ToArray());
            }
        }

        [Test]
        public void DefaultDecoder_MapsDefaultGenomeToNeutralPhenotype()
        {
            var phenotype = new CanonicalGenomeDecoder().Decode(Genome.CreateDefault());

            Assert.AreEqual(1f, phenotype.MaxSpeedMultiplier, 0.0001f);
            Assert.AreEqual(1f, phenotype.SprintSpeedMultiplier, 0.0001f);
            Assert.AreEqual(1f, phenotype.MetabolismMultiplier, 0.0001f);
            Assert.AreEqual(1f, phenotype.SensoryRangeMultiplier, 0.0001f);
            Assert.AreEqual(1f, phenotype.ReproductionThresholdMultiplier, 0.0001f);
            Assert.AreEqual(1f, phenotype.MaxEnergyMultiplier, 0.0001f);
            Assert.AreEqual(1f, phenotype.MaxAgeMultiplier, 0.0001f);
            Assert.AreEqual(1f, phenotype.BodySizeMultiplier, 0.0001f);
            Assert.AreEqual(
                CanonicalGenomeSchema.Get(TraitId.Aggression).Default,
                phenotype.Aggression,
                0.0001f);
        }

        [Test]
        public void DefaultDecoder_ScalesMultipliersFromTraitDefaults()
        {
            var genome = Genome.FromTraitValues(
                (TraitId.BaseMovementSpeed, 4f),
                (TraitId.MetabolismRate, 1f),
                (TraitId.VisionRange, 24f),
                (TraitId.MaximumEnergy, 200f),
                (TraitId.MaximumAge, 1000f));

            var phenotype = new CanonicalGenomeDecoder().Decode(genome);

            Assert.AreEqual(2f, phenotype.MaxSpeedMultiplier, 0.0001f);
            Assert.AreEqual(2f, phenotype.MetabolismMultiplier, 0.0001f);
            Assert.AreEqual(2f, phenotype.SensoryRangeMultiplier, 0.0001f);
            Assert.AreEqual(2f, phenotype.MaxEnergyMultiplier, 0.0001f);
            Assert.AreEqual(2f, phenotype.MaxAgeMultiplier, 0.0001f);
        }

        [Test]
        public void NormalizedGeneticsValues_RemainInUnitInterval()
        {
            var genome = Ops().CreateFounder(new System.Random(99));
            var vector = GeneticObservationProvider.GetObservationVector(genome);

            Assert.AreEqual(CanonicalGenomeSchema.TraitCount, vector.Length);
            Assert.AreEqual(GeneticObservationProvider.ObservationSize, vector.Length);
            foreach (var value in vector)
            {
                Assert.GreaterOrEqual(value, 0f);
                Assert.LessOrEqual(value, 1f);
            }

            var minGenome = GenomeAtBounds(useMax: false);
            var maxGenome = GenomeAtBounds(useMax: true);
            foreach (var value in minGenome.ToNormalizedArray())
            {
                Assert.AreEqual(0f, value, 0.0001f);
            }

            foreach (var value in maxGenome.ToNormalizedArray())
            {
                Assert.AreEqual(1f, value, 0.0001f);
            }
        }

        [Test]
        public void Set_ClampsToHardBounds()
        {
            var genome = Genome.CreateDefault();
            genome.Set(TraitId.BaseMovementSpeed, -100f);
            genome.Set(TraitId.MaximumAge, 999999f);

            Assert.AreEqual(CanonicalGenomeSchema.Get(TraitId.BaseMovementSpeed).HardMin, genome.Get(TraitId.BaseMovementSpeed));
            Assert.AreEqual(CanonicalGenomeSchema.Get(TraitId.MaximumAge).HardMax, genome.Get(TraitId.MaximumAge));
        }

        static Genome GenomeAtBounds(bool useMax)
        {
            var values = new float[CanonicalGenomeSchema.TraitCount];
            for (var i = 0; i < values.Length; i++)
            {
                var trait = CanonicalGenomeSchema.Get(i);
                values[i] = useMax ? trait.HardMax : trait.HardMin;
            }

            return new Genome(values);
        }

        static void AssertInHardBounds(Genome genome)
        {
            foreach (var trait in CanonicalGenomeSchema.All())
            {
                var value = genome.Get(trait.Id);
                Assert.GreaterOrEqual(value, trait.HardMin, trait.CanonicalName);
                Assert.LessOrEqual(value, trait.HardMax, trait.CanonicalName);
            }
        }
    }
}
