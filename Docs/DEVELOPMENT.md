# EvoLife Development Guide

## Project setup

### Prerequisites

- Unity **2022.3 LTS** (see `ProjectSettings/ProjectVersion.txt`)
- Git
- Python **3.10+** (for Backend)
- Optional: ML-Agents Python package matching `com.unity.ml-agents` 2.x for training

### Clone and open Unity

```bash
git clone <repo-url> EvoLife
# Open the repository root in Unity Hub → Editor 2022.3.x
```

On first open:

1. Let Package Manager resolve packages from `Packages/manifest.json`.
2. Confirm `com.unity.ml-agents`, Test Framework, and Newtonsoft JSON imported.
3. Create a Bootstrap scene under `Assets/EvoLife/Scenes/` if one is not yet assigned (wire `SimulationClock`, `SimulationRunner`, `PopulationTracker`, `CreatureSpawner`, `ReproductionSystem`, `EcosystemManager`, `ResourceManager`, `DayNightManager`, `EnvironmentalEventManager`, analytics components).
4. Unity will generate `.meta` files — **commit them** with assets so GUIDs stay stable across machines.

### Backend setup

```bash
cd Backend
python -m venv .venv
source .venv/bin/activate
pip install -r requirements.txt
uvicorn app.main:app --reload --host 127.0.0.1 --port 8000
```

Point Unity `BackendClient.baseUrl` at `http://127.0.0.1:8000`.

### Training setup (optional)

```bash
pip install mlagents  # version compatible with ML-Agents 2.0.x
chmod +x Training/scripts/*.sh
# Start Unity with the Agent scene, then:
./Training/scripts/train_herbivore.sh
```

Do not expect a trained production policy; the Agent wiring and YAML are development starters. See [AI_ML_AGENTS.md](AI_ML_AGENTS.md), [TRAINING_CURRICULUM.md](TRAINING_CURRICULUM.md), [EXPERIMENTS.md](EXPERIMENTS.md), and [Training/README.md](../Training/README.md).

---

## Folder conventions

```
Assets/EvoLife/
  Scripts/<Module>/     C# + asmdef for that domain
  Prefabs/<Area>/       Instantiable content
  ScriptableObjects/    Data assets (species, sim config, …)
  Materials/            Shared materials (when art begins)
  Scenes/               Playable / training scenes
  Tests/EditMode|PlayMode/
Backend/app/            FastAPI application code
Backend/tests/          Pytest
Training/configs/       ML-Agents YAML
Training/scripts/       Train/eval helpers
Docs/                   Architecture, analytics, and process
```

Rules:

- Runtime scripts live under `Assets/EvoLife/Scripts/<Module>/`, not loose at `Assets/`.
- Do not place domain logic in `UI` or scene-only MonoBehaviours without an owning module.
- Keep temporary experiments out of `main`; use feature branches.

---

## Naming conventions

| Kind | Convention | Example |
|------|------------|---------|
| Namespaces | `EvoLife.<Module>` | `EvoLife.Genetics` |
| Assemblies | `EvoLife.<Module>` | `EvoLife.AI` |
| Interfaces | `I` + capability | `IObservationSource` |
| ScriptableObjects | `*Definition` / `*Config` | `SpeciesVitalsDefinition` |
| MonoBehaviours | Noun + role | `CreatureVitals`, `PlantResource` |
| Tests | `<Subject>Tests` | `GeneticOperatorsTests` |
| Scenes | PascalCase | `Bootstrap`, `TrainingArena` |
| Prefabs | PascalCase | `HerbivoreAgent` |

File name matches primary type name (`Genome.cs` → `Genome`).

---

## How to run tests

### Offline Python (genetics + experiments)

```bash
pip install -e ".[dev]"
pytest -q
```

### Backend (automated in CI-friendly environments)

```bash
cd Backend
source .venv/bin/activate  # if needed
pytest -q
```

### Unity EditMode

