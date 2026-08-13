namespace EvoLife.Common
{
    /// <summary>
    /// Named canonical genome traits for analytics. Values are raw trait units, not
    /// phenotype multipliers. Genetics owns storage; observers only read.
    /// </summary>
    public interface IReadOnlyGenomeTraits
    {
        int TraitCount { get; }
        string GetTraitName(int index);
        float GetTraitValue(int index);
        bool TryGetTrait(string canonicalName, out float value);
    }
}
