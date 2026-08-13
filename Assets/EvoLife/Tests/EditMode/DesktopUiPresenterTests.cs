using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using EvoLife.Common;
using EvoLife.UI;

namespace EvoLife.Tests
{
    public sealed class CreatureInspectorPresenterTests
    {
        [Test]
        public void Build_NoSelection_ShowsEmptyReason()
        {
            var model = CreatureInspectorPresenter.Build(null);
            Assert.IsFalse(model.HasSelection);
            Assert.AreEqual(CreatureInspectorPresenter.NoSelection, model.EmptyReason);
            Assert.AreEqual(CreatureInspectorPresenter.Unavailable, model.CreatureId);
            Assert.AreEqual(CanonicalTraitNames.InSchemaOrder.Length, model.Traits.Length);
        }

        [Test]
        public void Build_ClearedSelection_UsesClearedReason()
        {
            var model = CreatureInspectorPresenter.Build(new SelectedCreatureSnapshot
            {
                HasSelection = true,
                HostDestroyed = true
            });
            Assert.IsFalse(model.HasSelection);
            Assert.AreEqual(CreatureInspectorPresenter.SelectionCleared, model.EmptyReason);
        }

        [Test]
        public void Build_LivingCreature_CopiesIdentityBiologyAndPolicy()
        {
            var snapshot = LivingSnapshot();
            var model = CreatureInspectorPresenter.Build(snapshot);
            Assert.IsTrue(model.HasSelection);
            Assert.AreEqual("Creature:7", model.CreatureId);
            Assert.AreEqual("deer", model.Species);
            Assert.AreEqual("herbivore", model.Role);
            Assert.AreEqual("2", model.Generation);
            Assert.AreEqual("Creature:1", model.ParentA);
            Assert.AreEqual("Creature:2", model.ParentB);
            Assert.AreEqual("3", model.OffspringCount);
            Assert.AreEqual("ScriptedBaseline", model.PolicyKind);
            Assert.AreEqual(PolicyKindNames.ScriptedBaseline, model.PolicyWireName);
            Assert.AreEqual("alive", model.Alive);
            Assert.AreEqual("—", model.DeathCause);
            Assert.AreEqual("10 / 100", model.Age);
            Assert.AreEqual("80 / 100", model.Health);
            Assert.AreEqual("Walking", model.CurrentActivity);
            Assert.AreEqual("SeekFood", model.ScriptedMotive);
            Assert.AreEqual("0.50", model.Forward);
            Assert.AreEqual("eat", model.InteractionRequest);
            StringAssert.Contains("IDENTITY", model.SummaryText);
        }

        [Test]
        public void Build_DeadCreature_ShowsDeathCause()
        {
            var snapshot = LivingSnapshot();
            ((StubVitalState)snapshot.Vitals).IsAlive = false;
            ((StubVitalState)snapshot.Vitals).CauseOfDeath = DeathCause.Starvation;
            var model = CreatureInspectorPresenter.Build(snapshot);
            Assert.AreEqual("dead", model.Alive);
            Assert.AreEqual("starvation", model.DeathCause);
        }

        [Test]
        public void Build_MissingAiDebug_MarksDecisionFieldsUnavailable()
        {
            var snapshot = LivingSnapshot();
            snapshot.AiDebug = null;
            snapshot.Activity = null;
            snapshot.Episode = null;
            snapshot.LivingOffspringCount = null;
            var model = CreatureInspectorPresenter.Build(snapshot);
            Assert.AreEqual(CreatureInspectorPresenter.Unavailable, model.ControlMode);
            Assert.AreEqual(CreatureInspectorPresenter.Unavailable, model.Forward);
            Assert.AreEqual(CreatureInspectorPresenter.Unavailable, model.ScriptedMotive);
            Assert.AreEqual(CreatureInspectorPresenter.Unavailable, model.CurrentActivity);
            Assert.AreEqual(CreatureInspectorPresenter.Unavailable, model.EpisodeReturn);
            Assert.AreEqual(CreatureInspectorPresenter.Unavailable, model.OffspringCount);
        }

        [Test]
        public void CanonicalTraits_AreLookedUpInSchemaOrder()
        {
            var genome = new StubGenomeTraits(
                (CanonicalTraitNames.MaximumAge, 9f),
                (CanonicalTraitNames.BaseMovementSpeed, 1.5f),
                (CanonicalTraitNames.Aggression, 0.4f));
            var traits = CreatureInspectorPresenter.BuildTraits(genome);
            Assert.AreEqual(CanonicalTraitNames.InSchemaOrder.Length, traits.Length);
            for (var i = 0; i < traits.Length; i++)
            {
                Assert.AreEqual(CanonicalTraitNames.InSchemaOrder[i], traits[i].Name);
            }

            Assert.IsTrue(traits[0].Found);
            Assert.AreEqual("1.5", traits[0].Value);
            Assert.IsFalse(traits[1].Found);
            Assert.AreEqual(CreatureInspectorPresenter.Unavailable, traits[1].Value);
        }

