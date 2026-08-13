# EvoLife Experiments

This document describes **evaluation experiments**: fixed, serializable configurations run through `ExperimentOrchestrator`, with analytics metadata recorded for later comparison.

Related: [TRAINING_CURRICULUM.md](TRAINING_CURRICULUM.md), [ANALYTICS.md](ANALYTICS.md), [SCRIPTED_BASELINE.md](SCRIPTED_BASELINE.md), [AI_ML_AGENTS.md](AI_ML_AGENTS.md), [ENVIRONMENT_EVENTS.md](ENVIRONMENT_EVENTS.md).

## Training vs evaluation vs presentation

Keep these modes separate. A PPO training session is not an experiment result, and a demo scene is not a controlled run.

| Mode | Purpose | Typical setup |
|------|---------|----------------|
| **Training** | Collect PPO experience and write checkpoints | `TrainingSupport` + respawn, `LearnedPpo`, curriculum stage, `mlagents-learn`. See [TRAINING_CURRICULUM.md](TRAINING_CURRICULUM.md) and [Training/README.md](../Training/README.md) |
| **Evaluation** | Compare policies or scenarios with a frozen config | Persistent (or fixed-time) run, `ExperimentOrchestrator`, analytics backend, no trainer connection |
| **Presentation / demo** | Show the sim to a person | HUD, camera, time scale for viewing. Do not treat demo numbers as experiment output |

Do not train a production model as part of an evaluation PR or demo pass. Checkpoints and `.onnx` files stay under `Training/results/` (gitignored).

## Configuration model

`ExperimentConfiguration` (Simulation) is the serializable document. `SimulationConfig` applies it onto the existing Unity wiring (`EcosystemSettings`, mutation fields, policies, seed).

JSON field names are snake_case so Unity, Python (`evolife.experiments`), and the analytics backend share one shape. Round-trip with `ExperimentConfigurationSerializer`.

Required knobs:

- experiment name, random seed
- initial herbivore / predator counts
- resource abundance, plant regeneration multiplier
- mutation probability / magnitude
- day length
- enabled environmental events + optional schedule
- herbivore / predator `AgentPolicyKind`
- population caps, training respawn mode, spawn radius
- scenario id, model id, optional curriculum stage id
- duration / stopping conditions (max sim time, extinction flags)

`ExperimentConfigurationValidator` rejects empty names, negative counts, mutation outside `[0, 1]`, unknown event kinds, invalid policies, and training respawn outside `training_support`.

Offline copy: `evolife.experiments.ExperimentConfiguration`. Starter JSON lives under `Training/experiments/`.

## Starter scenarios

`ExperimentScenarios` / `evolife.experiments.scenarios` only change configuration knobs. They do **not** claim expected population, survival, or learning outcomes.

| Id | What it changes |
|----|-----------------|
| `normal_control` | Default abundance, mutation, no extra events, persistent ecosystem |
| `reduced_food` | Lower plant abundance and regeneration |
| `drought` | Lower regen plus a scheduled drought event |
| `fast_predators` | Founder predator speed bias (genome `base_movement_speed` / `sprint_speed`) |
| `high_mutation` | Higher mutation probability and magnitude |
| `low_mutation` | Lower mutation probability and magnitude |
| `predator_pressure` | More initial predators and a higher predator cap |
| `recovery_after_event` | Drought followed by a food boom on a shorter time budget |

Create with `ExperimentScenarios.Create("drought")` or load the matching JSON.

## Runner

`ExperimentOrchestrator` is a thin lifecycle owner. It does **not** replace `SimulationRunner`, `EcosystemManager`, or `ExperimentSession`.

Pause/unpause authority lives on the orchestrator:

- `Awake`, `Load`, and initialization methods pause `SimulationClock`
- phases `Loaded`, `EnvironmentInitialized`, `PopulationInitialized`, and `AnalyticsStarted` stay paused
- **only** `BeginRunning()` transitions the coordinator to `Running` and unpauses the clock
- `FinishAsync` pauses again

Manual `Load` also pauses so a previous run cannot keep progressing.

### Initialization order

```
1. Pause SimulationClock
2. Validate / load ExperimentConfiguration
3. Apply onto SimulationConfig (deterministic seed streams included)
4. ExperimentEnvironmentApplicator.Apply  (resource knobs, day length, event schedule)
5. EcosystemManager.ApplyExperimentSettings  (clock scale, spawner, policy seeds,
   predator founder bias, reproduction, training respawn, environmental creature bridge,
   event Bind — not a second environment Apply)
6. ResourceManager.EnsurePlaced  (exactly once)
7. Spawn founders exactly once
8. IExperimentAnalyticsSession.BeginAsync  (ExperimentSession; still paused)
9. Mark AnalyticsStarted
10. BeginRunning: coordinator → Running, then unpause SimulationClock
        │
        v
Tick until ExperimentStopEvaluator says stop
  (max sim time / configured extinction / manual)
        │
        v
Pause clock + FinishAsync(status, stop_reason)
```

