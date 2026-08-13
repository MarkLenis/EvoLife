using EvoLife.Common;

namespace EvoLife.Environment
{
    /// <summary>
    /// Immutable resource census snapshot.
    /// </summary>
    public readonly struct ResourceCensus : IReadOnlyResourceCensus
    {
        public ResourceCensus(
            int plantCount,
            int waterSourceCount,
            float totalPlantFoodRemaining,
            float totalPlantCapacity,
            float worldArea)
        {
            PlantCount = plantCount < 0 ? 0 : plantCount;
            WaterSourceCount = waterSourceCount < 0 ? 0 : waterSourceCount;
            TotalPlantFoodRemaining = totalPlantFoodRemaining < 0f ? 0f : totalPlantFoodRemaining;
            TotalPlantCapacity = totalPlantCapacity < 0f ? 0f : totalPlantCapacity;
            WorldArea = worldArea < 0f ? 0f : worldArea;
        }

        public int PlantCount { get; }
        public int WaterSourceCount { get; }
        public float TotalPlantFoodRemaining { get; }
        public float TotalPlantCapacity { get; }
        public float WorldArea { get; }

        public float PlantAbundance =>
            TotalPlantCapacity <= 0f ? 0f : Clamp01(TotalPlantFoodRemaining / TotalPlantCapacity);

        public float PlantDensity => WorldArea <= 0f ? 0f : PlantCount / WorldArea;

        static float Clamp01(float value) =>
            value < 0f ? 0f : value > 1f ? 1f : value;
    }
}
