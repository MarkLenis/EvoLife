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
        static Material grasslandSunlit;
        static Material grasslandShade;
        static Material forest;
        static Material forestFloor;
        static Material canopy;
        static Material canopyAlt;
        static Material wetland;
        static Material wetMud;
        static Material rocky;
        static Material dryEarth;
        static Material outerBuffer;
        static Material water;
        static Material plantHealthy;
        static Material plantDepleted;
        static Material decorativeGrass;
        static Material dryGrass;
        static Material trunk;
        static Material stone;
        static Material stoneDark;
        static Material reed;
        static Material flower;
        static Material herbivore;
        static Material herbivoreAccent;
        static Material predator;
        static Material predatorAccent;
        static Material wildfire;
        static Material smoke;

        public static Material Grassland => grassland != null ? grassland : (grassland = CreateOpaque(PresentationPalette.Grassland));
        public static Material GrasslandSunlit => grasslandSunlit != null ? grasslandSunlit : (grasslandSunlit = CreateOpaque(PresentationPalette.GrasslandSunlit));
        public static Material GrasslandShade => grasslandShade != null ? grasslandShade : (grasslandShade = CreateOpaque(PresentationPalette.GrasslandShade));
        public static Material Forest => forest != null ? forest : (forest = CreateOpaque(PresentationPalette.Forest));
        public static Material ForestFloor => forestFloor != null ? forestFloor : (forestFloor = CreateOpaque(PresentationPalette.ForestFloor));
        public static Material Canopy => canopy != null ? canopy : (canopy = CreateOpaque(PresentationPalette.Canopy));
        public static Material CanopyAlt => canopyAlt != null ? canopyAlt : (canopyAlt = CreateOpaque(PresentationPalette.CanopyAlt));
        public static Material Wetland => wetland != null ? wetland : (wetland = CreateOpaque(PresentationPalette.Wetland));
        public static Material WetMud => wetMud != null ? wetMud : (wetMud = CreateOpaque(PresentationPalette.WetMud));
        public static Material Rocky => rocky != null ? rocky : (rocky = CreateOpaque(PresentationPalette.Rocky));
        public static Material DryEarth => dryEarth != null ? dryEarth : (dryEarth = CreateOpaque(PresentationPalette.DryEarth));
        public static Material OuterBuffer => outerBuffer != null ? outerBuffer : (outerBuffer = CreateOpaque(PresentationPalette.OuterBuffer));
        public static Material Water => water != null ? water : (water = CreateWater(PresentationPalette.Water));
        public static Material PlantHealthy => plantHealthy != null ? plantHealthy : (plantHealthy = CreateOpaque(PresentationPalette.PlantHealthy));
        public static Material PlantDepleted => plantDepleted != null ? plantDepleted : (plantDepleted = CreateOpaque(PresentationPalette.PlantDepleted));
        public static Material DecorativeGrass => decorativeGrass != null ? decorativeGrass : (decorativeGrass = CreateOpaque(PresentationPalette.DecorativeGrass));
        public static Material DryGrass => dryGrass != null ? dryGrass : (dryGrass = CreateOpaque(PresentationPalette.DryGrass));
        public static Material Trunk => trunk != null ? trunk : (trunk = CreateOpaque(PresentationPalette.Trunk));
        public static Material Stone => stone != null ? stone : (stone = CreateOpaque(PresentationPalette.Stone));
        public static Material StoneDark => stoneDark != null ? stoneDark : (stoneDark = CreateOpaque(PresentationPalette.StoneDark));
        public static Material Reed => reed != null ? reed : (reed = CreateOpaque(PresentationPalette.Reed));
        public static Material Flower => flower != null ? flower : (flower = CreateOpaque(PresentationPalette.Flower));
        public static Material Herbivore => herbivore != null ? herbivore : (herbivore = CreateOpaque(PresentationPalette.Herbivore));
        public static Material HerbivoreAccent => herbivoreAccent != null ? herbivoreAccent : (herbivoreAccent = CreateOpaque(PresentationPalette.HerbivoreAccent));
        public static Material Predator => predator != null ? predator : (predator = CreateOpaque(PresentationPalette.Predator));
        public static Material PredatorAccent => predatorAccent != null ? predatorAccent : (predatorAccent = CreateOpaque(PresentationPalette.PredatorAccent));
        public static Material Wildfire => wildfire != null ? wildfire : (wildfire = CreateOpaque(PresentationPalette.Wildfire));
        public static Material Smoke => smoke != null ? smoke : (smoke = CreateWater(new Color(0.38f, 0.36f, 0.34f, 0.32f)));

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
