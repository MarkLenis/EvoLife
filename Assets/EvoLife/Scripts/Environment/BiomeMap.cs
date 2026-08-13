using System;
using System.Collections.Generic;
using UnityEngine;
using EvoLife.Common;

namespace EvoLife.Environment
{
    /// <summary>
    /// Resolves a world position to a logical biome. First containing zone wins; else default.
    /// </summary>
    public sealed class BiomeMap
    {
        readonly List<BiomeZone> zones = new List<BiomeZone>();
        BiomeKind defaultBiome = BiomeKind.Grassland;
        float defaultRegenMultiplier = 1f;
        float defaultTemperatureOffset;
        float defaultPlantSpawnDensity = 0.04f;

        public IReadOnlyList<BiomeZone> Zones => zones;
        public BiomeKind DefaultBiome => defaultBiome;
        public float DefaultRegenMultiplier => defaultRegenMultiplier;
        public float DefaultTemperatureOffset => defaultTemperatureOffset;
        public float DefaultPlantSpawnDensity => defaultPlantSpawnDensity;

        public void ConfigureDefaults(
            BiomeKind biome,
            float regenMultiplier = 1f,
            float temperatureOffset = 0f,
            float plantSpawnDensity = 0.04f)
        {
            defaultBiome = biome;
            defaultRegenMultiplier = regenMultiplier < 0f ? 0f : regenMultiplier;
            defaultTemperatureOffset = temperatureOffset;
            defaultPlantSpawnDensity = plantSpawnDensity < 0f ? 0f : plantSpawnDensity;
        }

        public void ReplaceZones(IEnumerable<BiomeZone> next)
        {
            zones.Clear();
            if (next == null)
            {
                return;
            }

            foreach (var zone in next)
            {
                if (zone != null)
                {
                    zones.Add(zone);
                }
            }
        }

        public void AddZone(BiomeZone zone)
        {
            if (zone != null)
            {
                zones.Add(zone);
            }
        }

        public BiomeZone ResolveZone(Vector3 position)
        {
            for (var i = 0; i < zones.Count; i++)
            {
                if (zones[i] != null && zones[i].Contains(position))
                {
                    return zones[i];
                }
            }

            return null;
        }

        public BiomeKind ResolveKind(Vector3 position)
        {
            var zone = ResolveZone(position);
            return zone != null ? zone.Kind : defaultBiome;
        }

        public float RegenMultiplierAt(Vector3 position)
        {
            var zone = ResolveZone(position);
            return zone != null ? zone.RegenMultiplier : defaultRegenMultiplier;
        }

        public float TemperatureOffsetAt(Vector3 position)
        {
            var zone = ResolveZone(position);
            return zone != null ? zone.TemperatureOffset : defaultTemperatureOffset;
        }

        public float PlantSpawnDensityAt(Vector3 position)
        {
            var zone = ResolveZone(position);
            return zone != null ? zone.PlantSpawnDensity : defaultPlantSpawnDensity;
        }

        public float MeanTemperatureOffset()
        {
            if (zones.Count == 0)
            {
                return defaultTemperatureOffset;
            }

            var sum = 0f;
            var n = 0;
            for (var i = 0; i < zones.Count; i++)
            {
                if (zones[i] == null)
                {
                    continue;
                }

                sum += zones[i].TemperatureOffset;
                n++;
            }

            return n == 0 ? defaultTemperatureOffset : sum / n;
        }

        public static float DefaultTemperatureFor(BiomeKind kind)
        {
            switch (kind)
            {
                case BiomeKind.Grassland:
                    return 0.45f;
                case BiomeKind.Forest:
                    return 0.4f;
                case BiomeKind.Wetland:
                    return 0.35f;
                case BiomeKind.Rocky:
                    return 0.65f;
                default:
                    return Unreachable(kind);
            }
        }

        public static float DefaultRegenFor(BiomeKind kind)
        {
            switch (kind)
            {
                case BiomeKind.Grassland:
                    return 1f;
                case BiomeKind.Forest:
                    return 1.25f;
                case BiomeKind.Wetland:
                    return 1.1f;
                case BiomeKind.Rocky:
                    return 0.45f;
                default:
                    return Unreachable(kind);
            }
        }

        public static float DefaultDensityFor(BiomeKind kind)
        {
            switch (kind)
            {
                case BiomeKind.Grassland:
                    return 0.05f;
                case BiomeKind.Forest:
                    return 0.08f;
                case BiomeKind.Wetland:
                    return 0.03f;
                case BiomeKind.Rocky:
                    return 0.015f;
                default:
                    return Unreachable(kind);
            }
        }

        static float Unreachable(BiomeKind kind)
        {
            throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unhandled BiomeKind.");
        }
    }
}
