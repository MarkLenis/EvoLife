namespace EvoLife.Common
{
    /// <summary>
    /// Read-only plant/water statistics for Analytics and optional AI context.
    /// Environment owns the values; consumers must not mutate resources through this contract.
    /// </summary>
    public interface IReadOnlyResourceCensus
    {
        int PlantCount { get; }

        int WaterSourceCount { get; }

        float TotalPlantFoodRemaining { get; }

        float TotalPlantCapacity { get; }

        /// <summary>Remaining plant food divided by capacity. 0 when there is no capacity.</summary>
        float PlantAbundance { get; }

        /// <summary>Plant count divided by configured world area. 0 when area is not positive.</summary>
        float PlantDensity { get; }

        float WorldArea { get; }
    }
}
