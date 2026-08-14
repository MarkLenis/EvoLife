using System.Collections.Generic;
using UnityEngine;
using EvoLife.Common;
using EvoLife.Environment;

namespace EvoLife.Presentation
{
    /// <summary>
    /// Organic ground patches matching logical <see cref="BiomeMap"/> zones plus
    /// non-colliding décor. Does not change biome mechanics. Visual edges are
    /// irregular on purpose; <see cref="BiomeZone"/> circles stay authoritative.
    /// </summary>
    public sealed class BiomeGroundPresenter : MonoBehaviour
    {
        [SerializeField] Transform worldRoot;
        [SerializeField] ResourceManager resourceManager;

        readonly List<Renderer> groundRenderers = new List<Renderer>();
        readonly List<Color> baseColors = new List<Color>();
        MaterialPropertyBlock propertyBlock;
        static readonly int ColorId = Shader.PropertyToID("_Color");
        bool built;
        float heatTint;

        MaterialPropertyBlock PropertyBlock => propertyBlock ??= new MaterialPropertyBlock();

        public int GroundCount => groundRenderers.Count;
        public float Lushness { get; private set; } = 1f;
        public float HeatTint => heatTint;

        public void Bind(ResourceManager manager, Transform root)
        {
            resourceManager = manager;
            worldRoot = root;
        }

        public void Build()
        {
            if (resourceManager == null)
            {
                return;
            }

            if (worldRoot == null)
            {
                worldRoot = transform;
            }

            Clear();
            var biomes = EnsureChild(worldRoot, "Biomes");

            CreatePatch(
                EnsureChild(biomes, "GrasslandVisual"),
                "Ground_Grassland",
                Vector3.zero,
                DemoBiomeLayout.WorldRadius * 1.08f,
                PresentationPalette.Grassland,
                PresentationMaterials.Grassland,
                DemoBiomeLayout.ElevationGrassland - 0.06f,
                seed: 11,
                irregularity: 0.11f,
                heightNoise: 0.10f,
                segments: 32);

            ScatterGrasslandVariation(EnsureChild(biomes, "GrasslandVisual"));

            var zones = resourceManager.Biomes != null ? resourceManager.Biomes.Zones : null;
            if (zones != null)
            {
                for (var i = 0; i < zones.Count; i++)
                {
                    var zone = zones[i];
                    if (zone == null || zone.Kind == BiomeKind.Grassland)
                    {
                        continue;
                    }

                    CreateOrganicBiome(EnsureChild(biomes, zone.Kind + "Visual"), zone);
                }
            }

            ScatterOuterLand(EnsureChild(worldRoot, "Background"));
            DemoWorldDecor.Build(worldRoot, resourceManager);
            built = true;
        }

        public void SetLushness(float value)
        {
            Lushness = Mathf.Clamp(value, 0.25f, 1.2f);
            ApplyGroundColors();
        }

        public void SetHeatTint(float value)
        {
            heatTint = Mathf.Clamp01(value);
            ApplyGroundColors();
        }