        [Test]
        public void CountLivingOffspring_MatchesParentIds()
        {
            var parent = new CreatureId(10);
            var views = new IAnalyticsCreatureView[]
            {
                new StubAnalyticsView
                {
                    Lineage = new StubLineage { ParentA = parent, ParentB = new CreatureId(11) }
                },
                new StubAnalyticsView
                {
                    Lineage = new StubLineage { ParentA = new CreatureId(3), ParentB = parent }
                },
                new StubAnalyticsView
                {
                    Lineage = new StubLineage { ParentA = new CreatureId(1), ParentB = new CreatureId(2) }
                }
            };
            Assert.AreEqual(2, CreatureInspectorPresenter.CountLivingOffspring(parent, views));
            Assert.AreEqual(0, CreatureInspectorPresenter.CountLivingOffspring(parent, null));
        }

        static SelectedCreatureSnapshot LivingSnapshot()
        {
            return new SelectedCreatureSnapshot
            {
                HasSelection = true,
                Identity = new StubIdentity
                {
                    Id = new CreatureId(7),
                    Role = CreatureRole.Herbivore,
                    SpeciesId = "deer"
                },
                Lineage = new StubLineage
                {
                    Generation = 2,
                    ParentA = new CreatureId(1),
                    ParentB = new CreatureId(2)
                },
                Policy = new StubPolicyOwner { PolicyKind = AgentPolicyKind.ScriptedBaseline },
                Vitals = new StubVitalState
                {
                    IsAlive = true,
                    Age = 10f,
                    MaxAge = 100f,
                    Health = 80f,
                    MaxHealth = 100f,
                    Hunger = 20f,
                    MaxHunger = 100f,
                    Thirst = 15f,
                    MaxThirst = 100f,
                    Energy = 50f,
                    MaxEnergy = 100f
                },
                Activity = new StubActivity { CurrentActivity = "Walking" },
                Genome = new StubGenomeTraits(
                    (CanonicalTraitNames.BaseMovementSpeed, 2f),
                    (CanonicalTraitNames.SprintSpeed, 4f)),
                Episode = new StubEpisodeMetrics { HasEpisodeReturn = false },
                AiDebug = new StubAiDebug
                {
                    ControlMode = "ScriptedBaseline",
                    BehaviorName = "EvoLifeHerbivore",
                    Forward = 0.5f,
                    Turn = -0.25f,
                    SprintOrEffort = 0.1f,
                    InteractionRequest = "eat",
                    HasScriptedMotive = true,
                    ScriptedMotive = "SeekFood"
                },
                LivingOffspringCount = 3
            };
        }
    }

    public sealed class PolicyDisplayFormatterTests
    {
        [Test]
        public void FormatKind_DoesNotRankPolicies()
        {
            Assert.AreEqual("ScriptedBaseline", PolicyDisplayFormatter.FormatKind(AgentPolicyKind.ScriptedBaseline));
            Assert.AreEqual("LearnedPpo", PolicyDisplayFormatter.FormatKind(AgentPolicyKind.LearnedPpo));
            Assert.AreEqual(PolicyKindNames.LearnedPpo, PolicyDisplayFormatter.FormatWireName(AgentPolicyKind.LearnedPpo));
            StringAssert.Contains("model-a", PolicyDisplayFormatter.FormatKindAndModel(AgentPolicyKind.LearnedPpo, "model-a"));
            Assert.AreEqual("ScriptedBaseline", PolicyDisplayFormatter.FormatKindAndModel(AgentPolicyKind.ScriptedBaseline, "model-a"));
        }

        [Test]
        public void FormatKind_NullOwner_IsUnavailable()
        {
            Assert.AreEqual(PolicyDisplayFormatter.Unavailable, PolicyDisplayFormatter.FormatKind((IPolicyKindOwner)null));
            Assert.AreEqual(PolicyDisplayFormatter.Unavailable, PolicyDisplayFormatter.FormatModelId(""));
        }
    }

    public sealed class RatioFormatterTests
    {
        [Test]
        public void PredatorPrey_ZeroPopulation_IsNotAvailable()
        {
            Assert.AreEqual(RatioFormatter.NotAvailable, RatioFormatter.PredatorPrey(0, 0));
        }

        [Test]
        public void PredatorPrey_ZeroDenominator_IsNotAvailable()
        {
            Assert.AreEqual(RatioFormatter.NotAvailable, RatioFormatter.PredatorPrey(4, 0));
        }

