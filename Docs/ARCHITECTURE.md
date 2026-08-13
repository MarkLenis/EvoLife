# EvoLife Architecture

EvoLife is a desktop Unity ecosystem simulator centered on **AI** (ML-Agents / PPO), with genetics, scripted baselines, and a FastAPI analytics backend. There is **no XR/VR**.

This document describes the architectural skeleton established in the repository. Implementations are intentionally thin seams, not finished gameplay or trained policies.

Environment resources, biomes, day/night, and ecological events: [ENVIRONMENT.md](ENVIRONMENT.md), [ENVIRONMENT_EVENTS.md](ENVIRONMENT_EVENTS.md).

Experiments and training curriculum: [EXPERIMENTS.md](EXPERIMENTS.md), [TRAINING_CURRICULUM.md](TRAINING_CURRICULUM.md).

## Unity version

The repository previously contained only a README (no Unity project). The scaffold targets:

- **Unity 2022.3 LTS** (`ProjectSettings/ProjectVersion.txt`)
- **ML-Agents** `com.unity.ml-agents` **2.0.1** (`Packages/manifest.json`)

If the team standardizes on a different 2022.3 patch or Unity 6 later, update `ProjectVersion.txt` and re-validate ML-Agents compatibility.

---

## Major modules

```
Assets/EvoLife/Scripts/
  Common/        Shared IDs, enums, read-only contracts
  Creatures/     Biological vitals + capability application
  Genetics/      Genome, crossover, mutation, phenotype decode
  Environment/   Plants, water, biomes, day/night, resource registry, event effects on resources
  Simulation/    Time, population, spawning, config, tick runner
  AI/            Observations, actions, rewards, policies
  Analytics/     Stats capture + backend HTTP client
  UI/            Presentation only
```

Supporting trees:

| Path | Role |
|------|------|
| `Assets/EvoLife/Prefabs/` | Creature / environment / UI prefabs |
| `Assets/EvoLife/ScriptableObjects/` | Species, simulation, environment data assets |
| `Assets/EvoLife/Scenes/` | Simulation scenes |
| `Assets/EvoLife/Tests/` | EditMode / PlayMode tests |
| `Backend/` | FastAPI analytics API |
| `Training/` | ML-Agents YAML + scripts |

Each Unity module has an **assembly definition** (`EvoLife.*.asmdef`) so compile boundaries match domain boundaries.

---

## Responsibilities

| Module | Owns | Must not own |
|--------|------|--------------|
| **Common** | IDs, enums, `IReadOnly*` contracts, tick interfaces | Gameplay logic |
| **Creatures** | Health, hunger, thirst, energy, age; applying phenotype multipliers to motors/vitals | Genomes, rewards, spawning policy |
| **Genetics** | Genome storage, crossover, mutation, phenotype decoding | Vital drain formulas, RL rewards |
| **Environment** | Plant/water resources, regeneration, biomes, day/night, ecological event resource effects, resource queries | Creature brains, population counts, vitals mutation |
| **Simulation** | Clock, time scale, population registry, spawning, reproduction, ecosystem mode, experiment config, tick fan-out | Observation vectors, HTTP |
| **AI** | Observations, action execution, reward calculation, scripted vs PPO policy selection | Mutating vitals directly, genome operators |
| **Analytics** | Snapshots, export loop, backend transport | Simulation rules |
| **UI** | HUD / controls presentation | Domain logic |
| **Backend** | Experiment records, stats persistence API | Unity runtime |
| **Training** | PPO configs / train scripts | Runtime creature state |

---

## Dependency direction

Allowed references (arrows mean “depends on”):

```
UI -----------> Analytics ------> Simulation
                     \               |
                      \              v
                       \         Creatures <---- AI
                        \            ^            |
                         \           |            |
                          \      Genetics <-------+
                           \         ^
                            \        |
                             \   Environment
                              \      ^
                               \     |
                                Common  <--- (everything may use Common)
```

Concrete asmdef rules:

