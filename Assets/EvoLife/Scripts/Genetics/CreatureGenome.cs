using UnityEngine;
using EvoLife.Common;

namespace EvoLife.Genetics
{
    /// <summary>
    /// Holds a creature's genome and exposes the decoded phenotype to other modules.
    /// </summary>
    public sealed class CreatureGenome : MonoBehaviour, IReadOnlyPhenotype
    {
        [SerializeField] int geneCount = 4;

        Genome genome;
        Phenotype phenotype = Phenotype.Neutral;
        IGenomeDecoder decoder = new LinearGenomeDecoder();

        public Genome Genome => genome;
        public IReadOnlyPhenotype PhenotypeView => phenotype;

        public float MaxSpeedMultiplier => phenotype.MaxSpeedMultiplier;
        public float MetabolismMultiplier => phenotype.MetabolismMultiplier;
        public float SensoryRangeMultiplier => phenotype.SensoryRangeMultiplier;
        public float ReproductionThresholdMultiplier => phenotype.ReproductionThresholdMultiplier;

        public void Initialize(Genome initialGenome, IGenomeDecoder genomeDecoder = null)
        {
            decoder = genomeDecoder ?? decoder;
            genome = initialGenome?.Clone() ?? new Genome(geneCount);
            phenotype = decoder.Decode(genome);
        }

        public void ReplaceGenome(Genome next)
        {
            genome = next?.Clone() ?? new Genome(geneCount);
            phenotype = decoder.Decode(genome);
        }
    }
}
