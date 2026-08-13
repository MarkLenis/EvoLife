# Agent Boundaries (Parallel Work Map)

This guide prevents duplicate systems when multiple human or coding agents work in parallel.

**Rule:** If a change fits an existing owner, extend that owner. Do not create a second clock, vitals store, genome type, or stats client elsewhere.

---

## Ownership matrix

| Responsibility | Owner module | Canonical types | Do not implement in |
|----------------|--------------|-----------------|---------------------|
| Health, hunger, thirst, energy, age | **Creatures** | `CreatureVitals`, `SpeciesVitalsDefinition` | AI, Genetics, Simulation, Analytics, UI |
| Creature id / role / species label | **Creatures** | `CreatureIdentity` | AI, Analytics |
| Apply phenotype to speed/metabolism/senses | **Creatures** | `CreatureCapabilityMotor` | Genetics (Genetics only *produces* phenotype) |
| Genome data, crossover, mutation | **Genetics** | `Genome`, `IGeneticOperators` | Creatures, AI, Simulation (Simulation may *call* operators when spawning or reproducing) |
| Phenotype decode | **Genetics** | `IGenomeDecoder`, `Phenotype`, `CreatureGenome` | Creatures, AI |
| Plants, water, resource regen / query, biomes, day/night | **Environment** | `IResourceNode`, `PlantResource`, `WaterSource`, `ResourceRegistry`, `ResourceManager`, `BiomeMap`, `DayNightManager` | Creatures, AI |
| Ecological event orchestration | **Environment** | `EnvironmentalEventManager`, `EnvironmentalEventConfig` | Creatures (use vitals APIs via Simulation port), AI |
| Event damage / spawn / remove | **Simulation** | `EnvironmentalCreatureBridge` | Environment must not reference Creatures |
| Simulation time, pause, time scale | **Simulation** | `SimulationClock` | Anywhere else |
| Population counts registry | **Simulation** | `PopulationTracker` | Analytics (Analytics only *reads*) |
| Spawning / initial wiring of components | **Simulation** | `CreatureSpawner` | AI, Genetics |
| Reproduction eligibility, local mating, offspring spawn | **Simulation** | `ReproductionSystem`, `ReproductionEligibility`, `OffspringComposer`, `CreatureReproductionBridge` | Creatures (no mating in `CreatureVitals`), AI (request only), Genetics (operators only) |
| Founder population / extinction report | **Simulation** | `InitialPopulationSpawner`, `EcosystemManager`, `ExtinctionEvaluator` | Analytics |
| Training-support respawn | **Simulation** | `TrainingRespawnController` | Creatures, AI |
| Experiment/sim config assets | **Simulation** | `SimulationConfig`, `ExperimentConfiguration`, `EcosystemSettings`, `ReproductionConfig`, `ExperimentOrchestrator` (pause/unpause + one environment apply per run) | Backend (Backend stores experiment *records*) |
| Tick fan-out | **Simulation** | `SimulationRunner` | — keep this thin; `ExperimentOrchestrator` owns run start/stop only |
| Observations | **AI** | `IObservationSource`, `VitalObservationSource`, `CompositeObservationSource`, `CreatureObservationSchema`, optional `EnvironmentObservationSource` (not in PPO v2) | Creatures, Environment |
| Actions / locomotion intent | **AI** | `IActionExecutor`, `PlanarMoveActionExecutor`, `CreatureActionSchema`, `LocalCreatureInteractor` | Simulation (Simulation owns whether `reproduce_request` succeeds) |
| Rewards | **AI** | `IRewardCalculator`, `TrainingRewardCalculator`, `SurvivalRewardCalculator` | Creatures |
| Scripted baseline vs PPO policy | **AI** | `CreatureBrain`, `ScriptedBaselinePolicy`, `BaselineMotiveEvaluator`, `ScriptedBaselineSettings` / `ScriptedBaselineProfile`, `EvoLifeCreatureAgent`, `PpoPolicyAdapter` (idle fallback) | Simulation |
| Stats snapshots + HTTP upload | **Analytics** | `SimulationStatsSnapshot`, `BackendClient`, `StatsExportLoop`, `AnalyticsExportController`, collectors, lifetime/generation records | Simulation, UI, Creatures, AI |
| HUD / presentation | **UI** | `SimulationHud` | Domain modules |
| Shared contracts only | **Common** | `IReadOnly*`, `IPolicyKindOwner`, `IPolicySeedOwner`, `IExperimentAnalyticsSession`, `IReproductionRequestHandler`, `IEnvironmentalVitalEffects`, `IEnvironmentalPopulationCommands`, IDs, enums | Gameplay behavior |
| REST experiment/stats API | **Backend** | FastAPI `app/` | Unity (except DTO field alignment) |
| PPO YAML / train scripts | **Training** | `Training/configs`, `Training/scripts`, `Training/experiments` | Unity runtime folders |

---

## Parallel swimlanes (suggested)

Agents can work simultaneously if they stay in lane:

