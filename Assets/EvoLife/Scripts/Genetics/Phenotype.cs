using EvoLife.Common;

namespace EvoLife.Genetics
{
    /// <summary>
    /// Phenotype resolved from a genome. Immutable snapshot after decoding.
    /// Multipliers are trait / canonical default so a default genome is Neutral.
    /// </summary>
    public readonly struct Phenotype : IReadOnlyPhenotype
    {
        public float MaxSpeedMultiplier { get; }
        public float SprintSpeedMultiplier { get; }
        public float MetabolismMultiplier { get; }
        public float SensoryRangeMultiplier { get; }
        public float ReproductionThresholdMultiplier { get; }
        public float MaxEnergyMultiplier { get; }
        public float MaxAgeMultiplier { get; }
        public float BodySizeMultiplier { get; }
        public float Aggression { get; }

        public Phenotype(
            float maxSpeedMultiplier,
            float sprintSpeedMultiplier,
            float metabolismMultiplier,
            float sensoryRangeMultiplier,
            float reproductionThresholdMultiplier,
            float maxEnergyMultiplier,
            float maxAgeMultiplier,
            float bodySizeMultiplier,
            float aggression)
        {
            MaxSpeedMultiplier = maxSpeedMultiplier;
            SprintSpeedMultiplier = sprintSpeedMultiplier;
            MetabolismMultiplier = metabolismMultiplier;
            SensoryRangeMultiplier = sensoryRangeMultiplier;
            ReproductionThresholdMultiplier = reproductionThresholdMultiplier;
            MaxEnergyMultiplier = maxEnergyMultiplier;
            MaxAgeMultiplier = maxAgeMultiplier;
            BodySizeMultiplier = bodySizeMultiplier;
            Aggression = aggression;
        }

        public static Phenotype Neutral =>
            new Phenotype(1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, CanonicalGenomeSchema.Get(TraitId.Aggression).Default);
    }
}
