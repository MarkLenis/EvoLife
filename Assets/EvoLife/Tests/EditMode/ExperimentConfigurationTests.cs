using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using EvoLife.Analytics;
using EvoLife.Common;
using EvoLife.Creatures;
using EvoLife.Genetics;
using EvoLife.Simulation;

namespace EvoLife.Tests
{
    public sealed class ExperimentConfigurationSerializationTests
    {
        [Test]
        public void JsonRoundTrip_PreservesRequiredFields()
        {
            var original = ExperimentScenarios.Create(ExperimentScenarios.Drought);
            original.ModelId = "herbivore_dev";
            original.HerbivorePolicy = AgentPolicyKind.LearnedPpo;
            original.RandomSeed = 99;

            var json = ExperimentConfigurationSerializer.ToJson(original);
            var restored = ExperimentConfigurationSerializer.FromJson(json);

            Assert.AreEqual(original.ExperimentName, restored.ExperimentName);
            Assert.AreEqual(99, restored.RandomSeed);
            Assert.AreEqual(original.InitialHerbivores, restored.InitialHerbivores);
            Assert.AreEqual(original.InitialPredators, restored.InitialPredators);
            Assert.AreEqual(original.ResourceAbundance, restored.ResourceAbundance, 0.0001f);
            Assert.AreEqual(original.PlantRegenerationMultiplier, restored.PlantRegenerationMultiplier, 0.0001f);
            Assert.AreEqual(original.MutationProbability, restored.MutationProbability, 0.0001f);
            Assert.AreEqual(original.DayLengthSeconds, restored.DayLengthSeconds, 0.0001f);
            Assert.AreEqual(AgentPolicyKind.LearnedPpo, restored.HerbivorePolicy);
            Assert.AreEqual(original.ScenarioId, restored.ScenarioId);
            Assert.AreEqual("herbivore_dev", restored.ModelId);
            Assert.AreEqual(1, restored.EnabledEnvironmentalEvents.Length);
            Assert.AreEqual(EnvironmentalEventKindNames.Drought, restored.EnabledEnvironmentalEvents[0]);
            Assert.AreEqual(1, restored.ScheduledEvents.Length);
            Assert.AreEqual(60f, restored.ScheduledEvents[0].AtSimulationTime, 0.0001f);
            Assert.IsTrue(ExperimentConfigurationValidator.IsValid(restored));
        }

        [Test]
        public void Clone_IsIndependentCopy()
        {
            var original = ExperimentConfiguration.CreateDefault();
            original.ExperimentName = "alpha";
            original.EnabledEnvironmentalEvents = new[] { EnvironmentalEventKindNames.Wildfire };
            var clone = original.Clone();
            clone.ExperimentName = "beta";
            clone.EnabledEnvironmentalEvents[0] = EnvironmentalEventKindNames.Drought;
            Assert.AreEqual("alpha", original.ExperimentName);
            Assert.AreEqual(EnvironmentalEventKindNames.Wildfire, original.EnabledEnvironmentalEvents[0]);
        }
    }

    public sealed class DeterministicSeedTests
    {
        [Test]
        public void SameMasterSeed_YieldsSameDerivedStreams()
        {
            var a = new DeterministicSeedTable(42);
            var b = new DeterministicSeedTable(42);
            Assert.AreEqual(a.FounderGenomes, b.FounderGenomes);
            Assert.AreEqual(a.Reproduction, b.Reproduction);
            Assert.AreEqual(a.ResourceSpawn, b.ResourceSpawn);
            Assert.AreEqual(a.EventSchedule, b.EventSchedule);
            Assert.AreEqual(a.ScriptedWander, b.ScriptedWander);
            Assert.AreEqual(DeterministicSeeds.ScriptedWander(42, 7), DeterministicSeeds.ScriptedWander(42, 7));
        }

        [Test]
        public void DifferentMasterSeeds_YieldDifferentDerivedStreams()
        {
            var a = new DeterministicSeedTable(42);
            var b = new DeterministicSeedTable(43);
            Assert.AreNotEqual(a.FounderGenomes, b.FounderGenomes);
            Assert.AreNotEqual(a.Reproduction, b.Reproduction);
            Assert.AreNotEqual(a.ResourceSpawn, b.ResourceSpawn);
        }

