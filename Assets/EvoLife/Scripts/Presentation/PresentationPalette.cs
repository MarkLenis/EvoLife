using System;
using UnityEngine;
using EvoLife.Common;

namespace EvoLife.Presentation
{
    /// <summary>
    /// Muted natural research-diorama palette. Simulation biome kinds stay authoritative;
    /// these colors are presentation-only (no neon / arcade team colors).
    /// </summary>
    public static class PresentationPalette
    {
        public static readonly Color Grassland = new Color(0.48f, 0.66f, 0.34f);
        public static readonly Color GrasslandSunlit = new Color(0.54f, 0.72f, 0.36f);
        public static readonly Color GrasslandShade = new Color(0.40f, 0.56f, 0.30f);
        public static readonly Color Forest = new Color(0.16f, 0.32f, 0.20f);
        public static readonly Color ForestFloor = new Color(0.14f, 0.26f, 0.16f);
        public static readonly Color Canopy = new Color(0.16f, 0.48f, 0.24f);
        public static readonly Color CanopyAlt = new Color(0.26f, 0.54f, 0.26f);
        public static readonly Color Wetland = new Color(0.22f, 0.50f, 0.44f);
        public static readonly Color WetMud = new Color(0.24f, 0.38f, 0.32f);
        public static readonly Color Rocky = new Color(0.74f, 0.56f, 0.34f);
        public static readonly Color DryEarth = new Color(0.78f, 0.58f, 0.36f);
        public static readonly Color OuterBuffer = new Color(0.46f, 0.60f, 0.34f);
        public static readonly Color Water = new Color(0.22f, 0.50f, 0.58f, 0.80f);
        public static readonly Color PlantHealthy = new Color(0.32f, 0.78f, 0.26f);
        public static readonly Color PlantDepleted = new Color(0.52f, 0.42f, 0.26f);
        public static readonly Color DecorativeGrass = new Color(0.40f, 0.52f, 0.30f);
        public static readonly Color DryGrass = new Color(0.58f, 0.50f, 0.30f);
        public static readonly Color Trunk = new Color(0.36f, 0.26f, 0.16f);
        public static readonly Color Stone = new Color(0.52f, 0.50f, 0.46f);
        public static readonly Color StoneDark = new Color(0.42f, 0.40f, 0.38f);
        public static readonly Color Reed = new Color(0.34f, 0.48f, 0.32f);
        public static readonly Color Flower = new Color(0.82f, 0.74f, 0.38f);
        public static readonly Color Herbivore = new Color(0.76f, 0.64f, 0.46f);
        public static readonly Color HerbivoreAccent = new Color(0.86f, 0.74f, 0.54f);
        public static readonly Color Predator = new Color(0.32f, 0.24f, 0.22f);
        public static readonly Color PredatorAccent = new Color(0.58f, 0.34f, 0.26f);
        public static readonly Color Drought = new Color(0.64f, 0.54f, 0.34f);
        public static readonly Color Wildfire = new Color(0.90f, 0.46f, 0.18f);
        public static readonly Color HeatWave = new Color(0.92f, 0.62f, 0.34f);
        public static readonly Color DayAmbient = new Color(0.62f, 0.68f, 0.72f);
        public static readonly Color NightAmbient = new Color(0.40f, 0.44f, 0.52f);
        public static readonly Color FogDay = new Color(0.62f, 0.72f, 0.78f);
        public static readonly Color FogNight = new Color(0.32f, 0.38f, 0.48f);

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
