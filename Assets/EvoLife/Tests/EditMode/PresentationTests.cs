using NUnit.Framework;
using UnityEngine;
using EvoLife.Common;
using EvoLife.Creatures;
using EvoLife.Environment;
using EvoLife.Genetics;
using EvoLife.Presentation;

namespace EvoLife.Tests
{
    public sealed class PresentationTests
    {
        [Test]
        public void PhenotypeVisualScale_ClampsAndRejectsNonFinite()
        {
            Assert.AreEqual(PhenotypeVisualScale.Neutral, PhenotypeVisualScale.ForBodySize(float.NaN));
            Assert.AreEqual(PhenotypeVisualScale.Neutral, PhenotypeVisualScale.ForBodySize(float.PositiveInfinity));
            Assert.AreEqual(PhenotypeVisualScale.Minimum, PhenotypeVisualScale.ForBodySize(0.1f));
            Assert.AreEqual(PhenotypeVisualScale.Maximum, PhenotypeVisualScale.ForBodySize(8f));
            Assert.AreEqual(1.1f, PhenotypeVisualScale.ForBodySize(1.1f), 0.0001f);
            Assert.AreEqual(0.80f, PhenotypeVisualScale.Minimum, 0.0001f);
            Assert.AreEqual(1.25f, PhenotypeVisualScale.Maximum, 0.0001f);
        }

        [Test]
        public void CreatureFactory_HerbivoreAndPredatorHaveRequiredDistinctComponents()
        {
            var herbivore = CreaturePresentationFactory.CreateTemplate(CreatureRole.Herbivore);
            var predator = CreaturePresentationFactory.CreateTemplate(CreatureRole.Predator);
            try
            {
                Assert.IsTrue(CreaturePresentationFactory.HasRequiredComponents(herbivore), string.Join(",", CreaturePresentationFactory.MissingRequirements(herbivore)));
                Assert.IsTrue(CreaturePresentationFactory.HasRequiredComponents(predator), string.Join(",", CreaturePresentationFactory.MissingRequirements(predator)));
                Assert.AreEqual(CreatureRole.Herbivore, herbivore.GetComponent<CreatureIdentity>().Role);
                Assert.AreEqual(CreatureRole.Predator, predator.GetComponent<CreatureIdentity>().Role);
                Assert.AreNotEqual(
                    herbivore.GetComponent<CreaturePresentation>().BodyColor,
                    predator.GetComponent<CreaturePresentation>().BodyColor);
                Assert.IsNotNull(herbivore.GetComponent<CapsuleCollider>());
                Assert.IsNotNull(predator.GetComponent<CapsuleCollider>());
                Assert.IsNull(herbivore.GetComponent<MeshCollider>());
                Assert.IsNull(predator.GetComponent<MeshCollider>());
                Assert.IsNotNull(herbivore.transform.Find(PresentationPrimitives.VisualRootName));
                Assert.Greater(herbivore.transform.Find(PresentationPrimitives.VisualRootName).childCount, 0);
                Assert.IsNotNull(herbivore.transform.Find(PresentationPrimitives.VisualRootName + "/Tail"));
                Assert.IsNotNull(predator.transform.Find(PresentationPrimitives.VisualRootName + "/Tail"));
                Assert.IsNotNull(herbivore.transform.Find(PresentationPrimitives.VisualRootName + "/Head"));
                Assert.IsNotNull(predator.transform.Find(PresentationPrimitives.VisualRootName + "/Snout"));
            }
            finally
            {
                Object.DestroyImmediate(herbivore);
                Object.DestroyImmediate(predator);
            }
        }