`ExperimentOrchestrator.InitializeEnvironment()` is the experiment-time caller of `ExperimentEnvironmentApplicator.Apply()`. `EcosystemManager.ApplyExperimentSettings()` configures population/lifecycle systems only.

Standalone/demo scenes without an orchestrator may still call `EcosystemManager.ApplyStandaloneEnvironment()` from `Start` when `ApplyEnvironmentOnStart` is true. The orchestrator disables that flag, `SpawnFoundersOnStart`, `ResourceManager.PlaceOnStart`, and `ExperimentSession` auto-create so Unity `Start` order cannot double-place resources or double-spawn founders.

`SimulationRunner` still only fans out ticks. A paused clock yields `DeltaTimeSeconds == 0`, so tickables do not advance during initialization (including async analytics startup).

### Analytics startup failure

If `IExperimentAnalyticsSession.BeginAsync` returns false or throws:

- the clock remains paused
- the coordinator does **not** enter `Running`
- the failure is logged
- the scene is consumed; reload before another run

A missing analytics session is optional (local tests). `ExperimentSession` without a backend client is local-dev success (`RunReady = false`) and may still enter `Running`. A backend create that was attempted and failed is a startup failure.

### Second BeginAsync / restart

This orchestrator does **not** reset the scene. Calling `BeginAsync` a second time (including after `Finished`, while `Running`/`Stopping`, or after a failed analytics start) is **rejected**. Reload the scene before another experiment run. There is no restart UI in this layer.

Do not stack a second founder population, resource placement, event schedule, or analytics run on top of the first.

## Reproducibility

One master `randomSeed` is split into independent streams (`DeterministicSeeds`):

| Stream | Used for |
|--------|----------|
| Founder genomes | `CreatureSpawner` / `CreateFounder` |
| Reproduction | crossover + mutation RNG |
| Resource spawn | `PlantSpawnSettings.Seed` |
| Event schedule | `EnvironmentalEventConfig.Seed` (schedule times are explicit) |
| Scripted wander | `IPolicySeedOwner` / `BaselineMotiveEvaluator` per creature id |
| Training respawn | `TrainingRespawnController` placement |
| Environmental creatures | event spawn/remove sampling |

The same config + seed should reproduce those **logical** RNG draws. Analytics stores the derived seeds on the run (`seed_founder_genomes`, …).

### What may still be nondeterministic

Unity and physics are not a deterministic lockstep simulator. Even with fixed seeds you should expect drift from:

- `FixedUpdate` vs `Update` timing, variable `deltaTime`, and time scale
- PhysX / character controller collisions and penetration resolution
- `Instantiate` order interacting with Unity message order across machines
- Floating-point differences across CPU/OS
- ML-Agents inference threading (curriculum YAML sets `threaded: false`; Academy stepping still depends on the player loop)
- Any unseeded `UnityEngine.Random` use outside these streams
- Wall-clock export intervals in Analytics (sim timestamps are what matter for comparison)

Treat paired runs as **matched configurations**, not bit-identical replays.

## Analytics metadata

`ExperimentRunMetadata` records name, seed, policies, counts, abundance/regen, mutation, day length, events, caps, respawn, scenario/model/stage ids, derived seeds, and stop reason.

`ExperimentSession` creates `POST /api/v1/runs` and finishes with `stop_reason` on `POST /api/v1/runs/{id}/finish`. Queries stay in [ANALYTICS.md](ANALYTICS.md).

## Suggested evaluation procedure

1. Start the FastAPI backend.
2. Choose a scenario JSON or `ExperimentScenarios.Create(...)`.
3. Set policies (`scripted_baseline` / `learned_ppo`) and optional `model_id`.
4. Play with `ExperimentOrchestrator` (not a live `mlagents-learn` session).
5. Stop on time or extinction; inspect `population-series`, `survival`, and `policy-comparison`.

Presentation/demo scenes can reuse the same prefabs with `DesktopDebugUi` and `DesktopCameraController` on the Agent 10 hooks in `EvoLifeDemo`. Do not mix demo time-scale scrubbing into evaluation logs. See [UI_DEBUG.md](UI_DEBUG.md) and [PRESENTATION.md](PRESENTATION.md).

## Tests

EditMode: `ExperimentConfigurationTests`, `ExperimentLifecycleTests` (JSON/seeds/scenarios/stop conditions, initialization pause, unpause only at Running, failed analytics never Running, FinishAsync pauses, second BeginAsync rejected, one environment placement, founders spawned once).
