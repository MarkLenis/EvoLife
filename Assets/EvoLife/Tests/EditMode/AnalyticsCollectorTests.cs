using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using EvoLife.Analytics;
using EvoLife.Common;
using EvoLife.Simulation;

namespace EvoLife.Tests
{
    public sealed class AnalyticsSnapshotBuilderTests
    {
        [Test]
        public void Build_CopiesPopulationAndBirthDeathCounters()
        {
            var population = new StubPopulation
            {
                HerbivoreCount = 8,
                PredatorCount = 2,
                TotalAlive = 10,
                Births = 12,
                Deaths = 2
            };

            var snapshot = AnalyticsSnapshotBuilder.Build(
                "exp-1",
                15.5f,
                population,
                previousTotalAlive: 9,
                census: new PolicyCensus { ScriptedAlive = 7, PpoAlive = 3, MaxGeneration = 2 },
                timestampUtcUnix: 100f);

            Assert.AreEqual("exp-1", snapshot.experimentId);
            Assert.AreEqual(15.5f, snapshot.simulationTimeSeconds);
            Assert.AreEqual(8, snapshot.herbivoreCount);
            Assert.AreEqual(2, snapshot.predatorCount);
            Assert.AreEqual(10, snapshot.totalAlive);
            Assert.AreEqual(12, snapshot.births);
            Assert.AreEqual(2, snapshot.deaths);
            Assert.AreEqual(1, snapshot.populationChange);
            Assert.AreEqual(7, snapshot.scriptedAlive);
            Assert.AreEqual(3, snapshot.ppoAlive);
            Assert.AreEqual(2, snapshot.maxGeneration);
        }

        [Test]
        public void Build_EmptyPopulation_DoesNotDivideByZero()
        {
            var snapshot = AnalyticsSnapshotBuilder.Build("empty", 0f, null);
            Assert.AreEqual(0, snapshot.totalAlive);
            Assert.AreEqual(0, snapshot.births);
            Assert.AreEqual(0, snapshot.deaths);
            Assert.AreEqual(0, snapshot.populationChange);
        }

        [Test]
        public void Census_ClassifiesScriptedAndPpo()
        {
            var views = new IAnalyticsCreatureView[]
            {
                View(AgentPolicyKind.ScriptedBaseline, 0),
                View(AgentPolicyKind.ScriptedBaseline, 1),
                View(AgentPolicyKind.LearnedPpo, 2)
            };

            var census = AnalyticsSnapshotBuilder.Census(views);
            Assert.AreEqual(2, census.ScriptedAlive);
            Assert.AreEqual(1, census.PpoAlive);
            Assert.AreEqual(2, census.MaxGeneration);
        }

        [Test]
        public void Census_Empty_ReturnsZeros()
        {
            var census = AnalyticsSnapshotBuilder.Census(null);
            Assert.AreEqual(0, census.ScriptedAlive);
            Assert.AreEqual(0, census.PpoAlive);
            Assert.AreEqual(0, census.MaxGeneration);
        }

        static StubAnalyticsView View(AgentPolicyKind policy, int generation)
        {
            return new StubAnalyticsView
            {
                Identity = new StubIdentity { Id = new CreatureId(generation + 1), Role = CreatureRole.Herbivore, SpeciesId = "herb" },
                Lineage = new StubLineage { Generation = generation },
                Policy = new StubPolicyOwner { PolicyKind = policy },
                Vitals = new StubVitalState { IsAlive = true }
            };
        }
    }

    public sealed class CreatureLifetimeRecordTests
    {
        [Test]
        public void Create_CopiesIdentityGeneticsAndPolicy()
        {
            var view = new StubAnalyticsView
            {
                Identity = new StubIdentity
                {
                    Id = new CreatureId(7),
                    Role = CreatureRole.Predator,
                    SpeciesId = "wolf"
                },
                Lineage = new StubLineage
                {
                    Generation = 3,
                    ParentA = new CreatureId(1),
                    ParentB = new CreatureId(2)
                },
                Policy = new StubPolicyOwner { PolicyKind = AgentPolicyKind.LearnedPpo },
                Vitals = new StubVitalState { Age = 42f, IsAlive = false, CauseOfDeath = DeathCause.Starvation },
                GenomeTraits = new StubGenomeTraits(("base_movement_speed", 2.5f), ("aggression", 0.8f)),
                EpisodeMetrics = new StubEpisodeMetrics
                {
                    PolicyKind = AgentPolicyKind.LearnedPpo,
                    HasEpisodeReturn = true,
                    EpisodeReturn = 1.25f,
                    EpisodeSurvivalSeconds = 42f,
                    CompletedEpisodeCount = 1
                }
            };

            var record = CreatureLifetimeFactory.Create(
                view,
                new CreatureDeathNotice(new CreatureId(7), DeathCause.Starvation, 42f, 500f),
                birthTime: 10f,
                deathTime: 52f,
                offspringCount: 2);

            Assert.AreEqual("7", record.CreatureId);
            Assert.AreEqual("wolf", record.Species);
            Assert.AreEqual("predator", record.Role);
            Assert.AreEqual(PolicyKindNames.LearnedPpo, record.PolicyKind);
            Assert.AreEqual(3, record.Generation);
            Assert.AreEqual("starvation", record.CauseOfDeath);
            Assert.AreEqual("1", record.ParentId1);
            Assert.AreEqual("2", record.ParentId2);
            Assert.AreEqual(2, record.OffspringCount);
            Assert.AreEqual(42f, record.Lifetime);
            Assert.AreEqual(2.5f, record.GenomeTraits["base_movement_speed"]);
            Assert.IsTrue(record.HasEpisodeReturn);
            Assert.AreEqual(1.25f, record.EpisodeReturn);
        }