        [Test]
        public void FounderGenomes_AreReproducibleForTheSameSeed()
        {
            var ops = new DefaultGeneticOperators();
            var first = ops.CreateFounder(new System.Random(DeterministicSeeds.FounderGenomes(7)));
            var second = ops.CreateFounder(new System.Random(DeterministicSeeds.FounderGenomes(7)));
            CollectionAssert.AreEqual(first.ToArray(), second.ToArray());
        }

        [Test]
        public void SimulationConfig_ApplyPropagatesSeedAndPolicies()
        {
            var experiment = ExperimentConfiguration.CreateDefault();
            experiment.RandomSeed = 1234;
            experiment.HerbivorePolicy = AgentPolicyKind.LearnedPpo;
            experiment.PredatorPolicy = AgentPolicyKind.ScriptedBaseline;
            experiment.SetInitialPopulation(9, 3);
            experiment.MutationProbability = 0.4f;

            var config = ScriptableObject.CreateInstance<SimulationConfig>();
            try
            {
                config.ApplyExperiment(experiment);
                Assert.AreEqual(1234, config.RandomSeed);
                Assert.AreEqual(AgentPolicyKind.LearnedPpo, config.HerbivorePolicy);
                Assert.AreEqual(AgentPolicyKind.ScriptedBaseline, config.PredatorPolicy);
                Assert.AreEqual(9, config.InitialHerbivores);
                Assert.AreEqual(3, config.InitialPredators);
                Assert.AreEqual(0.4f, config.MutationProbability, 0.0001f);

                var roundTrip = config.ToExperimentConfiguration();
                Assert.AreEqual(1234, roundTrip.RandomSeed);
                Assert.AreEqual(AgentPolicyKind.LearnedPpo, roundTrip.PolicyFor(CreatureRole.Herbivore));
                Assert.AreEqual(AgentPolicyKind.ScriptedBaseline, roundTrip.PolicyFor(CreatureRole.Predator));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }
    }

    public sealed class ExperimentScenarioTests
    {
        [Test]
        public void StarterCatalog_ContainsRequiredIds()
        {
            CollectionAssert.AreEquivalent(
                new[]
                {
                    ExperimentScenarios.NormalControl,
                    ExperimentScenarios.ReducedFood,
                    ExperimentScenarios.Drought,
                    ExperimentScenarios.FastPredators,
                    ExperimentScenarios.HighMutation,
                    ExperimentScenarios.LowMutation,
                    ExperimentScenarios.PredatorPressure,
                    ExperimentScenarios.RecoveryAfterEvent
                },
                ExperimentScenarios.All);
        }

        [Test]
        public void ReducedFood_OverridesAbundanceOnlyFromControl()
        {
            var control = ExperimentScenarios.Create(ExperimentScenarios.NormalControl);
            var reduced = ExperimentScenarios.Create(ExperimentScenarios.ReducedFood);
            Assert.Less(reduced.ResourceAbundance, control.ResourceAbundance);
            Assert.AreEqual(control.InitialHerbivores, reduced.InitialHerbivores);
            Assert.AreEqual(ExperimentScenarios.ReducedFood, reduced.ScenarioId);
        }

        [Test]
        public void Drought_EnablesScheduledDroughtEvent()
        {
            var drought = ExperimentScenarios.Create(ExperimentScenarios.Drought);
            Assert.Contains(EnvironmentalEventKindNames.Drought, drought.EnabledEnvironmentalEvents);
            Assert.AreEqual(1, drought.ScheduledEvents.Length);
            Assert.AreEqual(EnvironmentalEventKindNames.Drought, drought.ScheduledEvents[0].Kind);
        }

        [Test]
        public void FastPredators_AppliesSpeedBias()
        {
            var fast = ExperimentScenarios.Create(ExperimentScenarios.FastPredators);
            Assert.Greater(fast.PredatorSpeedBias, 0f);
            var genome = FounderGenomeAdjuster.Apply(Genome.CreateDefault(), CreatureRole.Predator, fast.PredatorSpeedBias);
            Assert.Greater(genome.Get(TraitId.BaseMovementSpeed), Genome.CreateDefault().Get(TraitId.BaseMovementSpeed));
            Assert.AreEqual(
                Genome.CreateDefault().Get(TraitId.BaseMovementSpeed),
                FounderGenomeAdjuster.Apply(Genome.CreateDefault(), CreatureRole.Herbivore, fast.PredatorSpeedBias)
                    .Get(TraitId.BaseMovementSpeed));
        }

