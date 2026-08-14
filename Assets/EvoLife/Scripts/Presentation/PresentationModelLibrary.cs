using UnityEngine;

namespace EvoLife.Presentation
{
    /// <summary>
    /// Loads curated Kenney CC0 meshes from Resources. Falls back to primitives when missing
    /// so EditMode tests and training scenes stay safe.
    /// </summary>
    public static class PresentationModelLibrary
    {
        public static readonly string[] ForestTrees =
        {
            "EvoLifeModels/Trees/tree_oak",
            "EvoLifeModels/Trees/tree_oak_dark",
            "EvoLifeModels/Trees/tree_detailed",
            "EvoLifeModels/Trees/tree_detailed_dark",
            "EvoLifeModels/Trees/tree_pineTallA_detailed",
            "EvoLifeModels/Trees/tree_pineTallB_detailed",
            "EvoLifeModels/Trees/tree_pineTallC_detailed",
            "EvoLifeModels/Trees/tree_pineRoundA",
            "EvoLifeModels/Trees/tree_tall",
            "EvoLifeModels/Trees/tree_tall_dark",
            "EvoLifeModels/Trees/tree_fat",
            "EvoLifeModels/Trees/tree_simple"
        };

        public static readonly string[] EdgeTrees =
        {
            "EvoLifeModels/Trees/tree_small",
            "EvoLifeModels/Trees/tree_thin",
            "EvoLifeModels/Trees/tree_cone",
            "EvoLifeModels/Trees/tree_simple"
        };

        public static readonly string[] Bushes =
        {
            "EvoLifeModels/Plants/plant_bush",
            "EvoLifeModels/Plants/plant_bushDetailed",
            "EvoLifeModels/Plants/plant_bushLarge",
            "EvoLifeModels/Plants/plant_bushSmall",
            "EvoLifeModels/Plants/plant_bushTriangle"
        };

        public static readonly string[] LargeRocks =
        {
            "EvoLifeModels/Rocks/rock_largeA",
            "EvoLifeModels/Rocks/rock_largeB",
            "EvoLifeModels/Rocks/rock_largeC",
            "EvoLifeModels/Rocks/rock_largeD",
            "EvoLifeModels/Rocks/rock_tallA",
            "EvoLifeModels/Rocks/rock_tallB"
        };

        public static readonly string[] SmallRocks =
        {
            "EvoLifeModels/Rocks/rock_smallA",
            "EvoLifeModels/Rocks/rock_smallB",
            "EvoLifeModels/Rocks/rock_smallC",
            "EvoLifeModels/Rocks/rock_smallD",
            "EvoLifeModels/Rocks/rock_smallE",
            "EvoLifeModels/Rocks/stone_largeA"
        };

        public const string Herbivore = "EvoLifeModels/Creatures/animal-deer";
        public const string Predator = "EvoLifeModels/Creatures/animal-fox";
        public const string EdiblePlant = "EvoLifeModels/Plants/plant_bushSmall";
        public const string EdiblePlantAlt = "EvoLifeModels/Plants/crops_leafsStageB";
        public const string Grass = "EvoLifeModels/Plants/grass";
        public const string GrassLarge = "EvoLifeModels/Plants/grass_large";
        public const string Reed = "EvoLifeModels/Plants/plant_flatTall";
        public const string LilyLarge = "EvoLifeModels/Plants/lily_large";
        public const string LilySmall = "EvoLifeModels/Plants/lily_small";
        public const string FlowerA = "EvoLifeModels/Plants/flower_yellowA";
        public const string FlowerB = "EvoLifeModels/Plants/flower_redA";
        public const string Log = "EvoLifeModels/Plants/log";
        public const string Stump = "EvoLifeModels/Plants/stump_round";
        public const string CactusShort = "EvoLifeModels/Rocks/cactus_short";
        public const string CactusTall = "EvoLifeModels/Rocks/cactus_tall";

        public static GameObject Load(string resourcesPath)
        {
            if (string.IsNullOrEmpty(resourcesPath))
            {
                return null;
            }

            return Resources.Load<GameObject>(resourcesPath);
        }

        public static GameObject LoadRandom(string[] paths, System.Random rng)
        {
            if (paths == null || paths.Length == 0 || rng == null)
            {
                return null;
            }

            return Load(paths[rng.Next(paths.Length)]);
        }

        public static GameObject Spawn(
            Transform parent,
            string name,
            GameObject source,
            Vector3 localPosition,
            float uniformScale,
            Vector3 localEuler,
            bool castShadows)
        {
            if (source == null)
            {
                return null;
            }

            var go = Object.Instantiate(source, parent);
            go.name = name;
            go.transform.localPosition = localPosition;
            go.transform.localRotation = Quaternion.Euler(localEuler);
            go.transform.localScale = new Vector3(uniformScale, uniformScale, uniformScale);
            PresentationPrimitives.PrepareImportedVisual(go, castShadows);
            return go;
        }

        public static bool TrySpawn(
            Transform parent,
            string name,
            string resourcesPath,
            Vector3 localPosition,
            float uniformScale,
            Vector3 localEuler,
            bool castShadows,
            out GameObject spawned)
        {
            spawned = Spawn(parent, name, Load(resourcesPath), localPosition, uniformScale, localEuler, castShadows);
            return spawned != null;
        }
    }
}