        [Test]
        public void Create_FounderWithoutParentsOrReturn()
        {
            var view = new StubAnalyticsView
            {
                Identity = new StubIdentity { Id = new CreatureId(1), SpeciesId = "herb", Role = CreatureRole.Herbivore },
                Lineage = new StubLineage { Generation = 0 },
                Policy = new StubPolicyOwner { PolicyKind = AgentPolicyKind.ScriptedBaseline },
                Vitals = new StubVitalState { Age = 8f }
            };

            var record = CreatureLifetimeFactory.Create(
                view,
                new CreatureDeathNotice(new CreatureId(1), DeathCause.OldAge, 8f, 100f),
                0f,
                8f);

            Assert.IsNull(record.ParentId1);
            Assert.IsNull(record.ParentId2);
            Assert.AreEqual(PolicyKindNames.ScriptedBaseline, record.PolicyKind);
            Assert.IsFalse(record.HasEpisodeReturn);
            Assert.AreEqual("old_age", record.CauseOfDeath);
        }
    }

    public sealed class GenerationAggregatorTests
    {
        [Test]
        public void Aggregate_Empty_ReturnsNoRows()
        {
            var result = GenerationAggregator.Aggregate(new List<CreatureTraitSample>());
            Assert.AreEqual(0, result.Count);

            var upload = GenerationAnalyticsCollector.BuildUploadSummaries(result);
            Assert.AreEqual(0, upload.Count);
        }

        [Test]
        public void Aggregate_ComputesMeanAndVarianceByGenerationAndPolicy()
        {
            var samples = new List<CreatureTraitSample>
            {
                Sample("herb", "herbivore", PolicyKindNames.ScriptedBaseline, 0, 10f, 1f),
                Sample("herb", "herbivore", PolicyKindNames.ScriptedBaseline, 0, 20f, 3f),
                Sample("herb", "herbivore", PolicyKindNames.LearnedPpo, 0, 12f, 2f)
            };

            var aggregates = GenerationAggregator.Aggregate(samples);
            var overall = Find(aggregates, policy: "");
            var scripted = Find(aggregates, policy: PolicyKindNames.ScriptedBaseline);
            var ppo = Find(aggregates, policy: PolicyKindNames.LearnedPpo);

            Assert.AreEqual(3, overall.PopulationCount);
            Assert.AreEqual(2, scripted.PopulationCount);
            Assert.AreEqual(1, ppo.PopulationCount);
            Assert.AreEqual(2f, overall.AverageTraits["speed"]);
            Assert.AreEqual(2f, scripted.AverageTraits["speed"]);
            Assert.AreEqual(0f, ppo.TraitVariance["speed"]);
            Assert.Greater(scripted.TraitVariance["speed"], 0f);
            Assert.AreEqual(15f, scripted.AverageLifespan);

            var upload = GenerationAnalyticsCollector.BuildUploadSummaries(aggregates);
            Assert.AreEqual(1, upload.Count);
            Assert.AreEqual(3, upload[0].PopulationCount);
            Assert.IsTrue(upload[0].ExtraStatistics.ContainsKey("by_policy"));
            Assert.IsTrue(upload[0].ExtraStatistics.ContainsKey("trait_variance"));
        }

        static CreatureTraitSample Sample(string species, string role, string policy, int generation, float life, float speed)
        {
            return new CreatureTraitSample
            {
                Species = species,
                Role = role,
                PolicyKind = policy,
                Generation = generation,
                Lifespan = life,
                Traits = new Dictionary<string, float> { ["speed"] = speed }
            };
        }

        static GenerationAggregate Find(List<GenerationAggregate> aggregates, string policy)
        {
            for (var i = 0; i < aggregates.Count; i++)
            {
                if (aggregates[i].PolicyKind == policy)
                {
                    return aggregates[i];
                }
            }

            Assert.Fail("missing aggregate for policy " + policy);
            return null;
        }
    }