| Lane | Safe focus | Integration points |
|------|------------|--------------------|
| A — Creatures | Vitals formulas, species assets, motor | Consumes `IReadOnlyPhenotype`; exposes `IReadOnlyVitalState` |
| B — Genetics | Operators, decoder, gene layout versioning | Outputs phenotype; used by Spawner |
| C — Environment | Resources, biomes, day/night, events | `IResourceNode` for AI queries; creature ports in Common |
| D — AI | Obs/action/reward, ML-Agents Agent class | Reads vitals/resources; never owns them |
| E — Simulation | Spawn balancing, config, world bootstrap | Calls into A/B/C; does not implement brains |
| F — Analytics + Backend | Metrics, API, schemas | DTO parity with Unity snapshot |
| G — UI | HUD, controls | Bind to Analytics/Simulation read APIs |
| H — Training | YAML, eval scripts | Behavior names must match AI Agent |

**Conflict zones** (serialize changes or pair up):

- Changing observation vector size (AI + Training + any sensor code)
- Changing `SimulationStatsSnapshot` fields (Analytics + Backend + UI)
- Changing creature lifetime / generation upload DTOs (Analytics + Backend)
- Changing `CanonicalGenomeSchema` traits (Genetics + Creatures phenotype consumers + ML observation size)
- Adding components required on creature prefabs (Simulation spawn + AI + Creatures)

---

## Decision shortcuts

| If you want to… | Then edit… |
|-----------------|------------|
| Make animals get hungry faster | `SpeciesVitalsDefinition` / `CreatureVitals` |
| Make offspring differ from parents | `IGeneticOperators` |
| Make genes affect top speed | `IGenomeDecoder` + `CreatureCapabilityMotor` |
| Add berries as food | New `IResourceNode` in Environment |
| Add drought / wildfire | `EnvironmentalEventConfig` + Environment manager; Simulation bridge for damage/spawn |
| Add time-of-day to PPO | Do **not** silently grow schema v2 (size 31). Use `EnvironmentObservationSource`, then bump schema + Training YAML |
| Change RL reward for eating | `IRewardCalculator` implementation |
| Speed up the sim | `SimulationClock` / `SimulationConfig` |
| Run a reproducible experiment | `ExperimentConfiguration` + `ExperimentOrchestrator` (see [EXPERIMENTS.md](EXPERIMENTS.md)) |
| Count births for graphs | Analytics collector + Backend schema (see [ANALYTICS.md](ANALYTICS.md)) |
| Compare PPO vs scripted | `AgentPolicyKind` on `CreatureBrain` / `SimulationConfig`; Analytics records `policy_kind` (see [AI_ML_AGENTS.md](AI_ML_AGENTS.md), [SCRIPTED_BASELINE.md](SCRIPTED_BASELINE.md), [ANALYTICS.md](ANALYTICS.md)) |
| Add sexual reproduction / generations | `ReproductionSystem` + `IGeneticOperators` (see [REPRODUCTION.md](REPRODUCTION.md)) |

---

## Anti-patterns (reject in review)

1. **Second vitals** on an AI or Simulation component  
2. **Genome mutation** inside `CreatureBrain` or reward code  
3. **HTTP calls** from Simulation or Creatures  
4. **Expanding `SimulationRunner`** into a service locator / god object  
5. **UI buttons** that directly modify hunger/health instead of calling Creatures APIs  
6. **Duplicating** `BackendClient` in Training scripts that scrape Unity logs — extend Analytics/Backend instead  
7. **XR / VR** assemblies or scenes — out of scope  

---

## Extension checklist for coding agents

Before writing code:

1. Read this file and [ARCHITECTURE.md](ARCHITECTURE.md).  
2. Name the owning module in the PR/commit message.  
3. Prefer interfaces in `Common` over concrete cross-references.  
4. Add tests in the owner’s test surface (EditMode vs Backend pytest).  
5. Update this boundaries file if ownership moves.

---

## Manual Unity verification (all agents)

After Unity-side changes, a human (or Unity-equipped runner) should confirm:

- [ ] Project opens on the configured Editor version without package errors  
- [ ] Assemblies compile (no asmdef cycles)  
- [ ] EditMode tests pass in Test Runner (`AnalyticsCollectorTests`, `PopulationTrackerTests`, genetics/AI tests)  
- [ ] Creature prefab has exactly one owner component per concern (vitals, genome, brain)  
- [ ] ML-Agents package imports and `EVOLIFE_MLAGENTS` define appears when expected
- [ ] `EvoLifeCreatureAgent` Behavior Parameters names are `EvoLifeHerbivore` / `EvoLifePredator`
- [ ] Observation vector size is 31 (`CreatureObservationSchema` v2). Time-of-day is **not** in that vector.
- [ ] Action space is 3 continuous + 1 discrete branch of size 6 (`CreatureActionSchema` v2)
- [ ] Scripted baseline EditMode tests pass (`BaselineMotiveEvaluatorTests`, `ScriptedBaselinePolicyTests`)
- [ ] Reproduction EditMode tests pass (`ReproductionTests`, `EcosystemLifecycleTests`), including spawn-failure commit tests (no energy/health/cooldown on `SpawnFailed`)
- [ ] Environment EditMode tests pass (`PlantResourceTests`, `DayNightCycleTests`, `EnvironmentalEventTests`)
- [ ] Experiment EditMode tests pass (`ExperimentConfigurationTests`, `ExperimentLifecycleTests`) — initialization stays paused until `BeginRunning`; environment applicator/placement happens once per orchestrated start
- [ ] Analytics export retention tests pass (`AnalyticsExportControllerTests`)  
