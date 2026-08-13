using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using EvoLife.Common;
using EvoLife.Creatures;
using EvoLife.Environment;
using EvoLife.Genetics;
using EvoLife.Simulation;

namespace EvoLife.Tests
{
    public sealed class ExperimentSimulationProgressionTests
    {
        [Test]
        public void OnlyRunning_AllowsTimeProgression()
        {
            Assert.IsTrue(ExperimentSimulationProgression.AllowsTimeProgression(ExperimentRunPhase.Running));
            Assert.IsFalse(ExperimentSimulationProgression.ShouldPauseClock(ExperimentRunPhase.Running));

            var paused = new[]
            {
                ExperimentRunPhase.Created,
                ExperimentRunPhase.Loaded,
                ExperimentRunPhase.EnvironmentInitialized,
                ExperimentRunPhase.PopulationInitialized,
                ExperimentRunPhase.AnalyticsStarted,
                ExperimentRunPhase.Stopping,
                ExperimentRunPhase.Finished
            };
            for (var i = 0; i < paused.Length; i++)
            {
                Assert.IsFalse(ExperimentSimulationProgression.AllowsTimeProgression(paused[i]), paused[i].ToString());
                Assert.IsTrue(ExperimentSimulationProgression.ShouldPauseClock(paused[i]), paused[i].ToString());
            }
        }

        [Test]
        public void SecondBegin_IsRejectedAfterInitializationStarts()
        {
            Assert.IsFalse(ExperimentSimulationProgression.RejectsSecondBegin(ExperimentRunPhase.Created));
            Assert.IsFalse(ExperimentSimulationProgression.RejectsSecondBegin(ExperimentRunPhase.Loaded));
            Assert.IsTrue(ExperimentSimulationProgression.RejectsSecondBegin(ExperimentRunPhase.EnvironmentInitialized));
            Assert.IsTrue(ExperimentSimulationProgression.RejectsSecondBegin(ExperimentRunPhase.Running));
            Assert.IsTrue(ExperimentSimulationProgression.RejectsSecondBegin(ExperimentRunPhase.Finished));
        }

        [Test]
        public void Coordinator_StaysPausedUntilBeginRunning()
        {
            var coordinator = new ExperimentCoordinator();
            Assert.IsTrue(coordinator.ShouldPauseClock);
            coordinator.Load(ExperimentConfiguration.CreateDefault());
            Assert.IsFalse(coordinator.AllowsSimulationProgression);
            coordinator.MarkEnvironmentInitialized();
            coordinator.MarkPopulationInitialized();
            coordinator.MarkAnalyticsStarted("run");
            Assert.IsTrue(coordinator.ShouldPauseClock);
            coordinator.BeginRunning();
            Assert.IsTrue(coordinator.AllowsSimulationProgression);
            Assert.IsFalse(coordinator.ShouldPauseClock);
        }
    }

    public sealed class ExperimentLifecycleTests
    {
        [Test]
        public void Initialization_BeginsPausedAndUnpausesOnlyAtRunning()
        {
            using (var fixture = new ExperimentLifecycleFixture())
            {
                fixture.Clock.SetPaused(false);
                fixture.Orchestrator.Load(ExperimentConfiguration.CreateDefault());

                Assert.IsTrue(fixture.Clock.IsPaused);
                Assert.AreEqual(ExperimentRunPhase.Loaded, fixture.Orchestrator.State.Phase);
                fixture.Clock.Advance(5f);
                Assert.AreEqual(0f, fixture.Clock.SimulationTimeSeconds, 0.0001f);

                fixture.Orchestrator.InitializeEnvironment();
                fixture.Orchestrator.InitializePopulation();
                Assert.IsTrue(fixture.Clock.IsPaused);
                var started = fixture.Orchestrator.StartAnalyticsAsync().GetAwaiter().GetResult();
                Assert.IsTrue(started);
                Assert.AreEqual(ExperimentRunPhase.AnalyticsStarted, fixture.Orchestrator.State.Phase);
                Assert.IsTrue(fixture.Clock.IsPaused);
                Assert.IsFalse(fixture.Orchestrator.Coordinator.AllowsSimulationProgression);

                fixture.Orchestrator.BeginRunning();
                Assert.AreEqual(ExperimentRunPhase.Running, fixture.Orchestrator.State.Phase);
                Assert.IsFalse(fixture.Clock.IsPaused);
                fixture.Clock.Advance(2f);
                Assert.AreEqual(2f, fixture.Clock.SimulationTimeSeconds, 0.0001f);
            }
        }

