using System;
using UnityEngine;
using EvoLife.Common;

namespace EvoLife.Presentation
{
    /// <summary>
    /// Shared stylized palette. Simulation biome kinds stay authoritative;
    /// these colors are presentation-only.
    /// </summary>
    public static class PresentationPalette
    {
        public static readonly Color Grassland = new Color(0.62f, 0.82f, 0.38f);
        public static readonly Color Forest = new Color(0.22f, 0.48f, 0.28f);
        public static readonly Color Wetland = new Color(0.28f, 0.52f, 0.46f);
        public static readonly Color Rocky = new Color(0.62f, 0.54f, 0.42f);
        public static readonly Color Water = new Color(0.18f, 0.48f, 0.66f, 0.78f);
        public static readonly Color PlantHealthy = new Color(0.35f, 0.78f, 0.28f);
        public static readonly Color PlantDepleted = new Color(0.55f, 0.42f, 0.22f);
        public static readonly Color Herbivore = new Color(0.42f, 0.72f, 0.38f);
        public static readonly Color HerbivoreAccent = new Color(0.78f, 0.92f, 0.45f);
        public static readonly Color Predator = new Color(0.78f, 0.32f, 0.22f);
        public static readonly Color PredatorAccent = new Color(0.95f, 0.62f, 0.28f);
        public static readonly Color Drought = new Color(0.72f, 0.62f, 0.32f);
        public static readonly Color Wildfire = new Color(1f, 0.42f, 0.12f);
        public static readonly Color HeatWave = new Color(1f, 0.55f, 0.28f);
        public static readonly Color DayAmbient = new Color(0.62f, 0.72f, 0.82f);
        public static readonly Color NightAmbient = new Color(0.08f, 0.1f, 0.18f);

        public static Color ForBiome(BiomeKind kind)
        {
            switch (kind)
            {
                case BiomeKind.Grassland:
                    return Grassland;
                case BiomeKind.Forest:
                    return Forest;
                case BiomeKind.Wetland:
                    return Wetland;
                case BiomeKind.Rocky:
                    return Rocky;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unhandled BiomeKind.");
            }
        }

        public static Color ForRole(CreatureRole role)
        {
            switch (role)
            {
                case CreatureRole.Herbivore:
                    return Herbivore;
                case CreatureRole.Predator:
                    return Predator;
                default:
                    throw new ArgumentOutOfRangeException(nameof(role), role, "Unhandled CreatureRole.");
            }
        }

        public static Color AccentForRole(CreatureRole role)
        {
            switch (role)
            {
                case CreatureRole.Herbivore:
                    return HerbivoreAccent;
                case CreatureRole.Predator:
                    return PredatorAccent;
                default:
                    throw new ArgumentOutOfRangeException(nameof(role), role, "Unhandled CreatureRole.");
            }
        }
    }
}