    public sealed class PolicyClassifierTests
    {
        [Test]
        public void Classify_MapsEnumToWireNames()
        {
            Assert.AreEqual(PolicyKindNames.ScriptedBaseline, PolicyClassifier.Classify(AgentPolicyKind.ScriptedBaseline));
            Assert.AreEqual(PolicyKindNames.LearnedPpo, PolicyClassifier.Classify(AgentPolicyKind.LearnedPpo));
            Assert.IsTrue(PolicyClassifier.IsLearnedPpo(PolicyKindNames.LearnedPpo));
            Assert.IsFalse(PolicyClassifier.IsLearnedPpo(PolicyKindNames.ScriptedBaseline));
        }

        [Test]
        public void Classify_UsesPolicyOwnerOnView()
        {
            var view = new StubAnalyticsView
            {
                Policy = new StubPolicyOwner { PolicyKind = AgentPolicyKind.LearnedPpo }
            };
            Assert.AreEqual(PolicyKindNames.LearnedPpo, PolicyClassifier.Classify(view));
        }

        [Test]
        public void Classify_NullView_DefaultsToScripted()
        {
            Assert.AreEqual(PolicyKindNames.ScriptedBaseline, PolicyClassifier.Classify((IAnalyticsCreatureView)null));
        }
    }

    public sealed class ExperimentRunMetadataTests
    {
        [Test]
        public void FromConfig_RecordsEcosystemModeAndRespawn()
        {
            var config = ScriptableObject.CreateInstance<SimulationConfig>();
            try
            {
                config.Ecosystem.Mode = EcosystemMode.TrainingSupport;
                config.Ecosystem.TrainingRespawnEnabled = true;
                config.Ecosystem.MaxHerbivores = 40;
                config.Ecosystem.MaxPredators = 9;

                var metadata = ExperimentRunMetadata.FromConfig(config, 123);
                var dictionary = metadata.ToConfigurationDictionary();

                Assert.AreEqual(EcosystemModeNames.TrainingSupport, metadata.EcosystemMode);
                Assert.IsTrue(metadata.TrainingRespawnEnabled);
                Assert.AreEqual(EcosystemModeNames.TrainingSupport, dictionary["ecosystem_mode"]);
                Assert.AreEqual(true, dictionary["training_respawn_enabled"]);
                Assert.AreEqual(40, dictionary["max_herbivores"]);
                Assert.AreEqual(9, dictionary["max_predators"]);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(config);
            }
        }
    }

    public sealed class TraitStatisticsTests
    {
        [Test]
        public void EmptyAndSingleValue_DoNotDivideByZero()
        {
            Assert.AreEqual(0f, TraitStatistics.Mean(null));
            Assert.AreEqual(0f, TraitStatistics.Mean(new List<float>()));
            Assert.AreEqual(0f, TraitStatistics.Variance(new List<float>()));
            Assert.AreEqual(0f, TraitStatistics.Variance(new List<float> { 4f }));
            Assert.AreEqual(0f, TraitStatistics.Min(new List<float>()));
            Assert.AreEqual(0f, TraitStatistics.Max(new List<float>()));
        }

        [Test]
        public void MeanAndVariance_KnownValues()
        {
            var values = new List<float> { 1f, 3f, 5f };
            Assert.AreEqual(3f, TraitStatistics.Mean(values));
            Assert.AreEqual(1f, TraitStatistics.Min(values));
            Assert.AreEqual(5f, TraitStatistics.Max(values));
            Assert.AreEqual(8f / 3f, TraitStatistics.Variance(values), 0.0001f);
        }
    }

    public sealed class AnalyticsJsonTests
    {
        [Test]
        public void Serialize_WritesSnakeCaseAndNestedTraits()
        {
            var dto = AnalyticsDtoMapper.ToCreatureDto(new CreatureLifetimeRecord
            {
                CreatureId = "9",
                Species = "herb",
                Role = "herbivore",
                PolicyKind = PolicyKindNames.LearnedPpo,
                Generation = 1,
                BirthTime = 2f,
                DeathTime = 8f,
                Lifetime = 6f,
                CauseOfDeath = "starvation",
                GenomeTraits = new Dictionary<string, float> { ["base_movement_speed"] = 2.5f }
            });

            var json = AnalyticsJson.Serialize(new CreatureBatchDto
            {
                Records = new List<CreatureLifeRecordDto> { dto }
            });

            StringAssert.Contains("\"creature_id\":\"9\"", json);
            StringAssert.Contains("\"policy_kind\":\"learned_ppo\"", json);
            StringAssert.Contains("\"base_movement_speed\":", json);
            StringAssert.Contains("\"records\":[", json);
        }
    }
}
