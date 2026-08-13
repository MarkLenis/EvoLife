using UnityEngine;
using EvoLife.Common;

namespace EvoLife.Genetics
{
    /// <summary>
    /// Phenotype resolved from a genome. Immutable snapshot after decoding.
    /// </summary>
    public readonly struct Phenotype : IReadOnlyPhenotype
    {
        public float MaxSpeedMultiplier { get; }
        public float MetabolismMultiplier { get; }
        public float SensoryRangeMultiplier { get; }
        public float ReproductionThresholdMultiplier { get; }

        public Phenotype(
            float maxSpeedMultiplier,
            float metabolismMultiplier,
            float sensoryRangeMultiplier,
            float reproductionThresholdMultiplier)
        {
            MaxSpeedMultiplier = maxSpeedMultiplier;
            MetabolismMultiplier = metabolismMultiplier;
            SensoryRangeMultiplier = sensoryRangeMultiplier;
            ReproductionThresholdMultiplier = reproductionThresholdMultiplier;
        }

        public static Phenotype Neutral => new Phenotype(1f, 1f, 1f, 1f);
    }
}
