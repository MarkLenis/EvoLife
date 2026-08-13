# EvoLife Experiment Analytics

Analytics **observes** the simulation. It does not control biology, genetics, spawning, or policy behavior.

Related: [ARCHITECTURE.md](ARCHITECTURE.md), [AGENT_BOUNDARIES.md](AGENT_BOUNDARIES.md), [EXPERIMENTS.md](EXPERIMENTS.md), [AI_ML_AGENTS.md](AI_ML_AGENTS.md), [GENETICS.md](GENETICS.md), [REPRODUCTION.md](REPRODUCTION.md), [ENVIRONMENT.md](ENVIRONMENT.md), [Backend/README.md](../Backend/README.md).

## Metric families

Keep these separate when plotting or comparing runs.

| Family | What it measures | Typical sources |
|--------|------------------|-----------------|
| **Simulation metrics** | Ecosystem population over sim time | Snapshots: alive counts, births, deaths, population change, scripted vs PPO alive |
| **Evolution metrics** | Change in genomes across generations | Creature lifetime records + generation summaries (trait mean/variance) |
| **RL episode metrics** | PPO episode survival / return | Optional `IEpisodeMetrics` on the creature brain/agent. Scripted creatures usually have no return |

There is **no global genetic fitness score**. Selection is whatever emerges from survival and reproduction.

## Experiment lifecycle

```
ExperimentConfiguration / SimulationConfig
        │
        v
ExperimentOrchestrator  (load, init env/population, stop conditions)
        │
        v
ExperimentSession  --POST /api/v1/runs-->  FastAPI SimulationRun
        │
        ├─ CreatureSpawner + CreatureLifecycleHub
        │       spawn/death events (Common contracts only)
        │
        ├─ PopulationStatisticCollector     simulation snapshots
        ├─ CreatureLifetimeRecorder         per-creature records on death
        └─ GenerationAnalyticsCollector     trait aggregates
                │
                v
        StatsExportLoop (interval, default 5s, never per-frame)
                │
                ├─ POST /api/v1/runs/{id}/snapshots/batch
                ├─ POST /api/v1/runs/{id}/creatures
                └─ POST /api/v1/runs/{id}/generations   (upsert by species+generation)
```

If run creation or upload fails, Unity logs a warning (throttled to the export interval) and the simulation continues. Local collectors still capture in-memory.

Pending snapshot, lifetime, and generation records are **retained until a POST is confirmed successful**. `AnalyticsExportController` keeps bounded queues (default 64 snapshots, 256 lifetime records). One flush may be in flight at a time; records added during a flush are not dequeued with that batch.

**Overflow policy:** when a pending queue is full, the oldest record is dropped (FIFO) so memory cannot grow forever. Overflow is logged at most once per export interval.

Fallback: when the extended run was not created, `StatsExportLoop` posts the v1 `POST /api/v1/stats` payload only (population snapshot), with the same retain-until-success behavior. Creature/generation uploads require a successful run id.

## What gets recorded

### Simulation snapshot (interval)

Always measurable:

- `herbivoreCount` / `predatorCount` / `totalAlive`
- `births` / `deaths` (cumulative from `PopulationTracker`)
- `populationChange` (alive now minus previous snapshot)
- `simulationTimeSeconds`, UTC timestamp
- `scriptedAlive` / `ppoAlive` (from live `IPolicyKindOwner` views)
- `maxGeneration` among living observed creatures

Not recorded on the HTTP snapshot yet (contracts exist; upload not wired):

- plant counts / density / abundance (`IReadOnlyResourceCensus`)
- active ecological events and start/end times (`IReadOnlyEnvironmentalEvent`)
- food/water consumed totals (biology does not accumulate them)
- kills (no combat accounting yet)

### Creature lifetime record (on death)

Built from read-only contracts (`ICreatureIdentity`, `IReadOnlyVitalState`, `ICreatureLineage`, `IPolicyKindOwner`, `IReadOnlyGenomeTraits`, optional `IEpisodeMetrics`). HTTP is **not** called from `CreatureVitals` or `CreatureBiology`.

| Field | Source |
|-------|--------|
| creature id, species, role | `ICreatureIdentity` |
| generation, parent ids | `ICreatureLineage` (founders: generation 0, no parents) |
| lifetime / age | vitals age, else death_time − birth_time |
| cause of death | `DeathCause` on the Common death notice |
| policy kind | `scripted_baseline` or `learned_ppo` |
| genome traits | canonical schema v1 names/values |
| offspring count | incremented when a later spawn lists this id as a parent |
| episode return / survival | only if `IEpisodeMetrics.HasEpisodeReturn` |

### Generation aggregate

One uploaded row per `(species, generation)`:

- population count, average lifespan
- average trait values + variance/min/max
- `extra_statistics.by_policy` for scripted vs PPO slices
- `max_generation` reached in the observed set

Re-posts **upsert**; they do not duplicate unique species/generation rows.

### Experiment metadata (reproducibility)

Stored on the run, not a dump of the Unity ScriptableObject:

- experiment name, random seed, timestamp
- herbivore/predator policy kinds
- initial herbivore/predator counts, time scale
- resource abundance, plant regen, mutation, day length, enabled events
- derived deterministic seeds (founders, reproduction, resources, events, wander)
- ecosystem mode (`persistent_ecosystem` / `training_support`) and whether training respawn is enabled
- optional `scenario_id`, optional `training_model_id` / curriculum stage
- stop reason when the run finishes (`max_simulation_time`, extinction, `manual_stop`)

## Upload frequency / batching

| Stream | Default | Notes |
|--------|---------|-------|
| Snapshots | every `StatsExportLoop.intervalSeconds` (5s) | Optional `snapshotBatchSize` coalesces several captures |
| Creature records | same interval | Deaths queue in memory until the next flush |
| Generation summaries | on flush when dirty (after deaths) and on disable | Upserted |

