# Desktop UI, inspector, and AI debug

EvoLife’s desktop tools observe the simulation. They do not own biology, genetics, AI policy, environmental effects, or experiment lifecycle.

Related: [ARCHITECTURE.md](ARCHITECTURE.md), [AGENT_BOUNDARIES.md](AGENT_BOUNDARIES.md), [EXPERIMENTS.md](EXPERIMENTS.md), [ENVIRONMENT_EVENTS.md](ENVIRONMENT_EVENTS.md), [ANALYTICS.md](ANALYTICS.md), [AI_ML_AGENTS.md](AI_ML_AGENTS.md), [SCRIPTED_BASELINE.md](SCRIPTED_BASELINE.md).

## What this module provides

| Area | Types | Role |
|------|-------|------|
| Camera | `DesktopCameraController` | Free-fly observer camera, optional orbit around a selected transform |
| Selection | `CreatureSelectionController`, `CreatureSelectable`, `CreatureSelectionState` | Click/raycast select, deselect, destroyed-host clear |
| Inspector | `CreatureInspectorPresenter` | Identity, biology, genetics, AI decision display |
| AI overlay | `CreatureAiDebugVisualizer`, `AiDebugVisualizationSettings` | Selected-creature sensory/intent gizmos |
| Sim controls | `SimulationControlPresenter` | Pause/resume/speed through `SimulationClock` |
| Events | `EventPanelPresenter` | Trigger configured kinds on `EnvironmentalEventManager` |
| Dashboard / charts | `DashboardPresenter`, `ChartRingBuffer`, `DashboardChartSampler` | Read-only population, environment, experiment, sampled sparklines |
| Overlay | `DesktopDebugUi` | Runtime Canvas + wiring |

`SimulationHud` remains the tiny leftover status line. Use `DesktopDebugUi` for demos.

## Camera controls

No XR/VR. Old Input Manager (`activeInputHandler: 0`).

| Input | Action |
|-------|--------|
| **W A S D** | Move (planar relative to yaw) |
| **E** / **Space** | Up |
| **Q** / **Left Ctrl** | Down |
| **Right mouse drag** | Look (free) or orbit yaw/pitch (orbit mode) |
| **Mouse wheel** | FOV zoom (free) or orbit distance (orbit) |
| **Shift** | Faster move |
| **-** / **+** | Decrease / increase move speed |
| **F** | Focus / orbit the selected creature |
| **C** | Return to free camera |
| **Focus** / **Free cam** buttons | Same as F / C |

The camera follows a `Transform` only. It does not read vitals or genomes.

## Creature selection

- Left click raycasts from the desktop camera.
- Hits resolve `ICreatureIdentity` on the collider or parents.
- Click empty space or **Esc** deselects.
- Dead creatures stay selected so the inspector can show death cause.
- Destroyed hosts clear selection.

Existing creature colliders are used when present. If a visual prefab has no collider, add the optional `CreatureSelectable` component (can add a fallback trigger sphere). Do not change AI sensing colliders for selection.

## Inspector fields

Built by `CreatureInspectorPresenter` from Common contracts on the selected host.

**Identity:** creature id, species, role, generation, parent A/B, living offspring count (from live lineage views), policy kind.

**Biology:** alive/dead, death cause, age / max age, health / max health, hunger / max hunger, thirst / max thirst, energy / max energy, current activity (`IReadOnlyCreatureActivity`).

**Genetics:** canonical schema v1 names in order — `base_movement_speed`, `sprint_speed`, `vision_range`, `maximum_energy`, `metabolism_rate`, `body_size`, `aggression`, `reproduction_threshold`, `maximum_age`.

**AI / decision:** control mode, behavior name (`EvoLifeHerbivore` / `EvoLifePredator`), latest `forward` / `turn` / `sprint_or_effort`, discrete interaction (`none` / `eat` / `drink` / `attack` / `rest` / `reproduce_request`), episode return when `IEpisodeMetrics.HasEpisodeReturn`, scripted baseline motive when baseline-controlled.

Missing optional contracts (no `IReadOnlyCreatureAiDebug`, no activity, no episode return) display `unavailable`. The inspector does not reach into private fields.

## AI debug visualization

Disabled by default. **F3** or the **AI debug** button toggles `AiDebugVisualizationSettings.GlobalEnabled`. Selected-creature only by default.

When enabled, `CreatureAiDebugVisualizer` draws LineRenderers / `Debug.DrawLine` for:

- sensory range
- interaction range
- nearest food / water / herbivore / predator (from the last observation snapshot, not a second sensor)
- movement heading
- current forward/turn intent
- scripted heuristic target when present

`CreatureBrain` implements `IReadOnlyCreatureAiDebug` by copying last executor actions and last sensed observation results. Overlay code does not call `OverlapSphere` itself.

## Simulation controls

Buttons and `SimulationClock` (`ISimulationClockControl`):

- Pause / Resume
- **1x / 2x / 5x / 10x**

