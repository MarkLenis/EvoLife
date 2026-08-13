namespace EvoLife.Genetics
{
    /// <summary>
    /// Maps genome genes to phenotype multipliers. Keep decoding rules here — not in Creatures or AI.
    /// Gene layout (v0): [0]=speed, [1]=metabolism, [2]=sensory, [3]=reproduction.
    /// </summary>
    public interface IGenomeDecoder
    {
        Phenotype Decode(Genome genome);
    }

    public sealed class LinearGenomeDecoder : IGenomeDecoder
    {
        public Phenotype Decode(Genome genome)
        {
            if (genome == null || genome.Length < 4)
            {
                return Phenotype.Neutral;
            }

            // Map [0,1] gene → [0.5, 1.5] multiplier.
            float Map(float gene) => 0.5f + gene;

            return new Phenotype(
                Map(genome.GetGene(0)),
                Map(genome.GetGene(1)),
                Map(genome.GetGene(2)),
                Map(genome.GetGene(3)));
        }
    }
}