        [Test]
        public void MutationScenarios_DivergeFromControl()
        {
            var control = ExperimentScenarios.Create(ExperimentScenarios.NormalControl);
            var high = ExperimentScenarios.Create(ExperimentScenarios.HighMutation);
            var low = ExperimentScenarios.Create(ExperimentScenarios.LowMutation);
            Assert.Greater(high.MutationProbability, control.MutationProbability);
            Assert.Less(low.MutationProbability, control.MutationProbability);
        }

        [Test]
        public void PredatorPressure_IncreasesPredatorCount()
        {
            var control = ExperimentScenarios.Create(ExperimentScenarios.NormalControl);
            var pressure = ExperimentScenarios.Create(ExperimentScenarios.PredatorPressure);
            Assert.Greater(pressure.InitialPredators, control.InitialPredators);
        }

        [Test]
        public void RecoveryAfterEvent_SchedulesDroughtThenFoodBoom()
        {
            var recovery = ExperimentScenarios.Create(ExperimentScenarios.RecoveryAfterEvent);
            Assert.AreEqual(2, recovery.ScheduledEvents.Length);
            Assert.AreEqual(EnvironmentalEventKindNames.Drought, recovery.ScheduledEvents[0].Kind);
            Assert.AreEqual(EnvironmentalEventKindNames.FoodBoom, recovery.ScheduledEvents[1].Kind);
            Assert.Greater(recovery.ScheduledEvents[1].AtSimulationTime, recovery.ScheduledEvents[0].AtSimulationTime);
        }

        [Test]
        public void UnknownScenario_TryCreateIsFalse()
        {
            Assert.IsFalse(ExperimentScenarios.TryCreate("not_a_scenario", out _));
            Assert.Throws<ExperimentConfigurationException>(() => ExperimentScenarios.Create("not_a_scenario"));
        }

        [Test]
        public void BaselineOverrides_AreAppliedOnTopOfCustomCounts()
        {
            var baseline = ExperimentConfiguration.CreateDefault();
            baseline.SetInitialPopulation(4, 1);
            var reduced = ExperimentScenarios.Create(ExperimentScenarios.ReducedFood, baseline);
            Assert.AreEqual(4, reduced.InitialHerbivores);
            Assert.AreEqual(1, reduced.InitialPredators);
            Assert.AreEqual(0.35f, reduced.ResourceAbundance, 0.0001f);
        }
    }

    public sealed class ExperimentStopConditionTests
    {
        [Test]
        public void MaxTime_StopsWhenReached()
        {
            var conditions = ExperimentStoppingConditions.ForTrainingEpisode(10f);
            var reason = ExperimentStopEvaluator.Evaluate(
                conditions,
                10f,
                new StubPopulation { HerbivoreCount = 4, PredatorCount = 2, TotalAlive = 6 },
                false);
            Assert.AreEqual(ExperimentStopReason.MaxSimulationTime, reason);
        }

        [Test]
        public void EcosystemExtinction_StopsWhenConfigured()
        {
            var conditions = ExperimentStoppingConditions.ForPersistentEcosystem(600f);
            var reason = ExperimentStopEvaluator.Evaluate(
                conditions,
                5f,
                new StubPopulation { HerbivoreCount = 0, PredatorCount = 0, TotalAlive = 0 },
                false);
            Assert.AreEqual(ExperimentStopReason.EcosystemExtinct, reason);
        }

        [Test]
        public void TrainingEpisode_DoesNotStopOnExtinction()
        {
            var conditions = ExperimentStoppingConditions.ForTrainingEpisode(100f);
            var reason = ExperimentStopEvaluator.Evaluate(
                conditions,
                5f,
                new StubPopulation { HerbivoreCount = 0, PredatorCount = 0, TotalAlive = 0 },
                false);
            Assert.AreEqual(ExperimentStopReason.None, reason);
        }

