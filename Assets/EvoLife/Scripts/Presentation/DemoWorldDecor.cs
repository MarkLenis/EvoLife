using UnityEngine;
using EvoLife.Common;
using EvoLife.Environment;

namespace EvoLife.Presentation
{
    /// <summary>
    /// Non-colliding decorative props and landmarks. Never owns resources or AI.
    /// Prefers Kenney CC0 meshes; primitives are only a fallback.
    /// </summary>
    public static class DemoWorldDecor
    {
        static readonly Vector3[] ForestClusters =
        {
            new Vector3(0f, 0f, 0f),
            new Vector3(-12f, 0f, 8f),
            new Vector3(-8f, 0f, 16f),
            new Vector3(6f, 0f, 14f),
            new Vector3(-18f, 0f, -4f),
            new Vector3(10f, 0f, 2f),
            new Vector3(-6f, 0f, -12f),
            new Vector3(-20f, 0f, 12f),
            new Vector3(2f, 0f, 22f),
            new Vector3(-14f, 0f, -14f)
        };

        public static void Build(Transform worldRoot, ResourceManager resourceManager)
        {
            if (worldRoot == null)
            {
                return;
            }

            var decorations = EnsureChild(worldRoot, "Decorations");
            var trees = EnsureChild(decorations, "Trees");
            var rocks = EnsureChild(decorations, "Rocks");
            var grass = EnsureChild(decorations, "Grass");
            var reeds = EnsureChild(decorations, "Reeds");
            var landmarks = EnsureChild(worldRoot, "Landmarks");
            var background = EnsureChild(worldRoot, "Background");

            BuildForestTrees(trees);
            BuildForestUndergrowth(EnsureChild(decorations, "Undergrowth"));
            BuildRockyComposition(rocks);
            BuildGrasslandScatter(grass);
            BuildWetlandShore(reeds);
            BuildLandmarks(landmarks);
            BuildOuterHills(background);

            if (resourceManager != null)
            {
                // ResourceManager stays authoritative for plants/water.
            }
        }

        static void BuildForestTrees(Transform parent)
        {
            var center = DemoBiomeLayout.ForestCenter;
            var rng = new System.Random(17);

            for (var c = 0; c < ForestClusters.Length; c++)
            {
                var cluster = center + ForestClusters[c];
                var towardGrassland = ForestClusters[c].x > 4f && ForestClusters[c].z < 4f;
                var treesInCluster = towardGrassland ? 3 : (c == 0 ? 11 : 7);
                for (var i = 0; i < treesInCluster; i++)
                {
                    var spread = towardGrassland ? 3.5f : 5.4f;
                    var pos = cluster + new Vector3(Rand(rng, -spread, spread), 0f, Rand(rng, -spread, spread));
                    if ((pos - center).magnitude > DemoBiomeLayout.ForestRadius * 0.95f)
                    {
                        continue;
                    }

                    SpawnTree(parent, "Tree_" + c + "_" + i, pos, Rand(rng, 6.8f, 9.4f), rng, false);
                }
            }

            for (var i = 0; i < 16; i++)
            {
                var angle = Rand(rng, 0f, Mathf.PI * 2f);
                if (Mathf.Cos(angle) > 0.35f && Mathf.Sin(angle) < 0.15f)
                {
                    continue;
                }

                var dist = DemoBiomeLayout.ForestRadius * Rand(rng, 0.88f, 1.14f);
                var pos = center + new Vector3(Mathf.Cos(angle) * dist, 0f, Mathf.Sin(angle) * dist);
                SpawnTree(parent, "TreeEdge_" + i, pos, Rand(rng, 4.6f, 6.8f), rng, true);
            }
        }

        static void BuildForestUndergrowth(Transform parent)
        {
            var center = DemoBiomeLayout.ForestCenter;
            var rng = new System.Random(19);
            for (var i = 0; i < 28; i++)
            {
                var pos = OffsetInRadius(rng, center, DemoBiomeLayout.ForestRadius * 0.78f);
                if (!PresentationModelLibrary.TrySpawn(
                    parent, "Bush_" + i, Pick(rng, PresentationModelLibrary.Bushes),
                    pos, Rand(rng, 2.4f, 3.8f), new Vector3(0f, Rand(rng, 0f, 360f), 0f), false, out _))
                {
                    PresentationPrimitives.CreateChild(
                        parent, "Bush_" + i, PrimitiveType.Sphere,
                        pos + new Vector3(0f, 0.45f, 0f),
                        new Vector3(Rand(rng, 0.7f, 1.3f), Rand(rng, 0.45f, 0.7f), Rand(rng, 0.7f, 1.3f)),
                        PresentationMaterials.Canopy);
                }
            }

            PresentationModelLibrary.TrySpawn(
                parent, "FallenLog", PresentationModelLibrary.Log,
                center + new Vector3(6f, 0f, -4f), 2.2f, new Vector3(0f, 35f, 0f), false, out _);
        }