Do not enable per-frame HTTP. `BackendClient.enableUploads` can disable transport entirely.

## Backend endpoints

### Unity v1 (unchanged required fields)

| Method | Path | Use |
|--------|------|-----|
| POST/GET | `/api/v1/experiments` | Create/list experiments (id = run_id) |
| POST/GET | `/api/v1/stats` | Population snapshot. Optional extras: `births`, `deaths`, `scriptedAlive`, `ppoAlive`, … |

### Extended run API (same SQLite, same run id)

| Method | Path | Use |
|--------|------|-----|
| POST | `/api/v1/runs` | Create run with configuration metadata |
| POST | `/api/v1/runs/{id}/finish` | Mark completed/failed |
| POST | `/api/v1/runs/{id}/snapshots/batch` | Time-series population points |
| POST/GET | `/api/v1/runs/{id}/creatures` | Lifetime records (`?policy_kind=&species=&generation=`) |
| POST/GET | `/api/v1/runs/{id}/generations` | Generation summaries |
| GET | `/api/v1/runs/{id}/population-series` | Population over simulation time |
| GET | `/api/v1/runs/{id}/evolution-series` | Generation progression |
| GET | `/api/v1/runs/{id}/survival` | Survival records |
| GET | `/api/v1/runs/{id}/policy-comparison` | Scripted vs PPO aggregates |
| GET | `/api/v1/runs/{id}/trait-evolution?trait=` | Mean/variance of one trait by generation |

## Example: PPO vs scripted comparison

1. Create two `SimulationConfig` assets (or one mixed config):
   - all scripted: both policies `ScriptedBaseline`
   - all PPO: both `LearnedPpo`
   - mixed: herbivores PPO, predators scripted (or the reverse)
2. Set a distinct `experimentName`, shared `randomSeed` if you want paired worlds, and optional `trainingModelId` for the PPO run.
3. Start the backend, enable `BackendClient` uploads + `ExperimentSession.createRunOnStart` + `StatsExportLoop.uploadToBackend`.
4. Run each scenario to completion (or a fixed sim-time budget).
5. Query:

```bash
# population curves
curl -s http://127.0.0.1:8000/api/v1/runs/$RUN/population-series | python -m json.tool

# survival by policy
curl -s "http://127.0.0.1:8000/api/v1/runs/$RUN/survival?policy_kind=learned_ppo"

# side-by-side policy metrics
curl -s http://127.0.0.1:8000/api/v1/runs/$RUN/policy-comparison
```

Compare `mean_lifetime`, death-cause histograms, and (for PPO) `mean_episode_return` when present. Population series `scripted_alive` / `ppo_alive` in `extra_metrics` show mixed-run composition over time.

Scripted baseline behavior, sensors, and fairness notes: [SCRIPTED_BASELINE.md](SCRIPTED_BASELINE.md).

## Example: query / results workflow

```bash
# 1. Create run (Unity usually does this)
curl -X POST http://127.0.0.1:8000/api/v1/runs \
  -H 'Content-Type: application/json' \
  -d '{"experiment_name":"ppo_vs_scripted","random_seed":42,
       "configuration":{"policy_herbivore":"learned_ppo","policy_predator":"scripted_baseline"}}'

# 2. After Unity has uploaded, list generation trait means
curl "http://127.0.0.1:8000/api/v1/runs/$RUN/trait-evolution?trait=base_movement_speed"

# 3. Graph-ready population points
curl "http://127.0.0.1:8000/api/v1/runs/$RUN/population-series"
```

`population-series.points[].simulation_time` vs `herbivore_population` / `predator_population` / `total_alive` is the ecosystem graph. `trait-evolution.points[].generation` vs `mean` / `variance` is the evolution graph.

## Unity components

| Type | Role |
|------|------|
| `AnalyticsSnapshotBuilder` | Pure snapshot math |
| `CreatureLifetimeFactory` | Pure lifetime records |
| `GenerationAggregator` / `TraitStatistics` | Pure generation stats (empty-safe) |
| `PolicyClassifier` | Enum ↔ `scripted_baseline` / `learned_ppo` |
| `PopulationStatisticCollector` | Interval snapshot |
| `CreatureLifetimeRecorder` | Subscribes to `CreatureLifecycleHub` |
| `GenerationAnalyticsCollector` | Upload-shaped generation rows |
| `ExperimentSession` | Creates the backend run |
| `StatsExportLoop` | Interval capture + bounded retry queues |
| `AnalyticsExportController` | Pure pending-queue / in-flight / overflow logic |
| `BackendClient` | Existing HTTP client (v1 + extended) |

Analytics assembly references **Common + Simulation** only (plus JSON). It does not reference Creatures, Genetics, or AI.

## Limitations

- Founders are generation 0. Offspring receive `generation = max(parents) + 1` and parent ids from `CreatureSpawner` / `ReproductionSystem`.
- Experiment metadata includes `ecosystem_mode` (`persistent_ecosystem` vs `training_support`) and `training_respawn_enabled` so analytics can distinguish persistent ecosystems from training-support respawn.
- Food, water, and kills are not recorded until those quantities are exposed on a read-only contract.
- Episode return exists only when ML-Agents is present and a PPO episode has completed.
- Plant census and active events are readable via `IReadOnlyEnvironmentState` but are not uploaded on v1 `/stats` yet.
- Unique snapshot time per run: do not post two snapshots at the exact same `simulation_time`.
- Existing local SQLite files gain `policy_kind` via a lightweight `ALTER TABLE` on startup.
- Bounded pending queues can drop the oldest unsent records if the backend stays down long enough to overflow.
