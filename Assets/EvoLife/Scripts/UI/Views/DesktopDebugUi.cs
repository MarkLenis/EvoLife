using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using EvoLife.Analytics;
using EvoLife.Common;
using EvoLife.Simulation;

namespace EvoLife.UI
{
    /// <summary>
    /// Desktop debug overlay. Observes Simulation/Analytics/Common contracts and
    /// builds a runtime Canvas. Does not own biology, genetics, or experiment logic.
    /// </summary>
    public sealed class DesktopDebugUi : MonoBehaviour
    {
        const float SampleIntervalSeconds = 0.25f;

        [SerializeField] SimulationClock clock;
        [SerializeField] PopulationStatisticCollector collector;
        [SerializeField] ExperimentOrchestrator orchestrator;
        [SerializeField] SimulationConfig simulationConfig;
        [SerializeField] CreatureLifecycleHub lifecycleHub;
        [SerializeField] PopulationTracker populationTracker;
        [SerializeField] MonoBehaviour environmentCensusBehaviour;
        [SerializeField] MonoBehaviour dayNightBehaviour;
        [SerializeField] MonoBehaviour eventCommandsBehaviour;
        [SerializeField] DesktopCameraController cameraController;
        [SerializeField] CreatureSelectionController selectionController;
        [SerializeField] CreatureAiDebugVisualizer aiVisualizer;
        [SerializeField] bool buildRuntimeCanvas = true;
        [SerializeField] bool visible = true;
        [SerializeField] KeyCode toggleUiKey = KeyCode.F1;
        [SerializeField] KeyCode toggleAiDebugKey = KeyCode.F3;

        readonly List<IAnalyticsCreatureView> liveViews = new List<IAnalyticsCreatureView>(64);
        readonly DashboardChartSampler charts = new DashboardChartSampler();
        readonly ComposedEnvironmentState environment = new ComposedEnvironmentState();

        IReadOnlyResourceCensus census;
        IReadOnlyDayNightState dayNight;
        IEnvironmentalEventCommands eventCommands;
        DesktopDebugUiWidgets widgets;
        float nextSampleAt = -1f;
        string lastInspector;
        string lastDashboard;
        string lastCharts;

        void Awake()
        {
            ResolveDependencies();
            EnsureCameraAndSelection();
            if (buildRuntimeCanvas)
            {
                widgets = DesktopDebugUiBuilder.Build(transform, OnSpeedClicked, OnEventClicked, OnToggleAiDebug);
                BindControlButtons();
            }

            EnsureEventSystem();
        }

        void OnEnable()
        {
            if (selectionController != null)
            {
                selectionController.State.Changed += OnSelectionChanged;
            }
        }

        void OnDisable()
        {
            if (selectionController != null)
            {
                selectionController.State.Changed -= OnSelectionChanged;
            }
        }

        void Update()
        {
            if (Input.GetKeyDown(toggleUiKey))
            {
                visible = !visible;
                if (widgets != null && widgets.Root != null)
                {
                    widgets.Root.SetActive(visible);
                }
            }

            if (Input.GetKeyDown(toggleAiDebugKey))
            {
                AiDebugVisualizationSettings.ToggleGlobal();
                RefreshAiVisualizer();
            }

            if (selectionController != null && cameraController != null)
            {
                cameraController.SetFocusTarget(selectionController.SelectedTransform);
            }

            if (!visible)
            {
                return;
            }

            var time = clock != null ? clock.SimulationTimeSeconds : Time.unscaledTime;
            if (time < nextSampleAt)
            {
                RefreshInspectorOnly();
                return;
            }

            nextSampleAt = time + SampleIntervalSeconds;
            RefreshAll();
        }

        void ResolveDependencies()
        {
            if (clock == null)
            {
                clock = FindObjectOfType<SimulationClock>();
            }

            if (collector == null)
            {
                collector = FindObjectOfType<PopulationStatisticCollector>();
            }

            if (orchestrator == null)
            {
                orchestrator = FindObjectOfType<ExperimentOrchestrator>();
            }

            if (lifecycleHub == null)
            {
                lifecycleHub = FindObjectOfType<CreatureLifecycleHub>();
            }

            if (populationTracker == null)
            {
                populationTracker = FindObjectOfType<PopulationTracker>();
            }

            census = environmentCensusBehaviour as IReadOnlyResourceCensus
                ?? FindInterface<IReadOnlyResourceCensus>();
            dayNight = dayNightBehaviour as IReadOnlyDayNightState
                ?? FindInterface<IReadOnlyDayNightState>();
            eventCommands = eventCommandsBehaviour as IEnvironmentalEventCommands
                ?? FindInterface<IEnvironmentalEventCommands>();
        }