        static void BuildRockyComposition(Transform parent)
        {
            var center = DemoBiomeLayout.RockyCenter;
            var rng = new System.Random(29);
            SpawnRockFormation(parent, "FormationA", center + new Vector3(-2f, 0f, 1f), rng, 5.4f);
            SpawnRockFormation(parent, "FormationB", center + new Vector3(-11f, 0f, 8f), rng, 4.6f);
            SpawnRockFormation(parent, "FormationC", center + new Vector3(10f, 0f, -7f), rng, 5.0f);

            for (var i = 0; i < 14; i++)
            {
                var pos = OffsetInRadius(rng, center, DemoBiomeLayout.RockyRadius * 0.72f);
                if (!PresentationModelLibrary.TrySpawn(
                    parent, "Rubble_" + i, Pick(rng, PresentationModelLibrary.SmallRocks),
                    pos, Rand(rng, 2.4f, 3.6f), new Vector3(0f, Rand(rng, 0f, 360f), 0f), true, out _))
                {
                    PresentationPrimitives.CreateChild(
                        parent, "Rubble_" + i, PrimitiveType.Sphere,
                        pos + new Vector3(0f, 0.3f, 0f),
                        new Vector3(0.8f, 0.45f, 0.7f),
                        PresentationMaterials.Stone,
                        new Vector3(0f, Rand(rng, 0f, 360f), 0f),
                        true);
                }
            }

            for (var i = 0; i < 10; i++)
            {
                var pos = OffsetInRadius(rng, center, DemoBiomeLayout.RockyRadius * 0.8f);
                PresentationModelLibrary.TrySpawn(
                    parent, "DryGrass_" + i, PresentationModelLibrary.Grass,
                    pos, Rand(rng, 1.4f, 2.0f), new Vector3(0f, Rand(rng, 0f, 360f), 0f), false, out _);
            }

            PresentationModelLibrary.TrySpawn(
                parent, "CactusA", PresentationModelLibrary.CactusTall,
                center + new Vector3(4f, 0f, 6f), 3.6f, new Vector3(0f, 20f, 0f), false, out _);
            PresentationModelLibrary.TrySpawn(
                parent, "CactusB", PresentationModelLibrary.CactusShort,
                center + new Vector3(-6f, 0f, -5f), 3.2f, new Vector3(0f, 80f, 0f), false, out _);
        }

        static void SpawnRockFormation(Transform parent, string name, Vector3 origin, System.Random rng, float scale)
        {
            if (!PresentationModelLibrary.TrySpawn(
                parent, name + "_Core", Pick(rng, PresentationModelLibrary.LargeRocks),
                origin, scale, new Vector3(0f, Rand(rng, 0f, 360f), 0f), true, out _))
            {
                PresentationPrimitives.CreateChild(
                    parent, name + "_Core", PrimitiveType.Sphere,
                    origin + new Vector3(0f, 1.4f * scale, 0f),
                    new Vector3(4.2f * scale, 2.8f * scale, 3.6f * scale),
                    PresentationMaterials.StoneDark,
                    default,
                    true);
            }

            for (var i = 0; i < 3; i++)
            {
                var offset = origin + new Vector3(Rand(rng, -3.5f, 3.5f), 0f, Rand(rng, -3.5f, 3.5f));
                PresentationModelLibrary.TrySpawn(
                    parent, name + "_Sat_" + i, Pick(rng, PresentationModelLibrary.SmallRocks),
                    offset, scale * Rand(rng, 0.55f, 0.85f), new Vector3(0f, Rand(rng, 0f, 360f), 0f), true, out _);
            }
        }

