using System.Collections.Generic;
using UnityEngine;
using EvoLife.Common;
using EvoLife.Environment;

namespace EvoLife.Presentation
{
    /// <summary>
    /// Colored ground discs matching logical <see cref="BiomeMap"/> zones.
    /// Does not change biome mechanics.
    /// </summary>
    public sealed class BiomeGroundPresenter : MonoBehaviour
    {
        [SerializeField] Transform worldRoot;
        [SerializeField] ResourceManager resourceManager;

        readonly List<Renderer> groundRenderers = new List<Renderer>();
        readonly List<Color> baseColors = new List<Color>();
        readonly MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
        static readonly int ColorId = Shader.PropertyToID("_Color");
        bool built;

        public int GroundCount => groundRenderers.Count;
        public float Lushness { get; private set; } = 1f;

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
            var radius = resourceManager.SpawnSettings.WorldRadius;
            CreateDisc("Ground_Grassland", Vector3.zero, radius, BiomeKind.Grassland, 0f);
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

                    CreateDisc(
                        "Ground_" + zone.Kind,
                        zone.Center,
                        zone.Radius,
                        zone.Kind,
                        0.02f);
                }
            }

            AddForestDecor();
            built = true;
        }

        public void SetLushness(float value)
        {
            Lushness = Mathf.Clamp(value, 0.25f, 1.2f);
            for (var i = 0; i < groundRenderers.Count; i++)
            {
                var renderer = groundRenderers[i];
                if (renderer == null)
                {
                    continue;
                }

                var color = Color.Lerp(PresentationPalette.Drought, baseColors[i], Mathf.Clamp01(Lushness));
                renderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor(ColorId, color);
                renderer.SetPropertyBlock(propertyBlock);
            }
        }

        void CreateDisc(string name, Vector3 center, float radius, BiomeKind kind, float y)
        {
            var diameter = Mathf.Max(0.5f, radius * 2f);
            var color = PresentationPalette.ForBiome(kind);
            var go = PresentationPrimitives.CreateChild(
                worldRoot, name, PrimitiveType.Cylinder,
                new Vector3(center.x, y - 0.04f, center.z),
                new Vector3(diameter, 0.02f, diameter),
                PresentationMaterials.ForBiome(kind));
            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                groundRenderers.Add(renderer);
                baseColors.Add(color);
                renderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor(ColorId, color);
                renderer.SetPropertyBlock(propertyBlock);
            }
        }

        void AddForestDecor()
        {
            var zones = resourceManager.Biomes != null ? resourceManager.Biomes.Zones : null;
            if (zones == null)
            {
                return;
            }

            for (var i = 0; i < zones.Count; i++)
            {
                var zone = zones[i];
                if (zone == null || zone.Kind != BiomeKind.Forest)
                {
                    continue;
                }

                var parent = new GameObject("Decor_ForestTrees").transform;
                parent.SetParent(worldRoot, false);
                parent.position = zone.Center;
                var count = 10;
                for (var n = 0; n < count; n++)
                {
                    var angle = (n / (float)count) * Mathf.PI * 2f;
                    var distance = zone.Radius * 0.55f;
                    var pos = new Vector3(Mathf.Cos(angle) * distance, 0f, Mathf.Sin(angle) * distance);
                    PresentationPrimitives.CreateChild(
                        parent, "Trunk", PrimitiveType.Cylinder,
                        pos + new Vector3(0f, 0.7f, 0f), new Vector3(0.18f, 0.7f, 0.18f),
                        PresentationMaterials.PlantDepleted);
                    PresentationPrimitives.CreateChild(
                        parent, "Canopy", PrimitiveType.Sphere,
                        pos + new Vector3(0f, 1.5f, 0f), new Vector3(1.1f, 0.9f, 1.1f),
                        PresentationMaterials.Forest);
                }
            }
        }

        void Clear()
        {
            groundRenderers.Clear();
            baseColors.Clear();
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
