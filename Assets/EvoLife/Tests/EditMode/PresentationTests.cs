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
            Assert.AreEqual(BiomeKind.Forest, map.ResolveKind(new Vector3(-16f, 0f, 14f)));
            Assert.AreEqual(BiomeKind.Wetland, map.ResolveKind(new Vector3(18f, 0f, 12f)));
            Assert.AreEqual(BiomeKind.Rocky, map.ResolveKind(new Vector3(2f, 0f, -18f)));
        }
    }
}