        static void BuildGrasslandScatter(Transform parent)
        {
            var rng = new System.Random(41);
            for (var i = 0; i < 36; i++)
            {
                var pos = OffsetInRadius(rng, Vector3.zero, 52f);
                if (InSpecializedBiome(pos, 0.72f))
                {
                    continue;
                }

                if (!PresentationModelLibrary.TrySpawn(
                    parent, "Tuft_" + i, rng.NextDouble() < 0.4 ? PresentationModelLibrary.GrassLarge : PresentationModelLibrary.Grass,
                    pos, Rand(rng, 1.3f, 1.9f), new Vector3(0f, Rand(rng, 0f, 360f), 0f), false, out _))
                {
                    PresentationPrimitives.CreateChild(
                        parent, "Tuft_" + i, PrimitiveType.Cylinder,
                        pos + new Vector3(0f, 0.22f, 0f),
                        new Vector3(0.12f, 0.22f, 0.12f),
                        PresentationMaterials.DecorativeGrass);
                }
            }

            for (var i = 0; i < 12; i++)
            {
                var pos = OffsetInRadius(rng, Vector3.zero, 46f);
                if (InSpecializedBiome(pos, 0.78f))
                {
                    continue;
                }

                var flower = i % 2 == 0 ? PresentationModelLibrary.FlowerA : PresentationModelLibrary.FlowerB;
                if (!PresentationModelLibrary.TrySpawn(
                    parent, "Flower_" + i, flower, pos, Rand(rng, 1.5f, 2.1f),
                    new Vector3(0f, Rand(rng, 0f, 360f), 0f), false, out _))
                {
                    PresentationPrimitives.CreateChild(
                        parent, "Flower_" + i, PrimitiveType.Sphere,
                        pos + new Vector3(0f, 0.28f, 0f),
                        new Vector3(0.16f, 0.12f, 0.16f),
                        PresentationMaterials.Flower);
                }
            }

            for (var i = 0; i < 6; i++)
            {
                var pos = OffsetInRadius(rng, Vector3.zero, 44f);
                if (InSpecializedBiome(pos, 0.8f))
                {
                    continue;
                }

                PresentationModelLibrary.TrySpawn(
                    parent, "Pebble_" + i, Pick(rng, PresentationModelLibrary.SmallRocks),
                    pos, Rand(rng, 0.7f, 1.1f), new Vector3(0f, Rand(rng, 0f, 360f), 0f), false, out _);
            }
        }

        static void BuildWetlandShore(Transform parent)
        {
            var center = DemoBiomeLayout.WetlandCenter;
            var rng = new System.Random(53);
            var pondLocals = new[]
            {
                Vector3.zero,
                new Vector3(-7f, 0f, 5f),
                new Vector3(6f, 0f, -6f)
            };

            for (var p = 0; p < pondLocals.Length; p++)
            {
                var pond = center + pondLocals[p];
                for (var i = 0; i < 10; i++)
                {
                    var angle = Rand(rng, 0f, Mathf.PI * 2f);
                    var dist = Rand(rng, 3.4f, 6.8f);
                    var pos = pond + new Vector3(Mathf.Cos(angle) * dist, 0f, Mathf.Sin(angle) * dist);
                    if (!PresentationModelLibrary.TrySpawn(
                        parent, "Reed_" + p + "_" + i, PresentationModelLibrary.Reed,
                        pos, Rand(rng, 3.4f, 5.2f), new Vector3(0f, Rand(rng, 0f, 360f), 0f), false, out _))
                    {
                        var h = Rand(rng, 0.7f, 1.2f);
                        PresentationPrimitives.CreateChild(
                            parent, "Reed_" + p + "_" + i, PrimitiveType.Cylinder,
                            pos + new Vector3(0f, h, 0f),
                            new Vector3(0.08f, h, 0.08f),
                            PresentationMaterials.Reed);
                    }
                }

                for (var i = 0; i < 3; i++)
                {
                    var pos = pond + new Vector3(Rand(rng, -2.0f, 2.0f), 0f, Rand(rng, -2.0f, 2.0f));
                    var lily = i == 0 ? PresentationModelLibrary.LilyLarge : PresentationModelLibrary.LilySmall;
                    PresentationModelLibrary.TrySpawn(
                        parent, "Lily_" + p + "_" + i, lily, pos, Rand(rng, 1.4f, 2.0f),
                        new Vector3(0f, Rand(rng, 0f, 360f), 0f), false, out _);
                }
            }

            for (var i = 0; i < 10; i++)
            {
                var pos = OffsetInRadius(rng, center, DemoBiomeLayout.WetlandRadius * 0.85f);
                PresentationModelLibrary.TrySpawn(
                    parent, "WetBush_" + i, Pick(rng, PresentationModelLibrary.Bushes),
                    pos, Rand(rng, 1.4f, 2.1f), new Vector3(0f, Rand(rng, 0f, 360f), 0f), false, out _);
            }
        }

