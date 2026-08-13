namespace EvoLife.Common
{
    /// <summary>
    /// Capability multipliers derived from genetics (phenotype).
    /// Genetics owns production; Creatures apply them to movement/metabolism.
    /// </summary>
    public interface IReadOnlyPhenotype
    {
        float MaxSpeedMultiplier { get; }
        float MetabolismMultiplier { get; }
        float SensoryRangeMultiplier { get; }
        float ReproductionThresholdMultiplier { get; }
    }
}