        [Test]
        public void ManualStop_WinsOverTimeAndExtinction()
        {
            var conditions = ExperimentStoppingConditions.ForPersistentEcosystem(1f);
            var reason = ExperimentStopEvaluator.Evaluate(
                conditions,
                50f,
                new StubPopulation { HerbivoreCount = 0, PredatorCount = 0, TotalAlive = 0 },
                true);
            Assert.AreEqual(ExperimentStopReason.ManualStop, reason);
        }

        [Test]
        public void Coordinator_RunsUntilTimeLimitThenFinishes()
        {
            var config = ExperimentConfiguration.CreateDefault();
            config.Stopping = ExperimentStoppingConditions.ForTrainingEpisode(8f);
            var coordinator = new ExperimentCoordinator();
            coordinator.Load(config);
            coordinator.MarkEnvironmentInitialized();
            coordinator.MarkPopulationInitialized();
            coordinator.MarkAnalyticsStarted("run-1");
            coordinator.BeginRunning();

            Assert.AreEqual(ExperimentRunPhase.Running, coordinator.State.Phase);
            Assert.AreEqual(
                ExperimentStopReason.None,
                coordinator.Evaluate(3f, new StubPopulation { HerbivoreCount = 2, PredatorCount = 1, TotalAlive = 3 }));
            Assert.AreEqual(
                ExperimentStopReason.MaxSimulationTime,
                coordinator.Evaluate(8f, new StubPopulation { HerbivoreCount = 2, PredatorCount = 1, TotalAlive = 3 }));
            var finished = coordinator.Finish("run-1");
            Assert.AreEqual(ExperimentRunPhase.Finished, finished.Phase);
            Assert.AreEqual(ExperimentStopReason.MaxSimulationTime, finished.StopReason);
            Assert.AreEqual("run-1", finished.RunId);
        }
    }

    public sealed class ExperimentRunMetadataCorrectnessTests
    {
        [Test]
        public void FromExperiment_CopiesSeedTableAndStopSettings()
        {
            var experiment = ExperimentScenarios.Create(ExperimentScenarios.Drought);
            experiment.ModelId = "model-a";
            var metadata = ExperimentRunMetadata.FromExperiment(experiment, 50);
            var dictionary = metadata.ToConfigurationDictionary();

            Assert.AreEqual(experiment.ExperimentName, metadata.ExperimentName);
            Assert.AreEqual(experiment.RandomSeed, metadata.RandomSeed);
            Assert.AreEqual(experiment.Seeds.FounderGenomes, metadata.FounderGenomeSeed);
            Assert.AreEqual(experiment.Seeds.Reproduction, metadata.ReproductionSeed);
            Assert.AreEqual("model-a", metadata.TrainingModelId);
            Assert.AreEqual(experiment.ResourceAbundance, metadata.ResourceAbundance);
            Assert.AreEqual(experiment.Stopping.MaxSimulationTimeSeconds, metadata.MaxSimulationTimeSeconds);
            Assert.AreEqual(experiment.Seeds.FounderGenomes, dictionary["seed_founder_genomes"]);
            Assert.AreEqual(ExperimentScenarios.Drought, dictionary["scenario_id"]);
            CollectionAssert.Contains((string[])dictionary["enabled_environmental_events"], EnvironmentalEventKindNames.Drought);
        }

