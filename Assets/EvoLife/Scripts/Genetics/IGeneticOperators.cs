using System;

namespace EvoLife.Genetics
{
    public interface IGeneticOperators
    {
        Genome CreateFounder(System.Random random);
        Genome Crossover(Genome parentA, Genome parentB, System.Random random);
        Genome Mutate(Genome source, System.Random random);
    }

    /// <summary>
    /// Canonical inheritance operators matching the Python reference semantics:
    /// weighted/average/random-parent crossover, per-trait mutation, hard-bound clamps.
    /// </summary>
    public sealed class DefaultGeneticOperators : IGeneticOperators
    {
        readonly GeneticsConfig config;

        public DefaultGeneticOperators(GeneticsConfig geneticsConfig = null)
        {
            config = geneticsConfig ?? GeneticsConfig.Default;
        }

        public GeneticsConfig Config => config;

        public Genome CreateFounder(System.Random random)
        {
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            var values = new float[CanonicalGenomeSchema.TraitCount];
            for (var i = 0; i < values.Length; i++)
            {
                values[i] = CanonicalGenomeSchema.Get(i).SampleGeneration(random);
            }

            return new Genome(values);
        }

        public Genome Crossover(Genome parentA, Genome parentB, System.Random random)
        {
            if (parentA == null && parentB == null)
            {
                return Genome.CreateDefault();
            }

            if (parentA == null)
            {
                return parentB.Clone();
            }

            if (parentB == null)
            {
                return parentA.Clone();
            }

            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            var crossover = config.Crossover;
            var values = new float[CanonicalGenomeSchema.TraitCount];

            for (var i = 0; i < values.Length; i++)
            {
                var trait = CanonicalGenomeSchema.Get(i);
                var a = parentA.Get(trait.Id);
                var b = parentB.Get(trait.Id);
                float combined;

                switch (crossover.Mode)
                {
                    case CrossoverMode.Average:
                        combined = (a + b) * 0.5f;
                        break;
                    case CrossoverMode.RandomParent:
                        combined = random.NextDouble() < 0.5 ? a : b;
                        break;
                    default:
                        combined = crossover.ParentAWeight * a + (1f - crossover.ParentAWeight) * b;
                        break;
                }

                values[i] = trait.Clamp(combined);
            }

            return new Genome(values);
        }

        public Genome Mutate(Genome source, System.Random random) =>
            Mutate(source, config.Mutation, random);

        public Genome Mutate(Genome source, MutationConfig mutation, System.Random random)
        {
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            mutation = mutation ?? config.Mutation;
            var clone = source?.Clone() ?? Genome.CreateDefault();

            for (var i = 0; i < CanonicalGenomeSchema.TraitCount; i++)
            {
                if (random.NextDouble() >= mutation.Probability)
                {
                    continue;
                }

                var trait = CanonicalGenomeSchema.Get(i);
                var magnitude = trait.MutationMagnitude * mutation.MagnitudeScale;
                if (magnitude <= 0f)
                {
                    continue;
                }

                var delta = ((float)random.NextDouble() * 2f - 1f) * magnitude;
                clone.Set(trait.Id, clone.Get(trait.Id) + delta);
            }

            return clone;
        }

        public Genome CreateOffspring(Genome parentA, Genome parentB, System.Random random)
        {
            var child = Crossover(parentA, parentB, random);
            return Mutate(child, random);
        }
    }
}
