using System.Collections.Generic;
using UnityEngine;
using EvoLife.Common;
using EvoLife.Environment;

namespace EvoLife.Presentation
{
    /// <summary>
    /// Canonical demo biome layout. Matches <see cref="BiomeMap"/> contracts;
    /// specialized zones are listed first so first-containing-wins stays correct.
    /// </summary>
    public static class DemoBiomeLayout
    {
        public const float WorldRadius = 28f;
        public const int WaterSourceCount = 2;

        public static List<BiomeZone> CreateZones()
        {
            return new List<BiomeZone>
            {
                BiomeZone.Create(
                    BiomeKind.Forest,
                    new Vector3(-16f, 0f, 14f),
                    13f,
                    BiomeMap.DefaultDensityFor(BiomeKind.Forest),
                    BiomeMap.DefaultRegenFor(BiomeKind.Forest),
                    BiomeMap.DefaultTemperatureFor(BiomeKind.Forest)),
                BiomeZone.Create(
                    BiomeKind.Wetland,
                    new Vector3(18f, 0f, 12f),
                    11f,
                    BiomeMap.DefaultDensityFor(BiomeKind.Wetland),
                    BiomeMap.DefaultRegenFor(BiomeKind.Wetland),
                    BiomeMap.DefaultTemperatureFor(BiomeKind.Wetland)),
                BiomeZone.Create(
                    BiomeKind.Rocky,
                    new Vector3(2f, 0f, -18f),
                    13f,
                    BiomeMap.DefaultDensityFor(BiomeKind.Rocky),
                    BiomeMap.DefaultRegenFor(BiomeKind.Rocky),
                    BiomeMap.DefaultTemperatureFor(BiomeKind.Rocky)),
                BiomeZone.Create(
                    BiomeKind.Grassland,
                    Vector3.zero,
                    WorldRadius,
                    BiomeMap.DefaultDensityFor(BiomeKind.Grassland),
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
                DefaultDensity = BiomeMap.DefaultDensityFor(BiomeKind.Grassland),
                MinSeparation = 1.6f,
                DefaultCapacity = 20f,
                DefaultRemaining = 20f,
                DefaultRegenPerSecond = 0.5f,
                DefaultRegenDelaySeconds = 2f,
                MaxPlacementAttempts = 24
            };
        }
    }
}
