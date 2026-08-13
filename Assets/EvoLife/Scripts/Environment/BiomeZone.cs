using System;
using UnityEngine;
using EvoLife.Common;

namespace EvoLife.Environment
{
    /// <summary>
    /// Circular logical biome. Does not create terrain meshes.
    /// </summary>
    [Serializable]
    public sealed class BiomeZone
    {
        [SerializeField] BiomeKind kind = BiomeKind.Grassland;
        [SerializeField] Vector3 center;
        [SerializeField] float radius = 10f;
        [SerializeField] float plantSpawnDensity = 0.04f;
        [SerializeField] float regenMultiplier = 1f;
        [SerializeField] float temperatureOffset;

        public BiomeKind Kind
        {
            get => kind;
            set => kind = value;
        }

        public Vector3 Center
        {
            get => center;
            set => center = value;
        }

        public float Radius
        {
            get => radius;
            set => radius = Mathf.Max(0f, value);
        }

        public float PlantSpawnDensity
        {
            get => plantSpawnDensity;
            set => plantSpawnDensity = Mathf.Max(0f, value);
        }

        public float RegenMultiplier
        {
            get => regenMultiplier;
            set => regenMultiplier = Mathf.Max(0f, value);
        }

        public float TemperatureOffset
        {
            get => temperatureOffset;
            set => temperatureOffset = value;
        }

        public float Area => Mathf.PI * Radius * Radius;

        public bool Contains(Vector3 position)
        {
            var offset = position - center;
            offset.y = 0f;
            return offset.sqrMagnitude <= Radius * Radius;
        }

        public static BiomeZone Create(
            BiomeKind biomeKind,
            Vector3 zoneCenter,
            float zoneRadius,
            float density = 0.04f,
            float regen = 1f,
            float temperature = 0f)
        {
            return new BiomeZone
            {
                Kind = biomeKind,
                Center = zoneCenter,
                Radius = zoneRadius,
                PlantSpawnDensity = density,
                RegenMultiplier = regen,
                TemperatureOffset = temperature
            };
        }
    }
}
