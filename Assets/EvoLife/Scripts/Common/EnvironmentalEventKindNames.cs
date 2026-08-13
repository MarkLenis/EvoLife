using System;

namespace EvoLife.Common
{
    /// <summary>
    /// Stable wire names for ecological events. Analytics and config may use these strings.
    /// </summary>
    public static class EnvironmentalEventKindNames
    {
        public const string Drought = "drought";
        public const string Wildfire = "wildfire";
        public const string HeatWave = "heat_wave";
        public const string FoodBoom = "food_boom";
        public const string DiseasePressure = "disease_pressure";
        public const string PredatorIntroduction = "predator_introduction";
        public const string PredatorRemoval = "predator_removal";

        public static string ToWireName(EnvironmentalEventKind kind)
        {
            switch (kind)
            {
                case EnvironmentalEventKind.Drought:
                    return Drought;
                case EnvironmentalEventKind.Wildfire:
                    return Wildfire;
                case EnvironmentalEventKind.HeatWave:
                    return HeatWave;
                case EnvironmentalEventKind.FoodBoom:
                    return FoodBoom;
                case EnvironmentalEventKind.DiseasePressure:
                    return DiseasePressure;
                case EnvironmentalEventKind.PredatorIntroduction:
                    return PredatorIntroduction;
                case EnvironmentalEventKind.PredatorRemoval:
                    return PredatorRemoval;
                default:
                    return Unreachable(kind);
            }
        }

        static string Unreachable(EnvironmentalEventKind kind)
        {
            throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unhandled EnvironmentalEventKind.");
        }
    }
}