        [Test]
        public void PredatorPrey_NormalRatio()
        {
            Assert.AreEqual("0.25", RatioFormatter.PredatorPrey(1, 4));
        }
    }

    public sealed class EventPanelPresenterTests
    {
        [Test]
        public void FormatActiveList_Empty_IsNone()
        {
            Assert.AreEqual("none", EventPanelPresenter.FormatActiveList(null, 10f));
            Assert.AreEqual("none", EventPanelPresenter.FormatActiveList(new IReadOnlyEnvironmentalEvent[0], 10f));
        }

        [Test]
        public void FormatActiveList_IncludesRemainingDuration()
        {
            var events = new IReadOnlyEnvironmentalEvent[]
            {
                new StubEvent
                {
                    Kind = EnvironmentalEventKind.Drought,
                    IsActive = true,
                    StartedAtSimulationTime = 5f,
                    EndsAtSimulationTime = 20f
                },
                new StubEvent
                {
                    Kind = EnvironmentalEventKind.Wildfire,
                    IsActive = true,
                    StartedAtSimulationTime = 10f,
                    EndsAtSimulationTime = 10f
                }
            };
            var text = EventPanelPresenter.FormatActiveList(events, 8f);
            StringAssert.Contains("drought (12.0s remaining)", text);
            StringAssert.Contains("wildfire", text);
        }

        [Test]
        public void TriggerableKinds_CoverConfiguredEvents()
        {
            Assert.AreEqual(7, EventPanelPresenter.TriggerableKinds.Length);
            Assert.AreEqual("heat_wave", EventPanelPresenter.FormatKind(EnvironmentalEventKind.HeatWave));
        }
    }

    public sealed class SimulationControlPresenterTests
    {
        [Test]
        public void SpeedPresets_AreOneTwoFiveTen()
        {
            CollectionAssert.AreEqual(
                new[] { 1f, 2f, 5f, 10f },
                SimulationSpeedPresets.Values);
        }

        [Test]
        public void Build_ReadsClockAndMarksReloadRequired()
        {
            var clock = new StubClock { SimulationTimeSeconds = 12.5f, TimeScale = 2f, IsPaused = true };
            var model = SimulationControlPresenter.Build(clock, "run-a", "drought", "Running");
            Assert.AreEqual(12.5f, model.SimulationTimeSeconds);
            Assert.AreEqual(2f, model.TimeScale);
            Assert.IsTrue(model.IsPaused);
            Assert.AreEqual("paused", model.StatusLabel);
            Assert.AreEqual("run-a", model.ExperimentName);
            Assert.AreEqual("drought", model.Scenario);
            Assert.AreEqual("Running", model.RunState);
            Assert.IsTrue(model.RestartRequiresSceneReload);
            StringAssert.Contains("Reload the scene", model.RestartNote);
        }

        [Test]
        public void PauseResumeAndSpeed_UseClockControl()
        {
            var clock = new StubClock { TimeScale = 1f };
            SimulationControlPresenter.Pause(clock);
            Assert.IsTrue(clock.IsPaused);
            SimulationControlPresenter.Resume(clock);
            Assert.IsFalse(clock.IsPaused);
            SimulationControlPresenter.SetSpeed(clock, 5f);
            Assert.AreEqual(5f, clock.TimeScale);
        }
    }

    public sealed class ChartRingBufferTests
    {
        [Test]
        public void Capacity_IsBoundedAndDropsOldest()
        {
            var buffer = new ChartRingBuffer(3);
            Assert.AreEqual(3, buffer.Capacity);
            buffer.Push(1f);
            buffer.Push(2f);
            buffer.Push(3f);
            buffer.Push(4f);
            Assert.AreEqual(3, buffer.Count);
            var copy = new float[8];
            var written = buffer.CopyChronological(copy);
            Assert.AreEqual(3, written);
            Assert.AreEqual(2f, copy[0]);
            Assert.AreEqual(3f, copy[1]);
            Assert.AreEqual(4f, copy[2]);
            Assert.AreEqual(4f, buffer.Latest());
        }

        [Test]
        public void Sampler_RespectsIntervalAndCapacity()
        {
            var sampler = new DashboardChartSampler(4);
            Assert.AreEqual(4, sampler.Capacity);
            var model = new DashboardModel
            {
                HerbivoresAlive = 1,
                PredatorsAlive = 2,
                Births = 3,
                Deaths = 4,
                PlantAbundance = "0.50"
            };
            Assert.IsTrue(sampler.TrySample(0f, model, 1f, 1.5f));
            Assert.IsFalse(sampler.TrySample(0.5f, model, 1f, 1.5f));
            Assert.IsTrue(sampler.TrySample(1f, model, 1f, 1.6f));
            StringAssert.Contains("1", sampler.HerbivoreSparkline());
        }
    }

