using System.Collections.Generic;
using UnityEngine;
using EvoLife.Common;
using EvoLife.Environment;

namespace EvoLife.Presentation
{
    /// <summary>
    /// Canonical demo biome layout for the research diorama (~150m footprint).
    /// Specialized zones are listed first so <see cref="BiomeMap"/> first-containing-wins
    /// keeps forest / wetland / rocky preferred over the grassland basin.
    /// Densities are presentation-tuned for readability (~250–350 edible plants), not
    /// a claim about proven performance at 80/12 population caps.
    /// </summary>
    public static class DemoBiomeLayout
    {
        /// <summary>Active ecosystem radius (~150m diameter visual footprint).</summary>
        public const float WorldRadius = 75f;

        /// <summary>Outer visual buffer beyond the logical spawn radius.</summary>
        public const float OuterBuffer = 15f;

        public const int WaterSourceCount = 3;

        public static readonly Vector3 ForestCenter = new Vector3(-32f, 0f, 42f);
        public const float ForestRadius = 34f;

        public static readonly Vector3 WetlandCenter = new Vector3(-42f, 0f, -28f);
        public const float WetlandRadius = 24f;

        public static readonly Vector3 RockyCenter = new Vector3(38f, 0f, -40f);
        public const float RockyRadius = 30f;

        public const float ElevationWetland = 0.04f;
        public const float ElevationGrassland = 0f;
        public const float ElevationForest = 0.08f;
        public const float ElevationRocky = 0.12f;

        public static readonly Vector3 OverviewCameraPosition = new Vector3(0f, 52f, -102f);
        public static readonly Vector3 OverviewCameraLookAt = Vector3.zero;

        public static List<BiomeZone> CreateZones()
        {
            return new List<BiomeZone>
            {
                BiomeZone.Create(
                    BiomeKind.Forest,
                    ForestCenter,
                    ForestRadius,
                    0.018f,
                    BiomeMap.DefaultRegenFor(BiomeKind.Forest),
                    BiomeMap.DefaultTemperatureFor(BiomeKind.Forest)),
                BiomeZone.Create(
                    BiomeKind.Wetland,
                    WetlandCenter,
                    WetlandRadius,
                    0.012f,
                    BiomeMap.DefaultRegenFor(BiomeKind.Wetland),
                    BiomeMap.DefaultTemperatureFor(BiomeKind.Wetland)),
                BiomeZone.Create(
                    BiomeKind.Rocky,
                    RockyCenter,
                    RockyRadius,
                    0.004f,
                    BiomeMap.DefaultRegenFor(BiomeKind.Rocky),
                    BiomeMap.DefaultTemperatureFor(BiomeKind.Rocky)),
                BiomeZone.Create(
                    BiomeKind.Grassland,
                    Vector3.zero,
                    WorldRadius,
                    0.011f,
                    BiomeMap.DefaultRegenFor(BiomeKind.Grassland),
                    BiomeMap.DefaultTemperatureFor(BiomeKind.Grassland))
            };
        }

        public static PlantSpawnSettings CreateSpawnSettings(int seed = 42)
        {
            return new PlantSpawnSettings
            {
                Seed = seed,
                WorldRadius = WorldRadius,
                DefaultDensity = 0.011f,
                MinSeparation = 2.2f,
                DefaultCapacity = 20f,
                DefaultRemaining = 20f,
                DefaultRegenPerSecond = 0.5f,
                DefaultRegenDelaySeconds = 2f,
                MaxPlacementAttempts = 28
            };
        }
    }
}