        [Test]
        public void FromConfig_MatchesAppliedExperiment()
        {
            var experiment = TrainingCurriculum.Create(3, TrainingCurriculumFocus.Combined);
            var config = ScriptableObject.CreateInstance<SimulationConfig>();
            try
            {
                config.ApplyExperiment(experiment);
                var metadata = ExperimentRunMetadata.FromConfig(config, 1);
                Assert.AreEqual(experiment.HerbivorePolicy, ParsePolicy(metadata.HerbivorePolicy));
                Assert.AreEqual(experiment.PredatorPolicy, ParsePolicy(metadata.PredatorPolicy));
                Assert.AreEqual(experiment.InitialHerbivores, metadata.InitialHerbivores);
                Assert.AreEqual(TrainingCurriculum.Stage3PredatorPrey, metadata.CurriculumStageId);
                Assert.AreEqual(EcosystemModeNames.TrainingSupport, metadata.EcosystemMode);
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        static AgentPolicyKind ParsePolicy(string wire)
        {
            Assert.IsTrue(PolicyKindNames.TryParse(wire, out var kind));
            return kind;
        }
    }

    public sealed class ExperimentExtinctionSafetyTests
    {
        [Test]
        public void PopulationRates_DoNotDivideByZeroWhenExtinct()
        {
            var empty = new StubPopulation { HerbivoreCount = 0, PredatorCount = 0, TotalAlive = 0 };
            Assert.AreEqual(0f, ExperimentPopulationRates.HerbivoreFraction(empty));
            Assert.AreEqual(0f, ExperimentPopulationRates.PredatorFraction(empty));
            Assert.AreEqual(0f, ExperimentPopulationRates.PredatorsPerHerbivore(empty));
            Assert.AreEqual(0f, ExperimentPopulationRates.HerbivoreFraction(null));
            Assert.AreEqual(0f, TraitStatistics.Mean(new List<float>()));
        }

        [Test]
        public void ResourceCensus_ZeroCapacity_IsZeroAbundance()
        {
            var census = new EvoLife.Environment.ResourceCensus(0, 0, 0f, 0f, 0f);
            Assert.AreEqual(0f, census.PlantAbundance);
            Assert.AreEqual(0f, census.PlantDensity);
        }
    }

    public sealed class ExperimentPolicySelectionTests
    {
        [Test]
        public void PolicyFor_SelectsPerRole()
        {
            var config = ExperimentConfiguration.CreateDefault();
            config.HerbivorePolicy = AgentPolicyKind.LearnedPpo;
            config.PredatorPolicy = AgentPolicyKind.ScriptedBaseline;
            Assert.AreEqual(AgentPolicyKind.LearnedPpo, config.PolicyFor(CreatureRole.Herbivore));
            Assert.AreEqual(AgentPolicyKind.ScriptedBaseline, config.PolicyFor(CreatureRole.Predator));
        }

        [Test]
        public void CurriculumFocus_SetsExpectedPolicies()
        {
            var herbivore = TrainingCurriculum.Create(3, TrainingCurriculumFocus.Herbivore);
            Assert.AreEqual(AgentPolicyKind.LearnedPpo, herbivore.HerbivorePolicy);
            Assert.AreEqual(AgentPolicyKind.ScriptedBaseline, herbivore.PredatorPolicy);

            var predator = TrainingCurriculum.Create(3, TrainingCurriculumFocus.Predator);
            Assert.AreEqual(AgentPolicyKind.ScriptedBaseline, predator.HerbivorePolicy);
            Assert.AreEqual(AgentPolicyKind.LearnedPpo, predator.PredatorPolicy);

            var combined = TrainingCurriculum.Create(3, TrainingCurriculumFocus.Combined);
            Assert.AreEqual(AgentPolicyKind.LearnedPpo, combined.HerbivorePolicy);
            Assert.AreEqual(AgentPolicyKind.LearnedPpo, combined.PredatorPolicy);
        }

        [Test]
        public void InitialPopulationSpawner_UsesConfigPolicies()
        {
            var objects = new List<GameObject>();
            var vitals = ScriptableObject.CreateInstance<SpeciesVitalsDefinition>();
            var config = ScriptableObject.CreateInstance<SimulationConfig>();
            try
            {
                var experiment = ExperimentConfiguration.CreateDefault();
                experiment.SetInitialPopulation(1, 1);
                experiment.HerbivorePolicy = AgentPolicyKind.LearnedPpo;
                experiment.PredatorPolicy = AgentPolicyKind.ScriptedBaseline;
                experiment.RandomSeed = 21;
                config.ApplyExperiment(experiment);

                var root = new GameObject("PolicySpawnRoot");
                objects.Add(root);
                var tracker = root.AddComponent<PopulationTracker>();
                var hub = root.AddComponent<CreatureLifecycleHub>();
                hub.Bind(tracker);
                var spawner = root.AddComponent<CreatureSpawner>();
                spawner.SetSeed(experiment.Seeds.FounderGenomes);
                spawner.SetPolicyMasterSeed(experiment.RandomSeed);
                var prefab = new GameObject("PolicyPrefab");
                objects.Add(prefab);
                prefab.AddComponent<CreatureIdentity>();
                prefab.AddComponent<CreatureVitals>();
                prefab.AddComponent<CreatureGenome>();
                prefab.AddComponent<TestPolicyOwner>();
                spawner.Configure(tracker, hub, vitals: vitals);

                InitialPopulationSpawner.SpawnFounders(
                    spawner,
                    config,
                    prefab,
                    prefab,
                    Vector3.zero,
                    new System.Random(config.RandomSeed));

                var owners = Object.FindObjectsOfType<TestPolicyOwner>();
                var herbivores = 0;
                var predators = 0;
                for (var i = 0; i < owners.Length; i++)
                {
                    var identity = owners[i].GetComponent<CreatureIdentity>();
                    if (identity == null || identity.Id.Value <= 0)
                    {
                        continue;
                    }
                    {
                        herbivores++;
                        Assert.AreEqual(AgentPolicyKind.LearnedPpo, owners[i].PolicyKind);
                    }
                    else
                    {
                        predators++;
                        Assert.AreEqual(AgentPolicyKind.ScriptedBaseline, owners[i].PolicyKind);
                    }

                    Assert.AreEqual(
                        DeterministicSeeds.ScriptedWander(experiment.RandomSeed, identity.Id.Value),
                        owners[i].Seed);
                }

                Assert.AreEqual(1, herbivores);
                Assert.AreEqual(1, predators);
            }
            finally
            {
                for (var i = 0; i < objects.Count; i++)
                {
                    if (objects[i] != null)
                    {
                        Object.DestroyImmediate(objects[i]);
                    }
                }

                var leftovers = Object.FindObjectsOfType<CreatureIdentity>();
                for (var i = 0; i < leftovers.Length; i++)
                {
                    if (leftovers[i] != null)
                    {
                        Object.DestroyImmediate(leftovers[i].gameObject);
                    }
                }

                Object.DestroyImmediate(vitals);
                Object.DestroyImmediate(config);
            }
        }
    }

    sealed class TestPolicyOwner : MonoBehaviour, IPolicyKindOwner, IPolicySeedOwner
    {
        public AgentPolicyKind PolicyKind { get; private set; }
        public int Seed { get; private set; }
        public void SetPolicyKind(AgentPolicyKind kind) => PolicyKind = kind;
        public void SetPolicySeed(int seed) => Seed = seed;
    }

    public sealed class ExperimentValidationTests
    {
        [Test]
        public void DefaultConfiguration_IsValid()
        {
            Assert.IsTrue(ExperimentConfigurationValidator.IsValid(ExperimentConfiguration.CreateDefault()));
            Assert.IsTrue(ExperimentConfigurationValidator.IsValid(ExperimentScenarios.Create(ExperimentScenarios.NormalControl)));
        }

        [Test]
        public void InvalidNameCountsAndMutation_AreRejected()
        {
            var config = ExperimentConfiguration.CreateDefault();
            config.ExperimentName = " ";
            config.InitialHerbivores = -1;
            config.MutationProbability = 1.5f;
            config.DayLengthSeconds = 0f;
            var errors = ExperimentConfigurationValidator.Validate(config);
            Assert.GreaterOrEqual(errors.Count, 3);
        }

        [Test]
        public void TrainingRespawnRequiresTrainingSupportMode()
        {
            var config = ExperimentConfiguration.CreateDefault();
            config.EcosystemMode = EcosystemMode.Persistent;
            config.TrainingRespawnEnabled = true;
            var errors = ExperimentConfigurationValidator.Validate(config);
            StringAssert.Contains("training respawn", string.Join(" ", errors));
        }

        [Test]
        public void UnknownEventKind_IsRejected()
        {
            var config = ExperimentConfiguration.CreateDefault();
            config.EnabledEnvironmentalEvents = new[] { "tornado" };
            var errors = ExperimentConfigurationValidator.Validate(config);
            StringAssert.Contains("tornado", string.Join(" ", errors));
        }

        [Test]
        public void InvalidPolicyJson_Throws()
        {
            const string json = "{\"experiment_name\":\"x\",\"herbivore_policy\":\"magic\",\"predator_policy\":\"scripted_baseline\",\"day_length_seconds\":120,\"ecosystem_mode\":\"persistent_ecosystem\"}";
            Assert.Throws<ExperimentConfigurationException>(() => ExperimentConfigurationSerializer.FromJson(json));
        }

        [Test]
        public void InitialCountsCannotExceedCaps()
        {
            var config = ExperimentConfiguration.CreateDefault();
            config.MaxHerbivores = 5;
            config.InitialHerbivores = 9;
            var errors = ExperimentConfigurationValidator.Validate(config);
            StringAssert.Contains("initial herbivores", string.Join(" ", errors));
        }

        [Test]
        public void UnknownCurriculumStage_IsRejected()
        {
            var config = ExperimentConfiguration.CreateDefault();
            config.CurriculumStageId = "stage99_unknown";
            var errors = ExperimentConfigurationValidator.Validate(config);
            StringAssert.Contains("stage99_unknown", string.Join(" ", errors));
            Assert.IsFalse(TrainingCurriculum.TryCreate("stage99_unknown", TrainingCurriculumFocus.Combined, out _));
        }
    }

    public sealed class TrainingCurriculumTests
    {
        [Test]
        public void SixStages_ExistAndStayLightweight()
        {
            for (var stage = 1; stage <= 6; stage++)
            {
                var config = TrainingCurriculum.Create(stage, TrainingCurriculumFocus.Herbivore);
                Assert.IsTrue(ExperimentConfigurationValidator.IsValid(config), string.Join(" ", ExperimentConfigurationValidator.Validate(config)));
                Assert.LessOrEqual(config.InitialHerbivores + config.InitialPredators, 40);
            }
        }

        [Test]
        public void Stage1_HasNoPredatorsForHerbivoreFocus()
        {
            var stage1 = TrainingCurriculum.Create(1, TrainingCurriculumFocus.Herbivore);
            Assert.AreEqual(0, stage1.InitialPredators);
            Assert.AreEqual(EcosystemMode.TrainingSupport, stage1.EcosystemMode);
            Assert.IsTrue(stage1.TrainingRespawnEnabled);
        }

        [Test]
        public void Stage5_IsPersistentWithoutRespawn()
        {
            var stage5 = TrainingCurriculum.Create(5, TrainingCurriculumFocus.Combined);
            Assert.AreEqual(EcosystemMode.Persistent, stage5.EcosystemMode);
            Assert.IsFalse(stage5.TrainingRespawnEnabled);
            Assert.IsTrue(stage5.Stopping.StopOnEcosystemExtinction);
        }

        [Test]
        public void Stage6_EnablesReproductionAndEvents()
        {
            var stage6 = TrainingCurriculum.Create(6, TrainingCurriculumFocus.Combined);
            Assert.Greater(stage6.MutationProbability, 0f);
            Assert.AreEqual(2, stage6.ScheduledEvents.Length);
        }
    }

    public sealed class ExperimentEnvironmentApplicatorTests
    {
        [Test]
        public void ApplyResources_ScalesDensityFromAbundance()
        {
            var go = new GameObject("ResourceApply");
            try
            {
                var resources = go.AddComponent<EvoLife.Environment.ResourceManager>();
                var experiment = ExperimentConfiguration.CreateDefault();
                experiment.RandomSeed = 11;
                experiment.ResourceAbundance = 0.5f;
                experiment.PlantRegenerationMultiplier = 2f;
                ExperimentEnvironmentApplicator.ApplyResources(experiment, resources);
                Assert.AreEqual(experiment.Seeds.ResourceSpawn, resources.SpawnSettings.Seed);
                Assert.AreEqual(
                    ExperimentEnvironmentApplicator.BaselinePlantDensity * 0.5f,
                    resources.SpawnSettings.DefaultDensity,
                    0.0001f);
                Assert.AreEqual(
                    ExperimentEnvironmentApplicator.BaselineRegenPerSecond * 2f,
                    resources.SpawnSettings.DefaultRegenPerSecond,
                    0.0001f);
                Assert.AreEqual(0, resources.PlaceResourcesCallCount);
                Assert.IsFalse(resources.HasPlaced);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void BuildSchedule_MapsNamedEvents()
        {
            var experiment = ExperimentScenarios.Create(ExperimentScenarios.RecoveryAfterEvent);
            var schedule = new List<EvoLife.Environment.ScheduledEnvironmentalEvent>(
                ExperimentEnvironmentApplicator.BuildSchedule(experiment));
            Assert.AreEqual(2, schedule.Count);
            Assert.AreEqual(EnvironmentalEventKind.Drought, schedule[0].Kind);
            Assert.AreEqual(EnvironmentalEventKind.FoodBoom, schedule[1].Kind);
        }
    }
}
