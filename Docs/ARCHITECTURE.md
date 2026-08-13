# EvoLife Architecture

EvoLife is a desktop Unity ecosystem simulator centered on **AI** (ML-Agents / PPO), with genetics, scripted baselines, and a FastAPI analytics backend. There is **no XR/VR**.

This document describes the architectural skeleton established in the repository. Implementations are intentionally thin seams, not finished gameplay or trained policies.

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
  Environment/   Plants, water, resource registry
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
| **Environment** | Plant/water resources, regeneration, resource queries | Creature brains, population counts |
| **Simulation** | Clock, time scale, population registry, spawning, experiment config, tick fan-out | Observation vectors, HTTP |
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

Avoid “god managers.” `SimulationRunner` only fans out ticks; it must not accumulate unrelated systems.

---

## Important interfaces / contracts

| Contract | Module | Purpose |
|----------|--------|---------|
| `IReadOnlyVitalState` | Common | AI/UI/Analytics read vitals without mutation rights |
| `IReadOnlyPhenotype` | Common | Capability multipliers from genetics |
| `ICreatureIdentity` | Common | Id, role, species |
| `ISimulationClock` | Common | Time / scale / pause |
| `IPopulationSnapshot` | Common | Alive counts |
| `ISimulationTickable` | Common | Sim-time step hook |
| `IGenomeDecoder` / `IGeneticOperators` | Genetics | Inheritance pipeline |
| `IResourceNode` | Environment | Consumable world resources |
| `IObservationSource` / `IActionExecutor` / `IRewardCalculator` | AI | RL plumbing |
| `ICreaturePolicy` | AI | Scripted vs PPO step API |
| `IStatisticCollector` | Analytics | Snapshot production |

---

## Expected Unity data flow

1. **Bootstrap / config**  
   `SimulationConfig` (ScriptableObject) supplies seed, initial counts, policy kinds, time scale.

2. **Spawn**  
   `CreatureSpawner` instantiates a prefab, assigns `CreatureId`, initializes `CreatureVitals`, creates/assigns `Genome` via Genetics operators, decodes phenotype, applies it through `CreatureCapabilityMotor`.

3. **Tick**  
   `SimulationClock` advances sim time. `SimulationRunner` calls `ISimulationTickable.Tick` on registered systems (vitals, plants, etc.).

4. **Decide**  
   `CreatureBrain` selects `ScriptedBaselinePolicy` or `PpoPolicyAdapter`. Policy reads observations (vitals (+ later sensors)), computes/receives actions, applies them via `IActionExecutor`.

5. **Act / interact**  
   Movement and future eat/drink/attack use Environment `IResourceNode` and Creatures APIs (`ConsumeFood`, `Drink`, `ApplyDamage`). AI does not bypass these.

6. **Measure**  
   `PopulationStatisticCollector` builds `SimulationStatsSnapshot`. `StatsExportLoop` optionally POSTs to FastAPI via `BackendClient`.

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

- PPO / ML-Agents code lives under **AI**, behind `ICreaturePolicy` / future `Agent` subclass.
- Rewards are computed in `IRewardCalculator`, not inside vitals.
- Observations may include phenotype-derived sensory range, but **do not** mutate genomes.
- `PpoPolicyAdapter` is a compile-safe seam; when ML-Agents is imported, `EVOLIFE_MLAGENTS` is defined via asmdef `versionDefines`. Wire `Agent.CollectObservations` / `OnActionReceived` at that seam without moving vitals ownership.

Training configs live in `Training/configs/*.yaml` and must use behavior names that match the Unity Agent behavior name once added.

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
                               CreatureVitals
```

- Gene layout v0: `[speed, metabolism, sensory, reproduction]` in \[0,1\], mapped to multipliers ≈ \[0.5, 1.5\].
- Reproduction / inheritance: Simulation (or a future dedicated Reproduction service in Genetics+Simulation) calls `IGeneticOperators.Crossover` + `Mutate`, then spawns with the child genome.
- Creatures never implement crossover/mutation.

---

## Backend interaction

Unity `BackendClient` POSTs JSON snapshots to:

- `POST /api/v1/stats`
- Experiments: `POST/GET /api/v1/experiments`
- Health: `GET /health`

Payload fields match `SimulationStatsSnapshot` / Pydantic `StatsSnapshotIn` (`experimentId`, counts, sim time, unix timestamp).

Backend uses an in-memory store for early development; swap persistence later without changing Unity contracts.

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
| New gene | Genetics decoder + gene count | Phenotype consumers in Creatures |
| New statistic | Analytics (+ Backend schema) | UI display optional |
| PPO training | AI Agent wiring + Training configs | Reward calculator only for shaping |

---

## Explicit non-goals (current skeleton)

- Final terrain, meshes, animations, VFX
- Tuned RL rewards or trained ONNX models
- ShaderGraph assets
- XR / VR
- Full evolutionary algorithm research suite (operators are seams only)