        static void BuildLandmarks(Transform parent)
        {
            SpawnTree(parent, "Landmark_ForestCluster", DemoBiomeLayout.ForestCenter + new Vector3(-4f, 0f, 2f), 11.2f, null, false);
            SpawnTree(parent, "Landmark_ForestClusterB", DemoBiomeLayout.ForestCenter + new Vector3(3f, 0f, -3f), 9.6f, null, false);
            SpawnTree(parent, "Landmark_GrasslandLoneTree", new Vector3(12f, 0f, 8f), 7.4f, null, false);
            PresentationModelLibrary.TrySpawn(
                parent, "Landmark_Stump", PresentationModelLibrary.Stump,
                new Vector3(-8f, 0f, 4f), 2.0f, new Vector3(0f, 40f, 0f), false, out _);
        }

        static void BuildOuterHills(Transform parent)
        {
            var rng = new System.Random(67);
            for (var i = 0; i < 14; i++)
            {
                var angle = (i / 14f) * Mathf.PI * 2f + Rand(rng, -0.42f, 0.42f);
                var dist = DemoBiomeLayout.WorldRadius + Rand(rng, 8f, 20f);
                var pos = new Vector3(Mathf.Cos(angle) * dist, 0f, Mathf.Sin(angle) * dist);
                    var h = Rand(rng, 2.8f, 5.2f);
                var w = Rand(rng, 16f, 24f);
                PresentationPrimitives.CreateChild(
                    parent, "Hill_" + i, PrimitiveType.Sphere,
                    pos + new Vector3(0f, h * 0.28f, 0f),
                    new Vector3(w, h, w * Rand(rng, 0.75f, 1.1f)),
                    PresentationMaterials.OuterBuffer,
                    new Vector3(0f, Rand(rng, 0f, 360f), 0f),
                    true,
                    true);
            }
        }

        static void SpawnTree(Transform parent, string name, Vector3 position, float scale, System.Random rng, bool edge)
        {
            var yaw = rng != null ? Rand(rng, 0f, 360f) : 15f;
            var path = edge
                ? (rng != null ? Pick(rng, PresentationModelLibrary.EdgeTrees) : PresentationModelLibrary.EdgeTrees[0])
                : (rng != null ? Pick(rng, PresentationModelLibrary.ForestTrees) : PresentationModelLibrary.ForestTrees[0]);
            if (PresentationModelLibrary.TrySpawn(parent, name, path, position, scale, new Vector3(0f, yaw, 0f), true, out _))
            {
                return;
            }

            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            root.position = position;
            PresentationPrimitives.CreateChild(
                root, "Trunk", PrimitiveType.Cylinder,
                new Vector3(0f, 1.7f * (scale / 3f), 0f),
                new Vector3(0.32f * (scale / 3f), 1.7f * (scale / 3f), 0.32f * (scale / 3f)),
                PresentationMaterials.Trunk,
                default,
                true);
            PresentationPrimitives.CreateChild(
                root, "Canopy", PrimitiveType.Sphere,
                new Vector3(0f, 3.55f * (scale / 3f), 0f),
                new Vector3(3.6f * (scale / 3f), 2.7f * (scale / 3f), 3.6f * (scale / 3f)),
                PresentationMaterials.Canopy,
                default,
                true);
        }

        static bool InSpecializedBiome(Vector3 pos, float radiusScale)
        {
            if ((pos - DemoBiomeLayout.ForestCenter).magnitude < DemoBiomeLayout.ForestRadius * radiusScale)
            {
                return true;
            }

            if ((pos - DemoBiomeLayout.WetlandCenter).magnitude < DemoBiomeLayout.WetlandRadius * radiusScale)
            {
                return true;
            }

            return (pos - DemoBiomeLayout.RockyCenter).magnitude < DemoBiomeLayout.RockyRadius * radiusScale;
        }

        static string Pick(System.Random rng, string[] paths) => paths[rng.Next(paths.Length)];

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
    }
}