1. Open Window → General → Test Runner.
2. Select **EditMode**.
3. Run EditMode `ExperimentConfigurationTests`, `GeneticOperatorsTests`, `CreatureBiologyTests`, `VitalObservationSourceTests`, `PopulationTrackerTests`, `AnalyticsCollectorTests`, `AnalyticsExportControllerTests`, `CreatureObservationSchemaTests`, `CompositeObservationSourceTests`, `CreatureProximitySelectionTests`, `CreatureActionSchemaTests`, `TrainingRewardCalculatorTests`, `BaselineMotiveEvaluatorTests`, `ScriptedBaselinePolicyTests`, `ReproductionTests`, `PlantResourceTests`, `EnvironmentalEventTests`.

### Unity PlayMode

PlayMode assembly includes `EvoLifeCreatureAgentPlayModeTests` (schema-sized observations; Agent sizes without a trained model).

### What this environment can verify

Cloud/agent Linux VMs typically **cannot** run the Unity Editor compiler. Backend `pytest` can run after installing requirements. Treat Unity compilation and Test Runner results as **manual verification** unless a Unity batchmode runner is installed.

---

## How to add a new creature species

1. **Data (Creatures):** Create `SpeciesVitalsDefinition` asset under `ScriptableObjects/Species/`.
2. **Prefab:** Duplicate a creature prefab under `Prefabs/Creatures/` with:
   - `CreatureIdentity`
   - `CreatureVitals`
   - `CreatureGenome`
   - `CreatureCapabilityMotor`
   - `CreatureBrain` + `PlanarMoveActionExecutor` + `EvoLifeCreatureAgent` (for PPO)
3. **Role:** Set herbivore vs predator on identity / spawn call (`CreatureRole`).
4. **Simulation:** Extend spawn setup / `SimulationConfig` (or a species table SO) to include initial counts and prefab references.
5. **AI:** If observation needs change, extend `IObservationSource` implementations carefully and keep sizes documented for ML-Agents.
6. **Do not** fork vitals logic into AI or Simulation.

---

## How to add an environmental resource

1. Implement `IResourceNode` in **Environment** (see `PlantResource`, `WaterSource`).
2. Add regen/tick behavior via `ISimulationTickable` if needed; register with `SimulationRunner` or let `ResourceManager` tick owned nodes.
3. Register instances in `ResourceRegistry` on enable/spawn.
4. Prefab under `Prefabs/Environment/` (optional; `ResourceManager` can spawn empty nodes).
5. If agents should sense it, add observations in **AI** (not in Environment). Do not grow CreatureObservationSchema v2 without a documented bump.
6. Consumption must go through `TryConsume` + Creatures APIs (`ConsumeFood` / `Drink`).
7. Ecological events that change availability must call Environment APIs (`IEnvironmentEffectHost`), not rewrite plant fields from Simulation or AI.

See [ENVIRONMENT.md](ENVIRONMENT.md) and [ENVIRONMENT_EVENTS.md](ENVIRONMENT_EVENTS.md).

---

## How to add a statistic

1. Extend `SimulationStatsSnapshot` in **Analytics** (fields must remain `JsonUtility`-serializable for Unity).
2. Update the collector (`PopulationStatisticCollector` or a new focused collector).
3. Mirror the field in Backend schemas (`Backend/app/schemas/`) — extend v1 optionally, or the extended snapshot/creature/generation models.
4. Add/adjust Backend tests in `Backend/tests/` and Unity EditMode tests for pure collectors (`AnalyticsCollectorTests`).
5. Optionally display in `SimulationHud`.
6. Avoid computing domain rules inside Analytics — only aggregate what Simulation/Creatures already expose via Common contracts. See [ANALYTICS.md](ANALYTICS.md).

---

## Assembly / dependency checklist

Before merging:

- [ ] New script is in the correct module folder + namespace
- [ ] Asmdef references still match [ARCHITECTURE.md](ARCHITECTURE.md) dependency direction
- [ ] No new god-manager appeared
- [ ] Tests added for pure logic
- [ ] Docs updated if ownership boundaries changed