        [Test]
        public void BeginAsync_RemainsPausedUntilAnalyticsCompletes()
        {
            using (var fixture = new ExperimentLifecycleFixture())
            {
                fixture.Analytics.DelayBegin = true;
                var task = fixture.Orchestrator.BeginAsync(ExperimentConfiguration.CreateDefault());

                Assert.IsFalse(task.IsCompleted);
                Assert.AreEqual(ExperimentRunPhase.PopulationInitialized, fixture.Orchestrator.State.Phase);
                Assert.IsTrue(fixture.Clock.IsPaused);
                fixture.Clock.Advance(4f);
                Assert.AreEqual(0f, fixture.Clock.SimulationTimeSeconds, 0.0001f);

                fixture.Analytics.CompleteBegin(true);
                var state = task.GetAwaiter().GetResult();
                Assert.AreEqual(ExperimentRunPhase.Running, state.Phase);
                Assert.IsFalse(fixture.Clock.IsPaused);
            }
        }

        [Test]
        public void FailedAnalytics_NeverTransitionsToRunning()
        {
            using (var fixture = new ExperimentLifecycleFixture())
            {
                fixture.Analytics.BeginResult = false;
                LogAssert.Expect(LogType.Error, new Regex("BeginAsync returned false"));
                LogAssert.Expect(LogType.Error, new Regex("analytics startup failed"));

                var state = fixture.Orchestrator.BeginAsync(ExperimentConfiguration.CreateDefault())
                    .GetAwaiter()
                    .GetResult();

                Assert.AreNotEqual(ExperimentRunPhase.Running, state.Phase);
                Assert.AreEqual(ExperimentRunPhase.PopulationInitialized, state.Phase);
                Assert.IsTrue(fixture.Clock.IsPaused);
                fixture.Clock.Advance(3f);
                Assert.AreEqual(0f, fixture.Clock.SimulationTimeSeconds, 0.0001f);
            }
        }

        [Test]
        public void ThrownAnalytics_NeverTransitionsToRunning()
        {
            using (var fixture = new ExperimentLifecycleFixture())
            {
                fixture.Analytics.ThrowOnBegin = true;
                LogAssert.Expect(LogType.Error, new Regex("analytics startup failed"));
                LogAssert.Expect(LogType.Error, new Regex("will not enter Running"));

                var state = fixture.Orchestrator.BeginAsync(ExperimentConfiguration.CreateDefault())
                    .GetAwaiter()
                    .GetResult();

                Assert.AreNotEqual(ExperimentRunPhase.Running, state.Phase);
                Assert.IsTrue(fixture.Clock.IsPaused);
            }
        }

        [Test]
        public void FinishAsync_PausesTheClock()
        {
            using (var fixture = new ExperimentLifecycleFixture())
            {
                fixture.Orchestrator.BeginAsync(ExperimentConfiguration.CreateDefault()).GetAwaiter().GetResult();
                Assert.IsFalse(fixture.Clock.IsPaused);

                var finished = fixture.Orchestrator.FinishAsync(ExperimentStopReason.MaxSimulationTime)
                    .GetAwaiter()
                    .GetResult();

                Assert.AreEqual(ExperimentRunPhase.Finished, finished.Phase);
                Assert.IsTrue(fixture.Clock.IsPaused);
                Assert.AreEqual(1, fixture.Analytics.FinishCalls);
            }
        }

        [Test]
        public void ManualStop_FinishesAndPauses()
        {
            using (var fixture = new ExperimentLifecycleFixture())
            {
                fixture.Orchestrator.BeginAsync(ExperimentConfiguration.CreateDefault()).GetAwaiter().GetResult();
                fixture.Orchestrator.RequestManualStop();
                fixture.Orchestrator.FinishAsync(ExperimentStopReason.ManualStop).GetAwaiter().GetResult();

                Assert.AreEqual(ExperimentRunPhase.Finished, fixture.Orchestrator.State.Phase);
                Assert.AreEqual(ExperimentStopReason.ManualStop, fixture.Orchestrator.State.StopReason);
                Assert.IsTrue(fixture.Clock.IsPaused);
            }
        }