        void CreateOrganicBiome(Transform folder, BiomeZone zone)
        {
            var y = ElevationFor(zone.Kind);
            var height = HeightNoiseFor(zone.Kind);
            CreatePatch(
                folder,
                "Ground_" + zone.Kind,
                zone.Center,
                zone.Radius * 0.86f,
                PresentationPalette.ForBiome(zone.Kind),
                PresentationMaterials.ForBiome(zone.Kind),
                y,
                seed: 20 + (int)zone.Kind * 17,
                irregularity: 0.22f,
                heightNoise: height,
                segments: 28);

            var rng = new System.Random(40 + (int)zone.Kind * 13);
            var bleedCount = zone.Kind == BiomeKind.Forest ? 10 : 8;
            for (var i = 0; i < bleedCount; i++)
            {
                var angle = Rand(rng, 0f, Mathf.PI * 2f);
                var dist = zone.Radius * Rand(rng, 0.78f, 1.14f);
                var pos = zone.Center + new Vector3(Mathf.Cos(angle) * dist, 0f, Mathf.Sin(angle) * dist);
                CreatePatch(
                    folder,
                    "Bleed_" + zone.Kind + "_" + i,
                    pos,
                    Rand(rng, 5.5f, 12.5f),
                    PresentationPalette.ForBiome(zone.Kind),
                    PresentationMaterials.ForBiome(zone.Kind),
                    y + 0.01f,
                    seed: 80 + i + (int)zone.Kind * 9,
                    irregularity: 0.28f,
                    heightNoise: height * 0.6f,
                    segments: 16);
            }

            var biteCount = 6;
            for (var i = 0; i < biteCount; i++)
            {
                var angle = Rand(rng, 0f, Mathf.PI * 2f);
                var dist = zone.Radius * Rand(rng, 0.88f, 1.08f);
                var pos = zone.Center + new Vector3(Mathf.Cos(angle) * dist, 0f, Mathf.Sin(angle) * dist);
                CreatePatch(
                    folder,
                    "Bite_" + zone.Kind + "_" + i,
                    pos,
                    Rand(rng, 4.5f, 9.5f),
                    PresentationPalette.Grassland,
                    PresentationMaterials.Grassland,
                    DemoBiomeLayout.ElevationGrassland + 0.015f,
                    seed: 140 + i + (int)zone.Kind * 5,
                    irregularity: 0.30f,
                    heightNoise: 0.08f,
                    segments: 14);
            }

            AddBiomeAccentPatches(folder, zone, rng, y);
        }

        void AddBiomeAccentPatches(Transform folder, BiomeZone zone, System.Random rng, float y)
        {
            switch (zone.Kind)
            {
                case BiomeKind.Wetland:
                    for (var i = 0; i < 5; i++)
                    {
                        var pos = OffsetInRadius(rng, zone.Center, zone.Radius * 0.7f);
                        CreatePatch(
                            folder, "Mud_" + i, pos, Rand(rng, 3.5f, 7.5f),
                            PresentationPalette.WetMud, PresentationMaterials.WetMud,
                            y - 0.02f, 200 + i, 0.32f, 0.06f, 14);
                    }

                    break;
                case BiomeKind.Rocky:
                    for (var i = 0; i < 6; i++)
                    {
                        var pos = OffsetInRadius(rng, zone.Center, zone.Radius * 0.75f);
                        CreatePatch(
                            folder, "Earth_" + i, pos, Rand(rng, 3.2f, 7.0f),
                            PresentationPalette.DryEarth, PresentationMaterials.DryEarth,
                            y + 0.02f, 220 + i, 0.30f, 0.12f, 14);
                    }

                    break;
                case BiomeKind.Forest:
                    for (var i = 0; i < 5; i++)
                    {
                        var pos = OffsetInRadius(rng, zone.Center, zone.Radius * 0.65f);
                        CreatePatch(
                            folder, "Floor_" + i, pos, Rand(rng, 4.0f, 8.5f),
                            PresentationPalette.ForestFloor, PresentationMaterials.ForestFloor,
                            y + 0.02f, 240 + i, 0.26f, 0.18f, 14);
                    }

                    break;
                case BiomeKind.Grassland:
                    break;
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(zone), zone.Kind, "Unhandled BiomeKind.");
            }
        }

        void ScatterGrasslandVariation(Transform folder)
        {
            var rng = new System.Random(3);
            for (var i = 0; i < 7; i++)
            {
                var pos = OffsetInRadius(rng, Vector3.zero, 38f);
                var sunlit = i % 2 == 0;
                CreatePatch(
                    folder,
                    "GrassVar_" + i,
                    pos,
                    Rand(rng, 6f, 11f),
                    sunlit ? PresentationPalette.GrasslandSunlit : PresentationPalette.GrasslandShade,
                    sunlit ? PresentationMaterials.GrasslandSunlit : PresentationMaterials.GrasslandShade,
                    DemoBiomeLayout.ElevationGrassland - 0.02f,
                    seed: 300 + i,
                    irregularity: 0.28f,
                    heightNoise: 0.08f,
                    segments: 14);
            }
        }