- `Common` → nothing EvoLife-specific
- `Creatures`, `Genetics`, `Environment` → `Common` only
- `Simulation` → `Common`, `Creatures`, `Genetics`, `Environment`
- `AI` → `Common`, `Creatures`, `Genetics`, `Environment` (reads state; does not reference Analytics/UI)
- `Analytics` → `Common`, `Simulation`
- `UI` → `Common`, `Simulation`, `Analytics`

**Forbidden:** Creatures → AI; Genetics → Simulation; Environment → Creatures; circular asmdef references.

Environment talks to creatures only through Common ports (`IEnvironmentalVitalEffects`, `IEnvironmentalPopulationCommands`) implemented by Simulation.

Avoid “god managers.” `SimulationRunner` only fans out ticks; it must not accumulate unrelated systems.

---

## Important interfaces / contracts

| Contract | Module | Purpose |
|----------|--------|---------|
| `IReadOnlyVitalState` | Common | AI/UI/Analytics read vitals without mutation rights (includes per-creature `MaxHunger` / `MaxThirst`) |
| `IReadOnlyPhenotype` | Common | Capability multipliers from genetics |
| `ICreatureIdentity` | Common | Id, role, species |
| `ISimulationClock` | Common | Time / scale / pause |
| `IPopulationSnapshot` | Common | Alive counts |
| `ISimulationTickable` | Common | Sim-time step hook |
| `IGenomeDecoder` / `IGeneticOperators` | Genetics | Inheritance pipeline (`CanonicalGenomeSchema` v1) |
| `IResourceNode` | Environment | Consumable world resources |
| `IReadOnlyResourceCensus` / `IReadOnlyEnvironmentState` / `IReadOnlyDayNightState` | Common | Analytics/AI read plants, events, time-of-day without mutation |
| `IEnvironmentalVitalEffects` / `IEnvironmentalPopulationCommands` | Common | Event manager ports; Simulation implements via vitals + lifecycle |
| `DayNightManager` / `ResourceManager` / `EnvironmentalEventManager` | Environment | Sim-time cycle, seeded resources, config-driven events |
| `EnvironmentalCreatureBridge` | Simulation | Applies event damage/spawn/remove through existing owners |
| `IObservationSource` / `IActionExecutor` / `IRewardCalculator` | AI | RL plumbing |
| `ICreaturePolicy` | AI | Scripted vs PPO step API (`ScriptedBaselinePolicy` heuristic, `EvoLifeCreatureAgent` / `PpoPolicyAdapter`) |
| `EvoLifeCreatureAgent` | AI | ML-Agents Agent bridge (`EvoLifeHerbivore` / `EvoLifePredator`) |
| `CreatureObservationSchema` | AI | Documented observation size/order (v2, size 31) |
| `IStatisticCollector` | Analytics | Snapshot production |
| `AnalyticsSnapshotBuilder` / `CreatureLifetimeFactory` / `GenerationAggregator` | Analytics | Pure experiment metrics |
| `CreatureLifecycleHub` | Simulation | Spawn/death fan-out for observers |
| `ReproductionSystem` / `EcosystemManager` | Simulation | Local mating, offspring spawn, founder population, extinction report |
| `ExperimentConfiguration` / `ExperimentOrchestrator` | Simulation | Serializable experiment document, start/stop lifecycle (not a god manager) |
| `IReproductionRequestHandler` | Common | AI request seam; Simulation implements success/failure |
| `IAnalyticsCreatureView` / `ICreatureLineage` / `IReadOnlyGenomeTraits` / `IEpisodeMetrics` | Common | Read-only analytics observation |
| `GeneticObservationProvider` | Genetics | Normalized [0,1] genome vector for ML observations (`CreatureObservationSchema` indices 6–14) |
| `IPolicyKindOwner` | Common | Simulation can set scripted vs PPO without referencing AI |

---

## Expected Unity data flow

1. **Bootstrap / config**  
   `SimulationConfig` / `ExperimentConfiguration` supplies seed, initial counts, policy kinds, resources, events, and stop rules. `ExperimentOrchestrator` loads that document, asks existing owners to initialize, and ends the run. See [EXPERIMENTS.md](EXPERIMENTS.md).

