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
   - `CreatureObservationSchemaTests` / `CompositeObservationSourceTests` (size 28, order, sensor zeros)
   - `CreatureActionSchemaTests` (clamp / idle PPO fallback)
   - `TrainingRewardCalculatorTests` (death terminate, relief, critical-need)
   - `PopulationTrackerTests`
   - `AnalyticsCollectorTests` (snapshot math, lifetime records, generation aggregates, policy classification, empty-population safety)
5. Open `Assets/EvoLife/Scenes/Bootstrap.unity`, add missing component references (`SimulationClock`, `SimulationRunner`, `PopulationTracker`, `CreatureSpawner`, analytics) via the Inspector — the checked-in scene is a minimal placeholder.
6. Confirm ScriptableObject create menus appear: `EvoLife/Creatures/Species Vitals`, `EvoLife/Simulation/Config`.
7. Commit any Unity-generated `.meta` files so GUIDs are shared.
8. Optional: with ML-Agents Python installed, run `./Training/scripts/train_herbivore.sh`, set a creature to `LearnedPpo`, press Play, and confirm the trainer connects (`EvoLifeHerbivore`).
9. Optional: start Backend and toggle `StatsExportLoop.uploadToBackend` once a collector is wired in the scene.
