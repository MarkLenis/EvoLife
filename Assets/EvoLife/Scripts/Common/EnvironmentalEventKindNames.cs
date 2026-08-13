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

        public static bool TryParse(string wireName, out EnvironmentalEventKind kind)
        {
            if (string.Equals(wireName, Drought, StringComparison.OrdinalIgnoreCase)
                || string.Equals(wireName, "Drought", StringComparison.OrdinalIgnoreCase))
            {
                kind = EnvironmentalEventKind.Drought;
                return true;
            }

            if (string.Equals(wireName, Wildfire, StringComparison.OrdinalIgnoreCase)
                || string.Equals(wireName, "Wildfire", StringComparison.OrdinalIgnoreCase))
            {
                kind = EnvironmentalEventKind.Wildfire;
                return true;
            }

            if (string.Equals(wireName, HeatWave, StringComparison.OrdinalIgnoreCase)
                || string.Equals(wireName, "HeatWave", StringComparison.OrdinalIgnoreCase)
                || string.Equals(wireName, "Heat Wave", StringComparison.OrdinalIgnoreCase))
            {
                kind = EnvironmentalEventKind.HeatWave;
                return true;
            }

            if (string.Equals(wireName, FoodBoom, StringComparison.OrdinalIgnoreCase)
                || string.Equals(wireName, "FoodBoom", StringComparison.OrdinalIgnoreCase))
            {
                kind = EnvironmentalEventKind.FoodBoom;
                return true;
            }

            if (string.Equals(wireName, DiseasePressure, StringComparison.OrdinalIgnoreCase)
                || string.Equals(wireName, "DiseasePressure", StringComparison.OrdinalIgnoreCase))
            {
                kind = EnvironmentalEventKind.DiseasePressure;
                return true;
            }

            if (string.Equals(wireName, PredatorIntroduction, StringComparison.OrdinalIgnoreCase)
                || string.Equals(wireName, "PredatorIntroduction", StringComparison.OrdinalIgnoreCase))
            {
                kind = EnvironmentalEventKind.PredatorIntroduction;
                return true;
            }

            if (string.Equals(wireName, PredatorRemoval, StringComparison.OrdinalIgnoreCase)
                || string.Equals(wireName, "PredatorRemoval", StringComparison.OrdinalIgnoreCase))
            {
                kind = EnvironmentalEventKind.PredatorRemoval;
                return true;
            }

            kind = EnvironmentalEventKind.Drought;
            return false;
        }

        static string Unreachable(EnvironmentalEventKind kind)
        {
            throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unhandled EnvironmentalEventKind.");
        }
    }
}
