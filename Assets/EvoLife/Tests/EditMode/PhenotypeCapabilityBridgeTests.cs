using NUnit.Framework;
using UnityEngine;
using EvoLife.Creatures;
using EvoLife.Genetics;
using EvoLife.Simulation;

namespace EvoLife.Tests
{
    public sealed class PhenotypeCapabilityBridgeTests
    {
        [Test]
        public void PhenotypeModifiers_AffectOnlyTheTargetBiology()
        {
            var rates = new MetabolicRates(
                maxHealth: 100f,
                maxEnergy: 100f,
                maxAge: 500f,
                hungerIncreaseRate: 2f,
                thirstIncreaseRate: 0f,
                passiveEnergyConsumption: 1f,
                walkingEnergyConsumption: 0f,
                sprintingEnergyConsumption: 0f,
                attackEnergyConsumption: 0f,
                restingRecovery: 0f,
                starvationDamage: 0f,
                dehydrationDamage: 0f);

            var a = new CreatureBiology(rates);
            var b = new CreatureBiology(rates);
            var genome = Genome.FromTraitValues(
                (TraitId.MetabolismRate, 1f),
                (TraitId.MaximumEnergy, 200f),
                (TraitId.MaximumAge, 1000f));
            var phenotype = new CanonicalGenomeDecoder().Decode(genome);

            a.ApplyModifiers(
                a.Modifiers.With(
                    maxEnergyMultiplier: phenotype.MaxEnergyMultiplier,
                    maxAgeMultiplier: phenotype.MaxAgeMultiplier,
                    hungerRateMultiplier: phenotype.MetabolismMultiplier,
                    thirstRateMultiplier: phenotype.MetabolismMultiplier,
                    energyConsumptionMultiplier: phenotype.MetabolismMultiplier));

            a.Tick(1f);
            b.Tick(1f);

            Assert.AreEqual(200f, a.EffectiveMaxEnergy, 0.0001f);
            Assert.AreEqual(1000f, a.EffectiveMaxAge, 0.0001f);
            Assert.Greater(a.Snapshot.Hunger, b.Snapshot.Hunger);
            Assert.AreEqual(100f, b.EffectiveMaxEnergy, 0.0001f);
            Assert.AreEqual(500f, b.EffectiveMaxAge, 0.0001f);
            Assert.AreEqual(1f, b.Modifiers.HungerRateMultiplier);
        }

        [Test]
        public void CreatureSpawner_ResolveSpawnGenome_UsesCanonicalSchema()
        {
            var go = new GameObject("CreatureSpawnerGenomeTest");
            var spawner = go.AddComponent<CreatureSpawner>();
            spawner.SetSeed(11);

            var generated = spawner.ResolveSpawnGenome();
            var supplied = Genome.CreateDefault();
            var resolvedSupplied = spawner.ResolveSpawnGenome(supplied);

            Assert.AreEqual(CanonicalGenomeSchema.TraitCount, generated.Length);
            Assert.AreEqual(CanonicalGenomeSchema.Version, generated.SchemaVersion);
            CollectionAssert.AreEqual(supplied.ToArray(), resolvedSupplied.ToArray());
            Assert.AreNotSame(supplied, resolvedSupplied);

            Object.DestroyImmediate(go);
        }
    }
}