        [Test]
        public void SecondBeginAsync_IsRejected()
        {
            using (var fixture = new ExperimentLifecycleFixture())
            {
                var first = fixture.Orchestrator.BeginAsync(ExperimentConfiguration.CreateDefault())
                    .GetAwaiter()
                    .GetResult();
                Assert.AreEqual(ExperimentRunPhase.Running, first.Phase);
                Assert.IsFalse(fixture.Clock.IsPaused);
                var births = fixture.Tracker.Births;

                LogAssert.Expect(LogType.Error, new Regex("BeginAsync rejected"));
                var second = fixture.Orchestrator.BeginAsync(ExperimentConfiguration.CreateDefault())
                    .GetAwaiter()
                    .GetResult();

                Assert.AreEqual(ExperimentRunPhase.Running, second.Phase);
                Assert.IsFalse(fixture.Clock.IsPaused);
                Assert.AreEqual(births, fixture.Tracker.Births);
                Assert.AreEqual(1, fixture.Analytics.BeginCalls);
                Assert.AreEqual(1, fixture.Resources.PlaceResourcesCallCount);
            }
        }

        [Test]
        public void EnvironmentApplicator_IsInvokedOncePerInitialization()
        {
            using (var fixture = new ExperimentLifecycleFixture())
            {
                fixture.Orchestrator.Load(ExperimentConfiguration.CreateDefault());
                fixture.Orchestrator.InitializeEnvironment();
                Assert.AreEqual(1, fixture.Resources.PlaceResourcesCallCount);
                Assert.IsTrue(fixture.Resources.HasPlaced);
                var plants = fixture.Resources.Plants.Count;

                fixture.Orchestrator.InitializeEnvironment();
                fixture.Ecosystem.ApplyExperimentSettings();
                Assert.AreEqual(1, fixture.Resources.PlaceResourcesCallCount);
                Assert.AreEqual(plants, fixture.Resources.Plants.Count);
            }
        }

        [Test]
        public void ApplyExperimentSettings_DoesNotPlaceResources()
        {
            using (var fixture = new ExperimentLifecycleFixture())
            {
                fixture.Ecosystem.ApplyExperimentSettings();
                Assert.AreEqual(0, fixture.Resources.PlaceResourcesCallCount);
                Assert.IsFalse(fixture.Resources.HasPlaced);
            }
        }

        [Test]
        public void InitializePopulation_SpawnsFoundersOnce()
        {
            using (var fixture = new ExperimentLifecycleFixture(herbivores: 2, predators: 0))
            {
                fixture.Orchestrator.Load(fixture.Config.ToExperimentConfiguration());
                fixture.Orchestrator.InitializeEnvironment();
                fixture.Orchestrator.InitializePopulation();
                Assert.AreEqual(2, fixture.Tracker.Births);
                fixture.Orchestrator.InitializePopulation();
                Assert.AreEqual(2, fixture.Tracker.Births);
                Assert.AreEqual(2, fixture.Tracker.HerbivoreCount);
            }
        }

        sealed class ExperimentLifecycleFixture : IDisposable
        {
            readonly List<GameObject> objects = new List<GameObject>();
            readonly SpeciesVitalsDefinition vitals;