        void ScatterOuterLand(Transform folder)
        {
            var rng = new System.Random(71);
            for (var i = 0; i < 14; i++)
            {
                var angle = (i / 14f) * Mathf.PI * 2f + Rand(rng, -0.35f, 0.35f);
                var dist = DemoBiomeLayout.WorldRadius + Rand(rng, -4f, 16f);
                var pos = new Vector3(Mathf.Cos(angle) * dist, 0f, Mathf.Sin(angle) * dist);
                CreatePatch(
                    folder,
                    "OuterPatch_" + i,
                    pos,
                    Rand(rng, 8f, 16f),
                    PresentationPalette.Grassland,
                    PresentationMaterials.Grassland,
                    DemoBiomeLayout.ElevationGrassland - 0.08f,
                    seed: 400 + i,
                    irregularity: 0.34f,
                    heightNoise: 0.16f,
                    segments: 16);
            }
        }

        void CreatePatch(
            Transform parent,
            string name,
            Vector3 center,
            float radius,
            Color color,
            Material material,
            float y,
            int seed,
            float irregularity,
            float heightNoise,
            int segments)
        {
            var mesh = PresentationGroundMesh.CreateIrregularDisc(radius, segments, irregularity, seed, heightNoise);
            var go = PresentationPrimitives.CreateMeshChild(
                parent,
                name,
                mesh,
                new Vector3(center.x, y, center.z),
                material,
                receiveShadows: true);
            TrackGround(go, color);
        }

        void ApplyGroundColors()
        {
            for (var i = 0; i < groundRenderers.Count; i++)
            {
                var renderer = groundRenderers[i];
                if (renderer == null)
                {
                    continue;
                }

                var color = Color.Lerp(PresentationPalette.Drought, baseColors[i], Mathf.Clamp01(Lushness));
                color = Color.Lerp(color, PresentationPalette.HeatWave, heatTint * 0.34f);
                var block = PropertyBlock;
                renderer.GetPropertyBlock(block);
                block.SetColor(ColorId, color);
                renderer.SetPropertyBlock(block);
            }
        }

        void TrackGround(GameObject go, Color color)
        {
            if (go == null)
            {
                return;
            }

            var renderer = go.GetComponent<Renderer>();
            if (renderer == null)
            {
                return;
            }

            groundRenderers.Add(renderer);
            baseColors.Add(color);
            var block = PropertyBlock;
            renderer.GetPropertyBlock(block);
            block.SetColor(ColorId, color);
            renderer.SetPropertyBlock(block);
        }

        static float ElevationFor(BiomeKind kind)
        {
            switch (kind)
            {
                case BiomeKind.Wetland:
                    return DemoBiomeLayout.ElevationWetland;
                case BiomeKind.Forest:
                    return DemoBiomeLayout.ElevationForest;
                case BiomeKind.Rocky:
                    return DemoBiomeLayout.ElevationRocky;
                case BiomeKind.Grassland:
                    return DemoBiomeLayout.ElevationGrassland;
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(kind), kind, "Unhandled BiomeKind.");
            }
        }

        static float HeightNoiseFor(BiomeKind kind)
        {
            switch (kind)
            {
                case BiomeKind.Wetland:
                    return 0.08f;
                case BiomeKind.Forest:
                    return 0.28f;
                case BiomeKind.Rocky:
                    return 0.38f;
                case BiomeKind.Grassland:
                    return 0.10f;
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(kind), kind, "Unhandled BiomeKind.");
            }
        }

        static Vector3 OffsetInRadius(System.Random rng, Vector3 center, float radius)
        {
            var angle = Rand(rng, 0f, Mathf.PI * 2f);
            var dist = radius * Mathf.Sqrt(Rand(rng, 0f, 1f));
            return center + new Vector3(Mathf.Cos(angle) * dist, 0f, Mathf.Sin(angle) * dist);
        }

        static float Rand(System.Random rng, float min, float max) =>
            min + (float)rng.NextDouble() * (max - min);

        static Transform EnsureChild(Transform parent, string name)
        {
            var existing = parent.Find(name);
            if (existing != null)
            {
                return existing;
            }

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        void Clear()
        {
            groundRenderers.Clear();
            baseColors.Clear();
            heatTint = 0f;
            if (worldRoot == null || !built)
            {
                return;
            }

            for (var i = worldRoot.childCount - 1; i >= 0; i--)
            {
                var child = worldRoot.GetChild(i);
                if (child == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }
    }
}
