using UnityEngine;
using EvoLife.Common;

namespace EvoLife.Genetics
{
    /// <summary>
    /// Holds a creature's genome and exposes the decoded phenotype to other modules.
    /// </summary>
    public sealed class CreatureGenome : MonoBehaviour, IReadOnlyPhenotype
    {
        Genome genome;
        Phenotype phenotype = Phenotype.Neutral;
        IGenomeDecoder decoder = new CanonicalGenomeDecoder();

        public Genome Genome => genome;
        public IReadOnlyPhenotype PhenotypeView => phenotype;

        public float MaxSpeedMultiplier => phenotype.MaxSpeedMultiplier;
        public float SprintSpeedMultiplier => phenotype.SprintSpeedMultiplier;
        public float MetabolismMultiplier => phenotype.MetabolismMultiplier;
        public float SensoryRangeMultiplier => phenotype.SensoryRangeMultiplier;
        public float ReproductionThresholdMultiplier => phenotype.ReproductionThresholdMultiplier;
        public float MaxEnergyMultiplier => phenotype.MaxEnergyMultiplier;
        public float MaxAgeMultiplier => phenotype.MaxAgeMultiplier;
        public float BodySizeMultiplier => phenotype.BodySizeMultiplier;
        public float Aggression => phenotype.Aggression;

        public void Initialize(Genome initialGenome, IGenomeDecoder genomeDecoder = null)
        {
            decoder = genomeDecoder ?? decoder;
            genome = initialGenome?.Clone() ?? Genome.CreateDefault();
            phenotype = decoder.Decode(genome);
        }

        public void ReplaceGenome(Genome next)
        {
            genome = next?.Clone() ?? Genome.CreateDefault();
            phenotype = decoder.Decode(genome);
        }
    }
}