2. **Spawn**  
   `CreatureSpawner` instantiates a prefab, assigns `CreatureId`, initializes `CreatureVitals`, creates/assigns `Genome` via Genetics operators, decodes phenotype, applies it through `CreatureCapabilityMotor`, and sets `IPolicyKindOwner` from the requested `AgentPolicyKind`.

3. **Tick**  
   `SimulationClock` advances sim time. `SimulationRunner` calls `ISimulationTickable.Tick` on registered systems (vitals, plants, day/night, events, etc.). Day/night and events use that delta, not wall-clock time.

4. **Decide**  
   `CreatureBrain` selects exclusive control: `ScriptedBaselinePolicy` or `EvoLifeCreatureAgent` (learned PPO). Policy reads observations (vitals + genetics + optional local sensors), computes/receives actions, applies them via `IActionExecutor`. Scripted and PPO never run on the same creature at once. The scripted baseline is a utility/priority heuristic over the same observation schema; see [SCRIPTED_BASELINE.md](SCRIPTED_BASELINE.md).

5. **Act / interact**  
   Movement and eat/drink/attack/rest/reproduce_request use the same canonical `IActionExecutor` path (`PlanarMoveActionExecutor` + `LocalCreatureInteractor`). Eat/drink/attack call Environment `IResourceNode` and Creatures APIs (`ConsumeFood`, `Drink`, `ApplyDamage`). AI does not bypass these or write vital fields. PPO and the scripted baseline share CreatureObservationSchema v2 and CreatureActionSchema v2 ([AI_ML_AGENTS.md](AI_ML_AGENTS.md), [SCRIPTED_BASELINE.md](SCRIPTED_BASELINE.md)). `reproduce_request` is forwarded to Simulation (`IReproductionRequestHandler` / `ReproductionSystem`); Simulation decides whether a local eligible mate exists. See [REPRODUCTION.md](REPRODUCTION.md).

6. **Measure**  
   `PopulationStatisticCollector` builds `SimulationStatsSnapshot`. `CreatureLifetimeRecorder` observes spawn/death via `CreatureLifecycleHub` (Common contracts). `StatsExportLoop` batches POSTs to FastAPI via `BackendClient` (v1 `/stats` or extended run/snapshot/creature/generation endpoints). Failed POSTs retain pending records until a later successful flush (bounded FIFO overflow). See [ANALYTICS.md](ANALYTICS.md).

7. **Present**  
   `SimulationHud` renders collector/clock data only.

---

## How ML-Agents interacts with creature state

```
CreatureVitals (source of truth)
        │ read-only via IReadOnlyVitalState
        v
IObservationSource  --->  policy (PPO Agent / adapter)
IRewardCalculator   <---  reads vitals + episode end flags
        │
        v
IActionExecutor  --->  movement / interaction components
                          │
                          v
                   CreatureVitals mutations
                   (eat, drink, damage) via Creatures APIs
```

Rules:

- PPO / ML-Agents code lives under **AI** in `EvoLifeCreatureAgent` behind `ICreaturePolicy` / `CreatureBrain` control mode.
- Rewards are computed in `IRewardCalculator` / `TrainingRewardCalculator`, not inside vitals.
- Observations may include `GeneticObservationProvider` normalized genes and phenotype-derived sensory range, but **do not** mutate genomes.
- Hunger/thirst observations must use `IReadOnlyVitalState.MaxHunger` / `MaxThirst`, not a hard-coded 100.
- `PpoPolicyAdapter` is an idle fallback when the Agent or ML-Agents package is missing. It is not a second PPO implementation.
- Behavior names: `EvoLifeHerbivore` and `EvoLifePredator` (`MlAgentsBehaviorNames`). Observation layout: `CreatureObservationSchema` v2 (size 31). Action layout: `CreatureActionSchema` v2 (3 continuous + discrete interaction branch of size 6). See [AI_ML_AGENTS.md](AI_ML_AGENTS.md).

Training configs live in `Training/configs/*.yaml` and must use those same behavior names.

---

## How genetics modifies creature capabilities