    public sealed class DashboardPresenterTests
    {
        [Test]
        public void Build_ZeroPopulation_SafeRatios()
        {
            var model = DashboardPresenter.Build(new DashboardInputs
            {
                Population = new StubPopulation(),
                Clock = new StubClock()
            });
            Assert.AreEqual(0, model.TotalAlive);
            Assert.AreEqual(RatioFormatter.NotAvailable, model.PredatorPreyRatio);
            Assert.AreEqual(DashboardPresenter.Unavailable, model.PlantAbundance);
        }

        [Test]
        public void MeanTrait_AveragesLivingGenomes()
        {
            var views = new IAnalyticsCreatureView[]
            {
                new StubAnalyticsView
                {
                    GenomeTraits = new StubGenomeTraits((CanonicalTraitNames.BaseMovementSpeed, 2f))
                },
                new StubAnalyticsView
                {
                    GenomeTraits = new StubGenomeTraits((CanonicalTraitNames.BaseMovementSpeed, 4f))
                }
            };
            Assert.AreEqual(3f, DashboardPresenter.MeanTrait(views, CanonicalTraitNames.BaseMovementSpeed));
            Assert.IsNull(DashboardPresenter.MeanTrait(views, "missing"));
        }
    }

    public sealed class CreatureSelectionStateTests
    {
        [Test]
        public void Clear_EmitsChangedAndDropsSelection()
        {
            var host = new GameObject("SelectionHost");
            try
            {
                var state = new CreatureSelectionState();
                var fires = 0;
                state.Changed += () => fires++;
                state.Select(host, new SelectedCreatureSnapshot { HasSelection = true });
                Assert.AreEqual(1, fires);
                Assert.IsTrue(state.HasSelection);
                state.Clear();
                Assert.AreEqual(2, fires);
                Assert.IsFalse(state.HasSelection);
                Assert.IsFalse(CreatureInspectorPresenter.Build(state.Snapshot).HasSelection);
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }
    }

    public sealed class AiDebugVisualizationSettingsTests
    {
        [Test]
        public void ShouldDraw_DisabledByDefaultAndSelectedOnly()
        {
            AiDebugVisualizationSettings.GlobalEnabled = false;
            AiDebugVisualizationSettings.SelectedCreatureOnly = true;
            Assert.IsFalse(AiDebugVisualizationSettings.ShouldDraw(true));
            AiDebugVisualizationSettings.GlobalEnabled = true;
            Assert.IsTrue(AiDebugVisualizationSettings.ShouldDraw(true));
            Assert.IsFalse(AiDebugVisualizationSettings.ShouldDraw(false));
            AiDebugVisualizationSettings.SelectedCreatureOnly = false;
            Assert.IsTrue(AiDebugVisualizationSettings.ShouldDraw(false));
            AiDebugVisualizationSettings.GlobalEnabled = false;
            AiDebugVisualizationSettings.SelectedCreatureOnly = true;
        }
    }

    sealed class StubActivity : IReadOnlyCreatureActivity
    {
        public string CurrentActivity { get; set; }
    }

    sealed class StubAiDebug : IReadOnlyCreatureAiDebug
    {
        public string ControlMode { get; set; }
        public string BehaviorName { get; set; }
        public float Forward { get; set; }
        public float Turn { get; set; }
        public float SprintOrEffort { get; set; }
        public string InteractionRequest { get; set; }
        public bool HasScriptedMotive { get; set; }
        public string ScriptedMotive { get; set; }
        public float SensoryRange { get; set; }
        public float InteractionRange { get; set; }
        public float HeadingX { get; set; }
        public float HeadingZ { get; set; }
        public SensedTargetDebug NearestFood { get; set; }
        public SensedTargetDebug NearestWater { get; set; }
        public SensedTargetDebug NearestHerbivore { get; set; }
        public SensedTargetDebug NearestPredator { get; set; }
        public bool HasHeuristicTarget { get; set; }
        public SensedTargetDebug HeuristicTarget { get; set; }
    }

    sealed class StubEvent : IReadOnlyEnvironmentalEvent
    {
        public int EventId { get; set; }
        public EnvironmentalEventKind Kind { get; set; }
        public float StartedAtSimulationTime { get; set; }
        public float EndsAtSimulationTime { get; set; }
        public bool IsActive { get; set; }
    }

    sealed class StubClock : ISimulationClockControl
    {
        public float SimulationTimeSeconds { get; set; }
        public float DeltaTimeSeconds { get; set; }
        public float TimeScale { get; set; } = 1f;
        public bool IsPaused { get; set; }

        public void SetPaused(bool paused) => IsPaused = paused;

        public void SetTimeScale(float scale) => TimeScale = scale;
    }
}
