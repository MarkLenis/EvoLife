namespace EvoLife.Common
{
    /// <summary>
    /// Capability values derived from genetics (phenotype).
    /// Genetics owns production; Creatures apply them to movement/metabolism.
    /// Multipliers are relative to canonical trait defaults (neutral genome → 1).
    /// <see cref="Aggression"/> is the raw [0,1] trait, not a multiplier.
    /// </summary>
    public interface IReadOnlyPhenotype
    {
        float MaxSpeedMultiplier { get; }
        float SprintSpeedMultiplier { get; }
        float MetabolismMultiplier { get; }
        float SensoryRangeMultiplier { get; }
        float ReproductionThresholdMultiplier { get; }
        float MaxEnergyMultiplier { get; }
        float MaxAgeMultiplier { get; }
        float BodySizeMultiplier { get; }
        float Aggression { get; }
    }
}