```
Genome  --(IGenomeDecoder)-->  Phenotype (IReadOnlyPhenotype)
                                    │
                                    v
                         CreatureCapabilityMotor.ApplyPhenotype
                                    │
                    ┌───────────────┼────────────────┐
                    v               v                v
              max speed      metabolism on     sensory range
              sprint speed     CreatureVitals
                               (incl. max energy / max age)
```

- Canonical genome: **schema v1**, nine named traits (`TraitId` / `CanonicalGenomeSchema`). See [GENETICS.md](GENETICS.md). Unity C# is the runtime; `evolife/genetics/` is the offline reference of the same schema.
- Decode: `CanonicalGenomeDecoder` maps trait / default → phenotype multipliers (aggression stays raw \[0,1\]).
- `CreatureSpawner` calls `IGeneticOperators.CreateFounder` — it does not choose gene count or layout.
- Reproduction / inheritance: Simulation `ReproductionSystem` calls `IGeneticOperators.Crossover` + `Mutate`, then `CreatureSpawner` with the child genome, generation, and parent ids. See [REPRODUCTION.md](REPRODUCTION.md).
- Creatures never implement crossover/mutation.

---

## Backend interaction

Unity `BackendClient` reuses the existing FastAPI app (no second analytics service):

- v1: `POST /api/v1/stats`, `POST/GET /api/v1/experiments`, `GET /health`
- Extended (same SQLite run id): `POST /api/v1/runs`, snapshot batch, creature records, generation summaries
- Queries: population-series, evolution-series, survival, policy-comparison, trait-evolution

Payloads: v1 `SimulationStatsSnapshot` / `StatsSnapshotIn` (required camelCase fields unchanged; optional births/deaths/policy counts). Extended DTOs use snake_case and nested trait maps.

Default persistence is **SQLite**. See [ANALYTICS.md](ANALYTICS.md) and [Backend/README.md](../Backend/README.md).

---

## How future contributors should extend the project

1. **Identify the owning module** using [AGENT_BOUNDARIES.md](AGENT_BOUNDARIES.md).
2. **Add types in that module**; expose cross-cutting needs via `Common` interfaces.
3. **Prefer new small components** over expanding `SimulationRunner` / `CreatureBrain` into managers.
4. **Add EditMode tests** for pure logic (genetics, collectors); PlayMode tests for spawn/tick integration.
5. **Do not** introduce XR, ShaderGraph content, tuned reward tables, or art pipelines in architecture PRs.
6. **Keep Training YAML** in sync when renaming Agent behaviors.

### Adding major features (pointers)

| Feature | Primary module | Touch carefully |
|---------|----------------|-----------------|
| New species | Creatures + Prefabs + Simulation spawn config | AI obs size compatibility |
| New resource | Environment | AI observations / ResourceRegistry |
| Ecological event | Environment `EnvironmentalEventManager` + Simulation `EnvironmentalCreatureBridge` | Do not mutate `CreatureBiology` fields; do not bypass `CreatureSpawner` |
| Day/night or biome | Environment `DayNightManager` / `BiomeMap` | PPO observation size — use `EnvironmentObservationSource` until the schema is bumped |
| New gene | `CanonicalGenomeSchema` + decoder | Phenotype consumers in Creatures; bump schema version if observation size changes |
| New statistic | Analytics (+ Backend schema) | UI display optional |
| PPO training | AI Agent wiring + Training configs + curriculum stages | Reward calculator only for shaping; see [TRAINING_CURRICULUM.md](TRAINING_CURRICULUM.md) |
| Evaluation experiment | Simulation `ExperimentConfiguration` / `ExperimentOrchestrator` | Analytics records only; see [EXPERIMENTS.md](EXPERIMENTS.md) |
| Scripted baseline heuristic | AI `ScriptedBaselinePolicy` / settings profile | Observation schema, Creatures/Environment APIs |
| Reproduction / generations | Simulation `ReproductionSystem` + Genetics operators | Creature prefab request bridge; analytics lineage |

---

## Explicit non-goals (current skeleton)

- Final terrain, meshes, animations, VFX
- Tuned RL rewards or trained ONNX models
- ShaderGraph assets
- XR / VR
- Full evolutionary algorithm research suite (operators are seams only)