            public ExperimentLifecycleFixture(int herbivores = 1, int predators = 0)
            {
                var root = NewObject("ExperimentRoot");
                Tracker = root.AddComponent<PopulationTracker>();
                Hub = root.AddComponent<CreatureLifecycleHub>();
                Hub.Bind(Tracker);
                Clock = root.AddComponent<SimulationClock>();
                Spawner = root.AddComponent<CreatureSpawner>();
                Reproduction = root.AddComponent<ReproductionSystem>();
                Resources = root.AddComponent<ResourceManager>();
                Resources.PlaceOnStart = true;
                DayNight = root.AddComponent<DayNightManager>();
                Events = root.AddComponent<EnvironmentalEventManager>();
                Ecosystem = root.AddComponent<EcosystemManager>();
                Orchestrator = root.AddComponent<ExperimentOrchestrator>();
                Analytics = root.AddComponent<StubExperimentAnalytics>();
                vitals = ScriptableObject.CreateInstance<SpeciesVitalsDefinition>();
                Config = ScriptableObject.CreateInstance<SimulationConfig>();
                Config.SetInitialPopulation(herbivores, predators);
                var prefab = CreatePrefab();
                Spawner.Configure(Tracker, Hub, Reproduction, vitals);
                Reproduction.Configure(
                    Spawner,
                    Tracker,
                    Hub,
                    Clock,
                    ReproductionSettings.ForTests(),
                    Config.Ecosystem,
                    Config,
                    prefab,
                    prefab);
                Ecosystem.Configure(
                    Config,
                    Clock,
                    Spawner,
                    Tracker,
                    Hub,
                    Reproduction,
                    null,
                    prefab,
                    prefab,
                    Resources,
                    DayNight,
                    Events);
                Ecosystem.SpawnFoundersOnStart = true;
                Ecosystem.ApplyEnvironmentOnStart = true;
                Orchestrator.Configure(
                    Config,
                    Clock,
                    Ecosystem,
                    Resources,
                    DayNight,
                    Events,
                    null,
                    Reproduction,
                    Tracker,
                    Analytics,
                    startAutomatically: false,
                    spawnFounderPopulation: true);
                Assert.IsFalse(Ecosystem.SpawnFoundersOnStart);
                Assert.IsFalse(Ecosystem.ApplyEnvironmentOnStart);
                Assert.IsFalse(Resources.PlaceOnStart);
                Assert.IsTrue(Clock.IsPaused);
            }

            public SimulationConfig Config { get; }
            public SimulationClock Clock { get; }
            public PopulationTracker Tracker { get; }
            public CreatureLifecycleHub Hub { get; }
            public CreatureSpawner Spawner { get; }
            public ReproductionSystem Reproduction { get; }
            public ResourceManager Resources { get; }
            public DayNightManager DayNight { get; }
            public EnvironmentalEventManager Events { get; }
            public EcosystemManager Ecosystem { get; }
            public ExperimentOrchestrator Orchestrator { get; }
            public StubExperimentAnalytics Analytics { get; }

            public void Dispose()
            {
                var identities = UnityEngine.Object.FindObjectsOfType<CreatureIdentity>();
                for (var i = 0; i < identities.Length; i++)
                {
                    if (identities[i] != null)
                    {
                        UnityEngine.Object.DestroyImmediate(identities[i].gameObject);
                    }
                }

                for (var i = 0; i < objects.Count; i++)
                {
                    if (objects[i] != null)
                    {
                        UnityEngine.Object.DestroyImmediate(objects[i]);
                    }
                }

                if (vitals != null)
                {
                    UnityEngine.Object.DestroyImmediate(vitals);
                }

                if (Config != null)
                {
                    UnityEngine.Object.DestroyImmediate(Config);
                }
            }

            GameObject NewObject(string name)
            {
                var go = new GameObject(name);
                objects.Add(go);
                return go;
            }

            GameObject CreatePrefab()
            {
                var prefab = NewObject("FounderPrefab");
                prefab.AddComponent<CreatureIdentity>();
                prefab.AddComponent<CreatureVitals>();
                prefab.AddComponent<CreatureGenome>();
                prefab.AddComponent<CreatureReproductionBridge>();
                return prefab;
            }
        }
    }

    sealed class StubExperimentAnalytics : MonoBehaviour, IExperimentAnalyticsSession
    {
        public int BeginCalls;
        public int FinishCalls;
        public bool DelayBegin;
        public bool BeginResult = true;
        public bool ThrowOnBegin;
        TaskCompletionSource<bool> beginGate;

        public string RunId { get; set; } = "stub-run";
        public bool RunReady { get; set; } = true;

        public void SetAutoStart(bool enabled)
        {
        }

        public Task<bool> BeginAsync()
        {
            BeginCalls++;
            if (ThrowOnBegin)
            {
                throw new InvalidOperationException("analytics failed");
            }

            if (DelayBegin)
            {
                beginGate = beginGate ?? new TaskCompletionSource<bool>();
                return beginGate.Task;
            }

            return Task.FromResult(BeginResult);
        }

        public void CompleteBegin(bool result)
        {
            beginGate = beginGate ?? new TaskCompletionSource<bool>();
            beginGate.TrySetResult(result);
        }

        public Task<bool> FinishAsync(string status, string stopReason)
        {
            FinishCalls++;
            return Task.FromResult(true);
        }
    }
}