Do not set `Time.timeScale`. The overlay also shows simulation time, current speed, paused/running, experiment name/scenario, and orchestrator phase when present.

**Restart:** `ExperimentOrchestrator` rejects a second `BeginAsync` in the same scene. The UI does not invent a mid-scene experiment restart. **Reload scene** reloads the active Unity scene so a new orchestrated run can start. That is a scene reset, not a hidden second founder spawn.

## Event control panel

Buttons request `IEnvironmentalEventCommands.Trigger` for:

- drought
- wildfire
- heat_wave
- food_boom
- disease_pressure
- predator_introduction
- predator_removal

Effects stay in `EnvironmentalEventManager`. The panel lists active event names and remaining duration from `IReadOnlyEnvironmentalEvent`.

## Live dashboard

Read-only from `IPopulationSnapshot`, `AnalyticsSnapshotBuilder` (pure, not `PopulationStatisticCollector.Capture`), live creature views, and environment contracts:

**Population:** herbivores, predators, total alive, births, deaths, predator/prey ratio (`n/a` when herbivores are 0), max living generation, scripted vs PPO alive.

**Environment:** plant count, plant food remaining/capacity, abundance, water source count, active events, sim time, day/night if `IReadOnlyDayNightState` is present.

**Experiment:** name, scenario, seed, herbivore/predator policy kinds, PPO model id when set.

**Evolution:** generation summary and mean of each canonical trait across living genomes. There is no fitness score and no claim that PPO is better than the scripted baseline.

## Live charts

`ChartRingBuffer` / `DashboardChartSampler` (default capacity 96, sample interval 0.25–0.5s):

- herbivore population
- predator population
- births / deaths (cumulative counters)
- plant abundance
- optional selected trait mean (`base_movement_speed` by default)

Presentation-only. Not a second analytics backend. Text sparklines update on sample, not every frame.

## Policy / AI comparison visibility

Dashboard and inspector show `ScriptedBaseline` vs `LearnedPpo` and the experiment `model_id` when the policy is PPO. They do not rank policies.

## Dependencies / wiring

`EvoLife.UI` references **Common, Simulation, Analytics** only.

Minimal Common contracts added for this UI:

- `ISimulationClockControl`
- `IEnvironmentalEventCommands`
- `IReadOnlyCreatureActivity`
- `IReadOnlyCreatureAiDebug` / `SensedTargetDebug`
- `ILiveCreatureCatalog`

Implementors: `SimulationClock`, `EnvironmentalEventManager`, `CreatureVitals`, `CreatureBrain`, `CreatureLifecycleHub`.

### Add to any simulation scene

1. Main Camera: `DesktopCameraController`, `CreatureSelectionController` (or let `DesktopDebugUi` add them).
2. Empty GameObject `DesktopDebugUi`: `DesktopDebugUi` + `CreatureAiDebugVisualizer`.
3. Assign (or leave empty for `FindFirstObjectByType` / interface search): `SimulationClock`, `PopulationTracker`, `CreatureLifecycleHub`, `ExperimentOrchestrator`, `SimulationConfig`, census / day-night / event manager behaviours.
4. Creature prefabs: existing colliders, or optional `CreatureSelectable`.
5. Do **not** require a baked Canvas prefab; `DesktopDebugUi` builds one at runtime.

Dedicated test/debug scene: `Assets/EvoLife/Scenes/UiDebug.unity`.

Keyboard: **F1** hide/show overlay, **F3** AI debug.

## Final presentation-scene wiring (`EvoLifeDemo`)

`Assets/EvoLife/Scenes/EvoLifeDemo.unity` now exists (Presentation PR). Do **not** rewrite terrain, biomes, creature meshes, or ShaderGraph.

Additive attach points already in that scene:

1. `Main Camera` / `Agent10_CameraRigHook` — add `DesktopCameraController` + `CreatureSelectionController` on the existing Main Camera (or parent the rig under the hook). **Do not enable a second camera.**
2. `Agent10_UiCanvasHook` — add a `DesktopDebugUi` object here (`DesktopDebugUi` + `CreatureAiDebugVisualizer`). The overlay builds a Canvas at runtime; parenting under the hook keeps hierarchy tidy.
3. Assign clock / population / lifecycle / event manager if Find is too slow.
4. Creature visual prefabs already have colliders; use them. Add `CreatureSelectable` only if a visual prefab has none. Do not change meshes/materials.
5. Keep time-scale buttons off during recorded evaluation runs (demo scrubbing is not experiment output). See [EXPERIMENTS.md](EXPERIMENTS.md) and [PRESENTATION.md](PRESENTATION.md).

## Tests

EditMode: `DesktopUiPresenterTests` (inspector empty/living/dead, trait order, policy labels, zero ratios, event formatting, speed presets, ring-buffer capacity, selection clear, missing AI debug).

PlayMode: `DesktopUiPlayModeTests` (camera focus/free, selection clear on destroy). Unity Editor is required to execute them.
