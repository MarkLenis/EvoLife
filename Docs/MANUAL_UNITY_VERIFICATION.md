# Manual Unity Verification Checklist

Unity Editor is not available in typical cloud agent environments. After pulling architecture changes, verify locally:

1. Open the project with **Unity 2022.3 LTS** matching `ProjectSettings/ProjectVersion.txt` (or allow Unity to upgrade the patch and commit the new version if the team agrees).
2. Package Manager resolves `com.unity.ml-agents` **2.0.1** and Test Framework without errors.
3. Console has **no compile errors** across `EvoLife.*` assemblies (check for asmdef reference issues).
4. Open **Test Runner → EditMode** and run all `EvoLife.Tests.EditMode` tests, including:
   - `CreatureBiologyTests` (modifier isolation)
   - `GeneticOperatorsTests` (canonical schema, founder/crossover/mutation, decode, normalization)
   - `PhenotypeCapabilityBridgeTests`
   - `VitalObservationSourceTests` (non-100 hunger/thirst capacities)
   - `CreatureObservationSchemaTests` / `CompositeObservationSourceTests` (size 31, order, independent herbivore/predator channels, sensor zeros)
   - `CreatureProximitySelectionTests` (nearest herbivore does not hide predator)
   - `CreatureActionSchemaTests` (3 continuous + interaction branch, clamp, local forward/turn, invalid no-ops)
   - `TrainingRewardCalculatorTests` (death terminate, relief, critical-need)
   - `BaselineMotiveEvaluatorTests` / `ScriptedBaselinePolicyTests` (scripted heuristic priorities, canonical action path, no vital mutation)
   - `ReproductionTests` / `EcosystemLifecycleTests` (mating eligibility, lineage, mutation bounds, training vs persistent respawn, spawn-failure does not charge energy/health or start cooldown, success commits costs/cooldown once)
  - `PlantResourceTests` / `DayNightCycleTests` / `EnvironmentalEventTests` (depletion, regen, drought/food boom, event restore, no double death, lifecycle spawn/remove)
   - `PresentationTests` (creature prefab component contract, herbivore/predator distinction, visual-only body_size scale, event adapter does not mutate resources, missing visual refs are safe)
   - `ExperimentConfigurationTests` / `ExperimentLifecycleTests` (JSON round-trip, seeds, scenarios, stop conditions, metadata, extinct rates, policy selection, validation, initialization pause until `BeginRunning`, failed analytics never Running, FinishAsync pauses, second `BeginAsync` rejected, environment applied/placed once, founders spawned once)
  - `AnalyticsExportControllerTests` (failed upload retains records, success dequeues, bounded overflow)
  - `PopulationTrackerTests`
  - `AnalyticsCollectorTests` (snapshot math, lifetime records, generation aggregates, policy classification, empty-population safety)
  - `DesktopUiPresenterTests` (inspector empty/living/dead, trait order, policy labels, zero ratios, event formatting, speed presets, ring-buffer capacity, selection clear, missing AI debug)
5. Open `Assets/EvoLife/Scenes/Bootstrap.unity`, add missing component references (`SimulationClock`, `SimulationRunner`, `PopulationTracker`, `CreatureSpawner`, analytics) via the Inspector — the checked-in scene is a minimal placeholder.
6. Open `Assets/EvoLife/Scenes/EvoLifeDemo.unity` and confirm:
   - grassland / forest / wetland / rocky ground colors are distinct
   - plants look like food and shrink/tint when depleted (without destroying the node)
   - water discs sit in the wetland and do not block walking
   - herbivores (green, round) vs predators (rust, elongated) are obvious; snouts face +Z
   - directional light moves with sim-time day/night
   - triggering a drought/wildfire from the event manager (or Inspector) only changes presentation cues, not a second resource system
   - UI attaches to `Agent10_CameraRigHook` / `Agent10_UiCanvasHook` without adding a second enabled camera (see [UI_DEBUG.md](UI_DEBUG.md))
7. For desktop UI: open `Assets/EvoLife/Scenes/UiDebug.unity` or add `DesktopDebugUi` to a simulation scene (see [UI_DEBUG.md](UI_DEBUG.md)). Confirm camera WASD/RMB, click-select, inspector, pause/speed, event buttons, and F3 AI debug overlay. Do not treat demo time-scale as experiment output.
8. Confirm ScriptableObject create menus appear: `EvoLife/Creatures/Species Vitals`, `EvoLife/Simulation/Config`, `EvoLife/Simulation/Experiment Configuration`, `EvoLife/Simulation/Reproduction Config`, `EvoLife/Environment/Config`, `EvoLife/Environment/Event Config`, `EvoLife/AI/Scripted Baseline Profile`.
9. Commit any Unity-generated `.meta` files so GUIDs are shared.
10. Optional: with ML-Agents Python installed, run `./Training/scripts/train_herbivore.sh`, set a creature to `LearnedPpo`, press Play, and confirm the trainer connects (`EvoLifeHerbivore`).
11. Optional: start Backend and toggle `StatsExportLoop.uploadToBackend` once a collector is wired in the scene.