        void EnsureCameraAndSelection()
        {
            var cam = Camera.main;
            if (cam != null)
            {
                if (cameraController == null)
                {
                    cameraController = cam.GetComponent<DesktopCameraController>();
                    if (cameraController == null)
                    {
                        cameraController = cam.gameObject.AddComponent<DesktopCameraController>();
                    }
                }

                if (selectionController == null)
                {
                    selectionController = cam.GetComponent<CreatureSelectionController>();
                    if (selectionController == null)
                    {
                        selectionController = cam.gameObject.AddComponent<CreatureSelectionController>();
                    }
                }
            }

            if (selectionController != null)
            {
                selectionController.BindCatalog(lifecycleHub);
                selectionController.SetExperimentModelId(ResolveModelId());
            }

            if (aiVisualizer == null)
            {
                aiVisualizer = GetComponent<CreatureAiDebugVisualizer>();
                if (aiVisualizer == null)
                {
                    aiVisualizer = gameObject.AddComponent<CreatureAiDebugVisualizer>();
                }
            }
        }

        void BindControlButtons()
        {
            if (widgets == null)
            {
                return;
            }

            widgets.Pause.onClick.AddListener(() => SimulationControlPresenter.Pause(clock));
            widgets.Resume.onClick.AddListener(() => SimulationControlPresenter.Resume(clock));
            widgets.ReloadScene.onClick.AddListener(ReloadActiveScene);
            widgets.Focus.onClick.AddListener(() =>
            {
                if (cameraController != null && selectionController != null)
                {
                    cameraController.FocusSelected(selectionController.SelectedTransform);
                }
            });
            widgets.FreeCamera.onClick.AddListener(() => cameraController?.ReturnToFree());
        }

        void OnSpeedClicked(float scale) => SimulationControlPresenter.SetSpeed(clock, scale);

        void OnEventClicked(EnvironmentalEventKind kind) =>
            EventPanelPresenter.RequestTrigger(eventCommands, kind);

        void OnToggleAiDebug()
        {
            AiDebugVisualizationSettings.ToggleGlobal();
            RefreshAiVisualizer();
        }

        void OnSelectionChanged()
        {
            RefreshInspectorOnly();
            RefreshAiVisualizer();
        }

