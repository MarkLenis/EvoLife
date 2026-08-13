using UnityEngine;

namespace EvoLife.Genetics
{
    public interface IGeneticOperators
    {
        Genome CreateRandom(int geneCount, System.Random random);
        Genome Crossover(Genome parentA, Genome parentB, System.Random random);
        Genome Mutate(Genome source, float mutationRate, float mutationStrength, System.Random random);
    }

    /// <summary>
    /// Minimal crossover + gaussian-style mutation. Not a full EA — architectural seam only.
    /// </summary>
    public sealed class DefaultGeneticOperators : IGeneticOperators
    {
        public Genome CreateRandom(int geneCount, System.Random random)
        {
            var genome = new Genome(geneCount);
            for (var i = 0; i < genome.Length; i++)
            {
                genome.SetGene(i, (float)random.NextDouble());
            }

            return genome;
        }

        public Genome Crossover(Genome parentA, Genome parentB, System.Random random)
        {
            if (parentA == null || parentB == null)
            {
                return parentA?.Clone() ?? parentB?.Clone() ?? new Genome(4);
            }

            var length = Mathf.Min(parentA.Length, parentB.Length);
            var child = new Genome(length);
            var midpoint = random.Next(0, length);

            for (var i = 0; i < length; i++)
            {
                child.SetGene(i, i < midpoint ? parentA.GetGene(i) : parentB.GetGene(i));
            }

            return child;
        }

        public Genome Mutate(Genome source, float mutationRate, float mutationStrength, System.Random random)
        {
            var clone = source?.Clone() ?? new Genome(4);
            mutationRate = Mathf.Clamp01(mutationRate);
            mutationStrength = Mathf.Max(0f, mutationStrength);

            for (var i = 0; i < clone.Length; i++)
            {
                if (random.NextDouble() > mutationRate)
                {
                    continue;
                }

                var delta = ((float)random.NextDouble() * 2f - 1f) * mutationStrength;
                clone.SetGene(i, clone.GetGene(i) + delta);
            }

            return clone;
        }
    }
}