        [Test]
        public void PhenotypeVisual_ScalesChildNotRoot()
        {
            var go = CreaturePresentationFactory.CreateTemplate(CreatureRole.Herbivore);
            try
            {
                var genome = go.GetComponent<CreatureGenome>();
                genome.Initialize(Genome.FromTraitValues((TraitId.BodySize, 3f)));
                Assert.AreEqual(3f, genome.BodySizeMultiplier, 0.0001f);
                var visual = go.GetComponent<PhenotypeVisual>();
                visual.Apply();

                Assert.AreEqual(PhenotypeVisualScale.Maximum, visual.AppliedScale, 0.0001f);
                Assert.AreEqual(Vector3.one, go.transform.localScale);
                Assert.AreEqual(
                    new Vector3(PhenotypeVisualScale.Maximum, PhenotypeVisualScale.Maximum, PhenotypeVisualScale.Maximum),
                    visual.VisualRoot.localScale);
                Assert.AreEqual(0.34f, go.GetComponent<CapsuleCollider>().radius, 0.0001f);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void EventVisualAdapter_DoesNotMutateResources()
        {
            var root = new GameObject("EventVisualAdapterTest");
            var plantGo = new GameObject("Plant");
            try
            {
                var plant = plantGo.AddComponent<PlantResource>();
                plant.Configure(20f, 20f, 0f);
                var events = root.AddComponent<EnvironmentalEventManager>();
                var adapter = root.AddComponent<EnvironmentalEventVisualAdapter>();
                adapter.EnableEffects = false;
                adapter.Bind(events);
                adapter.RefreshVisuals();

                Assert.AreEqual(20f, plant.AvailableAmount, 0.0001f);
                Assert.IsFalse(plant.IsDepleted);
                Assert.AreEqual(1f, adapter.LastLushness, 0.0001f);
                Assert.IsFalse(adapter.WildfireVisible);
            }
            finally
            {
                Object.DestroyImmediate(plantGo);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void PresentationComponents_TolerateMissingOptionalReferences()
        {
            var root = new GameObject("PresentationMissingRefs");
            try
            {
                var lighting = root.AddComponent<DayNightLightingPresenter>();
                Assert.DoesNotThrow(() => lighting.OnDayNightUpdated(null));
                Assert.DoesNotThrow(() => lighting.OnDayNightUpdated(new DayNightCycle()));

                var adapter = root.AddComponent<EnvironmentalEventVisualAdapter>();
                adapter.EnableEffects = false;
                Assert.DoesNotThrow(() => adapter.RefreshVisuals());

                var builder = root.AddComponent<PresentationWorldBuilder>();
                Assert.DoesNotThrow(() => builder.Build());

                var plant = root.AddComponent<PlantPresentation>();
                Assert.DoesNotThrow(() => plant.EnsureVisuals());
                Assert.DoesNotThrow(() => plant.ApplyDepletion());

                var water = root.AddComponent<WaterPresentation>();
                Assert.DoesNotThrow(() => water.EnsureVisuals());
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void DemoBiomeLayout_KeepsSpecializedZonesFirst()
        {
            var zones = DemoBiomeLayout.CreateZones();
            Assert.AreEqual(BiomeKind.Forest, zones[0].Kind);
            Assert.AreEqual(BiomeKind.Wetland, zones[1].Kind);
            Assert.AreEqual(BiomeKind.Rocky, zones[2].Kind);
            Assert.AreEqual(BiomeKind.Grassland, zones[3].Kind);

            var map = new BiomeMap();
            map.ReplaceZones(zones);
            Assert.AreEqual(BiomeKind.Forest, map.ResolveKind(DemoBiomeLayout.ForestCenter));
            Assert.AreEqual(BiomeKind.Wetland, map.ResolveKind(DemoBiomeLayout.WetlandCenter));
            Assert.AreEqual(BiomeKind.Rocky, map.ResolveKind(DemoBiomeLayout.RockyCenter));
            Assert.AreEqual(BiomeKind.Grassland, map.ResolveKind(Vector3.zero));
            Assert.Greater(DemoBiomeLayout.WorldRadius, 60f);
        }

        [Test]
        public void DecorAndAnchors_CreateWithoutCollidersOnProps()
        {
            var root = new GameObject("DecorTestRoot");
            try
            {
                DemoWorldDecor.Build(root.transform, null);
                var anchors = PresentationCameraAnchors.Ensure(root.transform);
                Assert.IsNotNull(anchors.Find("CameraAnchor_Overview"));
                Assert.IsNotNull(root.transform.Find("Decorations/Trees"));
                Assert.IsNotNull(root.transform.Find("Landmarks"));
                Assert.Greater(root.transform.Find("Decorations/Trees").childCount, 60);
                Assert.Greater(root.transform.Find("Decorations/Rocks").childCount, 20);
                Assert.Greater(root.transform.Find("Decorations/Reeds").childCount, 30);
                var colliders = root.GetComponentsInChildren<Collider>();
                Assert.AreEqual(0, colliders.Length);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ModelLibrary_ExposesCuratedResourcePaths()
        {
            Assert.Greater(PresentationModelLibrary.ForestTrees.Length, 5);
            Assert.Greater(PresentationModelLibrary.LargeRocks.Length, 3);
            Assert.IsFalse(string.IsNullOrEmpty(PresentationModelLibrary.Herbivore));
            Assert.IsFalse(string.IsNullOrEmpty(PresentationModelLibrary.Predator));
        }

        [Test]
        public void IrregularDisc_BreaksCircularRadius()
        {
            var mesh = PresentationGroundMesh.CreateIrregularDisc(20f, 28, 0.25f, 11, 0.2f);
            try
            {
                Assert.Greater(PresentationGroundMesh.RimRadiusVariance(mesh), 4f);
                Assert.Greater(mesh.vertexCount, 20);
            }
            finally
            {
                Object.DestroyImmediate(mesh);
            }
        }

        [Test]
        public void PlantPresentation_DoesNotOverwriteExistingTriggerRadius()
        {
            var go = new GameObject("PlantRadiusGuard");
            try
            {
                var collider = go.AddComponent<SphereCollider>();
                collider.isTrigger = true;
                collider.radius = 0.91f;
                var plant = go.AddComponent<PlantPresentation>();
                plant.EnsureVisuals();
                Assert.AreEqual(0.91f, go.GetComponent<SphereCollider>().radius, 0.0001f);
                Assert.IsTrue(go.GetComponent<SphereCollider>().isTrigger);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void CreatureSilhouettes_DifferByShapeNotOnlyColor()
        {
            var herbivore = CreaturePresentationFactory.CreateTemplate(CreatureRole.Herbivore);
            var predator = CreaturePresentationFactory.CreateTemplate(CreatureRole.Predator);
            try
            {
                var herbTail = herbivore.transform.Find(PresentationPrimitives.VisualRootName + "/Tail");
                var predTail = predator.transform.Find(PresentationPrimitives.VisualRootName + "/Tail");
                var herbEar = herbivore.transform.Find(PresentationPrimitives.VisualRootName + "/EarL");
                var predSnout = predator.transform.Find(PresentationPrimitives.VisualRootName + "/Snout");
                Assert.IsNotNull(herbEar);
                Assert.IsNotNull(predSnout);
                Assert.Less(predTail.localPosition.z, herbTail.localPosition.z);
                Assert.Greater(herbEar.localPosition.y, predator.transform.Find(PresentationPrimitives.VisualRootName + "/EarL").localPosition.y);
                Assert.Greater(predSnout.localPosition.z, herbivore.transform.Find(PresentationPrimitives.VisualRootName + "/Snout").localPosition.z);
            }
            finally
            {
                Object.DestroyImmediate(herbivore);
                Object.DestroyImmediate(predator);
            }
        }

        [Test]
        public void BiomeGroundPresenter_UsesOrganicPatchesInsteadOfTransitionRings()
        {
            var world = new GameObject("OrganicBiomeWorld");
            var env = new GameObject("OrganicBiomeEnv");
            try
            {
                var registry = env.AddComponent<ResourceRegistry>();
                var manager = env.AddComponent<ResourceManager>();
                manager.PlaceOnStart = false;
                manager.Configure(registry, DemoBiomeLayout.CreateSpawnSettings(), DemoBiomeLayout.CreateZones(), 0);
                var presenter = env.AddComponent<BiomeGroundPresenter>();
                presenter.Bind(manager, world.transform);
                presenter.Build();

                Assert.Greater(presenter.GroundCount, 20);
                Assert.IsNotNull(world.transform.Find("Biomes/ForestVisual/Ground_Forest"));
                Assert.IsNotNull(world.transform.Find("Biomes/WetlandVisual/Ground_Wetland"));
                Assert.IsNotNull(world.transform.Find("Biomes/RockyVisual/Ground_Rocky"));
                Assert.IsNull(world.transform.Find("Biomes/Transitions"));
                var transforms = world.GetComponentsInChildren<Transform>();
                for (var i = 0; i < transforms.Length; i++)
                {
                    Assert.IsFalse(transforms[i].name.StartsWith("Transition_"));
                }

                var colliders = world.GetComponentsInChildren<Collider>();
                Assert.AreEqual(0, colliders.Length);
            }
            finally
            {
                Object.DestroyImmediate(world);
                Object.DestroyImmediate(env);
            }
        }
    }
}
