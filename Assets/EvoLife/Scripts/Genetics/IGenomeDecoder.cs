namespace EvoLife.Genetics
{
    /// <summary>
    /// Maps genome traits to phenotype multipliers. Keep decoding rules here — not in Creatures or AI.
    /// </summary>
    public interface IGenomeDecoder
    {
        Phenotype Decode(Genome genome);
    }

    /// <summary>
    /// Canonical decoder: multiplier = trait_value / trait_default (aggression stays raw [0,1]).
    /// </summary>
    public sealed class CanonicalGenomeDecoder : IGenomeDecoder
    {
        public Phenotype Decode(Genome genome)
        {
            if (genome == null)
            {
                return Phenotype.Neutral;
            }

            return new Phenotype(
                Multiplier(genome, TraitId.BaseMovementSpeed),
                Multiplier(genome, TraitId.SprintSpeed),
                Multiplier(genome, TraitId.MetabolismRate),
                Multiplier(genome, TraitId.VisionRange),
                Multiplier(genome, TraitId.ReproductionThreshold),
                Multiplier(genome, TraitId.MaximumEnergy),
                Multiplier(genome, TraitId.MaximumAge),
                Multiplier(genome, TraitId.BodySize),
                genome.Get(TraitId.Aggression));
        }

        static float Multiplier(Genome genome, TraitId id)
        {
            var trait = CanonicalGenomeSchema.Get(id);
            if (trait.Default == 0f)
            {
                return 1f;
            }

            return genome.Get(id) / trait.Default;
        }
    }
}