        void RefreshAll()
        {
            liveViews.Clear();
            lifecycleHub?.CopyLiveViews(liveViews);
            environment.DayNight = dayNight;
            environment.Resources = census;
            environment.ActiveEvents = eventCommands != null
                ? eventCommands.ActiveEvents
                : System.Array.Empty<IReadOnlyEnvironmentalEvent>();

            var censusPolicy = AnalyticsSnapshotBuilder.Census(liveViews);
            var experimentId = collector != null ? collector.ExperimentIdValue : "local-dev";
            var stats = AnalyticsSnapshotBuilder.Build(
                experimentId,
                clock != null ? clock.SimulationTimeSeconds : 0f,
                populationTracker,
                populationTracker != null ? populationTracker.TotalAlive : 0,
                censusPolicy);
            var config = orchestrator != null ? orchestrator.Configuration : null;
            var dashboard = DashboardPresenter.Build(new DashboardInputs
            {
                Population = populationTracker,
                Stats = stats,
                Environment = environment,
                Clock = clock,
                ExperimentName = config != null ? config.ExperimentName : simulationConfig != null ? simulationConfig.ExperimentName : stats?.experimentId,
                ScenarioId = config != null ? config.ScenarioId : simulationConfig != null ? simulationConfig.ScenarioId : "",
                RandomSeed = config != null ? config.RandomSeed : (int?)(simulationConfig != null ? simulationConfig.RandomSeed : (int?)null),
                HerbivorePolicy = config != null ? config.HerbivorePolicy : simulationConfig != null ? simulationConfig.HerbivorePolicy : (AgentPolicyKind?)null,
                PredatorPolicy = config != null ? config.PredatorPolicy : simulationConfig != null ? simulationConfig.PredatorPolicy : (AgentPolicyKind?)null,
                ModelId = ResolveModelId(),
                LiveViews = liveViews
            });

            var simTime = clock != null ? clock.SimulationTimeSeconds : 0f;
            var traitMean = DashboardPresenter.MeanTrait(liveViews, CanonicalTraitNames.BaseMovementSpeed);
            var sampled = charts.TrySample(simTime, dashboard, SampleIntervalSeconds, traitMean);
            var control = SimulationControlPresenter.Build(
                clock,
                dashboard.ExperimentName,
                dashboard.Scenario,
                orchestrator != null && orchestrator.State != null ? orchestrator.State.Phase.ToString() : "n/a");

            if (widgets != null)
            {
                var dashboardText = dashboard.SummaryText
                    + "\nSIMULATION\n  Status: " + control.StatusLabel
                    + "\n  Speed: " + SimulationControlPresenter.FormatSpeed(control.TimeScale)
                    + "\n  " + control.RestartNote;
                if (dashboardText != lastDashboard)
                {
                    widgets.Dashboard.text = dashboardText;
                    lastDashboard = dashboardText;
                }

                if (sampled)
                {
                    var chartText =
                        "CHARTS (sampled)\n"
                        + "  Herbivores: " + charts.HerbivoreSparkline() + "\n"
                        + "  Predators: " + charts.PredatorSparkline() + "\n"
                        + "  Births: " + charts.BirthsSparkline() + "\n"
                        + "  Deaths: " + charts.DeathsSparkline() + "\n"
                        + "  Plant abundance: " + charts.AbundanceSparkline() + "\n"
                        + "  " + CanonicalTraitNames.BaseMovementSpeed + ": " + charts.TraitSparkline();
                    if (chartText != lastCharts)
                    {
                        widgets.Charts.text = chartText;
                        lastCharts = chartText;
                    }
                }

                widgets.SimStatus.text =
                    "t=" + SimulationControlPresenter.FormatTime(control.SimulationTimeSeconds)
                    + "  " + SimulationControlPresenter.FormatSpeed(control.TimeScale)
                    + "  " + control.StatusLabel
                    + "  AI debug: " + (AiDebugVisualizationSettings.GlobalEnabled ? "ON" : "off");
            }

            RefreshInspectorOnly();
            RefreshAiVisualizer();
        }

        void RefreshInspectorOnly()
        {
            if (widgets == null)
            {
                return;
            }

            SelectedCreatureSnapshot snapshot;
            if (selectionController != null && selectionController.SelectedTransform != null)
            {
                snapshot = selectionController.RefreshSnapshot();
            }
            else
            {
                snapshot = selectionController != null
                    ? selectionController.State.Snapshot
                    : null;
            }

            var model = CreatureInspectorPresenter.Build(snapshot);
            if (model.SummaryText != lastInspector)
            {
                widgets.Inspector.text = model.SummaryText;
                lastInspector = model.SummaryText;
            }
        }

        void RefreshAiVisualizer()
        {
            if (aiVisualizer == null)
            {
                return;
            }

            if (selectionController == null || selectionController.SelectedTransform == null)
            {
                aiVisualizer.SetSelected(null, null);
                return;
            }

            var debug = selectionController.SelectedTransform.GetComponent<IReadOnlyCreatureAiDebug>();
            aiVisualizer.SetSelected(selectionController.SelectedTransform, debug);
        }

        string ResolveModelId()
        {
            if (orchestrator != null && orchestrator.Configuration != null)
            {
                return orchestrator.Configuration.ModelId;
            }

            return simulationConfig != null ? simulationConfig.TrainingModelId : "";
        }

        static void ReloadActiveScene()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            UnityEngine.SceneManagement.SceneManager.LoadScene(scene.name);
        }

        static T FindInterface<T>() where T : class
        {
            var behaviours = FindObjectsOfType<MonoBehaviour>();
            for (var i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is T match)
                {
                    return match;
                }
            }

            return null;
        }

        static void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null)
            {
                return;
            }

            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }

        sealed class ComposedEnvironmentState : IReadOnlyEnvironmentState
        {
            public IReadOnlyDayNightState DayNight { get; set; }
            public IReadOnlyResourceCensus Resources { get; set; }
            public IReadOnlyList<IReadOnlyEnvironmentalEvent> ActiveEvents { get; set; } =
                System.Array.Empty<IReadOnlyEnvironmentalEvent>();
            public float TemperatureNormalized => 0f;
        }
    }
}
