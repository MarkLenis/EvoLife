namespace EvoLife.Genetics
{
    /// <summary>
    /// Named identifiers for CanonicalGenomeSchema v1.
    /// Ordinal values match schema storage order; prefer these over raw indices.
    /// Canonical snake_case names match the Python reference package.
    /// </summary>
    public enum TraitId
    {
        BaseMovementSpeed = 0,
        SprintSpeed = 1,
        VisionRange = 2,
        MaximumEnergy = 3,
        MetabolismRate = 4,
        BodySize = 5,
        Aggression = 6,
        ReproductionThreshold = 7,
        MaximumAge = 8
    }
}
