using UnityEngine;
using UnityEngine.Rendering;

namespace EvoLife.Presentation
{
    /// <summary>
    /// Shared materials for the demo world. Uses EvoLife shaders when imported,
    /// otherwise Built-in Unlit/Color. Callers must assign <c>sharedMaterial</c>
    /// (never <c>material</c>) to avoid per-renderer instancing.
    /// </summary>
    public static class PresentationMaterials
    {
        const string ColorShader = "EvoLife/StylizedColor";
        const string WaterShader = "EvoLife/StylizedWater";
        const string FallbackShader = "Unlit/Color";

        static Material grassland;
        static Material forest;
        static Material wetland;
        static Material rocky;
        static Material water;
        static Material plantHealthy;
        static Material plantDepleted;
        static Material herbivore;
        static Material herbivoreAccent;
        static Material predator;
        static Material predatorAccent;
        static Material wildfire;
        static Material smoke;

        public static Material Grassland => grassland != null ? grassland : (grassland = CreateOpaque(PresentationPalette.Grassland));
        public static Material Forest => forest != null ? forest : (forest = CreateOpaque(PresentationPalette.Forest));
        public static Material Wetland => wetland != null ? wetland : (wetland = CreateOpaque(PresentationPalette.Wetland));
        public static Material Rocky => rocky != null ? rocky : (rocky = CreateOpaque(PresentationPalette.Rocky));
        public static Material Water => water != null ? water : (water = CreateWater(PresentationPalette.Water));
        public static Material PlantHealthy => plantHealthy != null ? plantHealthy : (plantHealthy = CreateOpaque(PresentationPalette.PlantHealthy));
        public static Material PlantDepleted => plantDepleted != null ? plantDepleted : (plantDepleted = CreateOpaque(PresentationPalette.PlantDepleted));
        public static Material Herbivore => herbivore != null ? herbivore : (herbivore = CreateOpaque(PresentationPalette.Herbivore));
        public static Material HerbivoreAccent => herbivoreAccent != null ? herbivoreAccent : (herbivoreAccent = CreateOpaque(PresentationPalette.HerbivoreAccent));
        public static Material Predator => predator != null ? predator : (predator = CreateOpaque(PresentationPalette.Predator));
        public static Material PredatorAccent => predatorAccent != null ? predatorAccent : (predatorAccent = CreateOpaque(PresentationPalette.PredatorAccent));
        public static Material Wildfire => wildfire != null ? wildfire : (wildfire = CreateOpaque(PresentationPalette.Wildfire));
        public static Material Smoke => smoke != null ? smoke : (smoke = CreateWater(new Color(0.35f, 0.32f, 0.3f, 0.35f)));

        public static Material ForBiome(EvoLife.Common.BiomeKind kind)
        {
            switch (kind)
            {
                case EvoLife.Common.BiomeKind.Grassland:
                    return Grassland;
                case EvoLife.Common.BiomeKind.Forest:
                    return Forest;
                case EvoLife.Common.BiomeKind.Wetland:
                    return Wetland;
                case EvoLife.Common.BiomeKind.Rocky:
                    return Rocky;
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(kind), kind, "Unhandled BiomeKind.");
            }
        }

        public static Material ForRole(EvoLife.Common.CreatureRole role)
        {
            switch (role)
            {
                case EvoLife.Common.CreatureRole.Herbivore:
                    return Herbivore;
                case EvoLife.Common.CreatureRole.Predator:
                    return Predator;
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(role), role, "Unhandled CreatureRole.");
            }
        }

        public static Material AccentForRole(EvoLife.Common.CreatureRole role)
        {
            switch (role)
            {
                case EvoLife.Common.CreatureRole.Herbivore:
                    return HerbivoreAccent;
                case EvoLife.Common.CreatureRole.Predator:
                    return PredatorAccent;
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(role), role, "Unhandled CreatureRole.");
            }
        }

        static Material CreateOpaque(Color color)
        {
            var shader = Shader.Find(ColorShader) ?? Shader.Find(FallbackShader) ?? Shader.Find("Standard");
            return Finish(shader, color, true);
        }

        static Material CreateWater(Color color)
        {
            var shader = Shader.Find(WaterShader) ?? Shader.Find(ColorShader) ?? Shader.Find(FallbackShader);
            return Finish(shader, color, false);
        }

        static Material Finish(Shader shader, Color color, bool opaque)
        {
            if (shader == null)
            {
                return null;
            }

            var material = new Material(shader)
            {
                name = "EvoLifePresentation",
                hideFlags = HideFlags.HideAndDontSave,
                enableInstancing = opaque,
                color = color
            };
            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            if (!opaque)
            {
                material.renderQueue = (int)RenderQueue.Transparent;
            }

            return material;
        }
    }
}
