# Environment

Environment owns world resources, logical biomes, and the simulation-time day/night cycle. It does **not** own creature biology, population counts, or AI policy.

Related: [ARCHITECTURE.md](ARCHITECTURE.md), [AGENT_BOUNDARIES.md](AGENT_BOUNDARIES.md), [ENVIRONMENT_EVENTS.md](ENVIRONMENT_EVENTS.md), [AI_ML_AGENTS.md](AI_ML_AGENTS.md), [ANALYTICS.md](ANALYTICS.md).

## Authority

```
SimulationClock / SimulationRunner ticks
        │
        ├─ DayNightManager          (sim-time cycle, optional lighting hook)
        ├─ ResourceManager          (seeded placement once, then in-place regen)
        │       ├─ PlantResource / PlantStock
        │       ├─ WaterSource / WaterStock
        │       ├─ ResourceRegistry (spatial query + census)
        │       └─ BiomeMap         (logical zones)
        └─ EnvironmentalEventManager
                ├─ IEnvironmentEffectHost     → ResourceManager
                ├─ IEnvironmentalVitalEffects → Simulation EnvironmentalCreatureBridge
                └─ IEnvironmentalPopulationCommands → CreatureSpawner / CreatureVitals.Die
```

AI reads resources through `ResourceRegistry` / `IResourceProximitySensor`. Eating and drinking still go through `IResourceNode.TryConsume` plus Creatures `ConsumeFood` / `Drink`. Environment never calls those creature APIs.

## Plants

`PlantResource` wraps `PlantStock` and implements `IResourceNode` + `ISimulationTickable`.

| Field | Meaning |
|-------|---------|
| Capacity | Maximum stored food |
| Remaining / `AvailableAmount` | Current food |
| Depleted | Remaining ≤ 0; `ResourceRegistry.FindNearest` skips it |
| Regen rate | Food per simulation second, after biome × event multipliers |
| Regen delay | Wait after depletion before refill starts |
| Spawn density | Plants per square unit, used by `ResourceManager` at placement time |

Rules:

- Plants **regenerate in place**. They are not instantiated every frame.
- `TryConsume(0)` and negative requests take nothing.
- Optional `ResourceManager` places plants once from a seed + density, then ticks existing nodes.
- Placement is deterministic for the same seed, radius, density, and biome list.

Read-only statistics: `IReadOnlyResourceCensus` (`ResourceCensus` / `ResourceManager.CaptureCensus`) exposes plant count, density, remaining food, capacity, and abundance (remaining / capacity).

## Water

`WaterSource` wraps `WaterStock` and uses the same `IResourceNode` contract, so it is locally detectable via `ResourceRegistry.FindNearest(..., ResourceKind.Water)`.

Default: **infinite** source. Drinking does not remove the node.

Optional experiment mode: finite capacity + recharge rate. Drought can scale recharge through the event modifier stack. Infinite sources still do not disappear.

## Biomes / zones

Logical circular zones, not authored terrain or NavMesh:

| `BiomeKind` | Typical regen | Typical plant density | Temperature bias |
|-------------|---------------|----------------------|------------------|
| Grassland | 1.0 | medium | mid |
| Forest | 1.25 | high | slightly cooler |
| Wetland | 1.1 | lower (water-focused) | cooler |
| Rocky | 0.45 | low | hotter / drier |

`BiomeMap.ResolveKind(position)` uses the first containing zone, else the default grassland. Zones may change plant spawn density, regen multipliers, temperature pressure, and which plants an event affects.

There is no procedural mesh, ShaderGraph, or final art in this module.

## Day / night

`DayNightManager` / `DayNightCycle` are Environment-owned and advance from **simulation tick delta**, never `DateTime` or wall-clock `Time.realtimeSinceStartup`.

Exposed on `IReadOnlyDayNightState`:

- `NormalizedTimeOfDay` in [0, 1)
- `DayDurationSeconds` (configurable)
- `IsDay` / `IsNight` / `Phase`

Optional `IDayNightLightingHook` sinks can react to updates. No lighting implementation is required.

## Observation seam (PPO schema unchanged)

**CreatureObservationSchema v2 remains size 31.** Time of day is not appended to the training vector.

`EvoLife.AI.EnvironmentObservationSource` is an optional 2-float contextual source (`time_of_day`, `temperature`). It is **not** wired into `CompositeObservationSource`. To add it to PPO later: bump the schema version, update Training YAML, and update all observation tests.

## Analytics contracts

`IReadOnlyEnvironmentState` aggregates:

- day/night
- resource census (count, density, abundance)
- active events (kind, start, end)
- normalized temperature

Analytics may read these contracts later. They are not posted on the v1 `/stats` snapshot in this change.

## Configuration

Create assets:

- **EvoLife → Environment → Config** (`EnvironmentConfig`) — plant spawn, day length, water count, zones
- **EvoLife → Environment → Event Config** (`EnvironmentalEventConfig`) — event definitions + schedule

Wire in the scene: `ResourceRegistry`, `ResourceManager`, `DayNightManager`, `EnvironmentalEventManager`, plus Simulation `EnvironmentalCreatureBridge`. Add those tickables to `SimulationRunner`. `EcosystemManager` binds the creature bridge when present.

During **orchestrated experiments**, `ExperimentOrchestrator.InitializeEnvironment()` is the single caller of `ExperimentEnvironmentApplicator.Apply()`. That applies plant/day-night/event knobs; `ResourceManager.EnsurePlaced()` then places resources once. `EcosystemManager.ApplyExperimentSettings()` does not re-apply that configuration. Standalone/demo scenes without an orchestrator use `EcosystemManager.ApplyStandaloneEnvironment()` instead. See [EXPERIMENTS.md](EXPERIMENTS.md).

## Tests

EditMode: `PlantResourceTests`, `WaterSourceTests`, `DayNightCycleTests`, `BiomeMapTests`, `ResourceManagerTests`, `EnvironmentalEventTests`, `ExperimentLifecycleTests` (orchestrated environment applied once), plus `CreatureObservationSchemaTests.EnvironmentObservationSource_IsNotPartOfPpoSchema`.
