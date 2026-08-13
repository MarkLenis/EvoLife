# Environmental Events

Ecological events are data/config driven. `EnvironmentalEventManager` (Environment) applies resource effects through Environment APIs and creature effects through Simulation ports. It does **not** own `CreatureBiology` and does **not** instantiate or destroy creatures itself.

Related: [ENVIRONMENT.md](ENVIRONMENT.md), [ARCHITECTURE.md](ARCHITECTURE.md), [AGENT_BOUNDARIES.md](AGENT_BOUNDARIES.md), [REPRODUCTION.md](REPRODUCTION.md).

## Authority

```
EnvironmentalEventConfig (definitions + schedule + seed)
        │
        v
EnvironmentalEventManager.Tick(sim dt)  or  Trigger(kind)
        │
        ├─ IEnvironmentEffectHost (ResourceManager)
        │     regen multipliers, plant availability, depletion, water recharge, temperature
        ├─ IEnvironmentalVitalEffects (Simulation EnvironmentalCreatureBridge)
        │     CreatureVitals.ApplyDamage(..., DeathCause.Environmental)
        └─ IEnvironmentalPopulationCommands (same bridge)
              SpawnRole → CreatureSpawner
              RemoveRole → CreatureVitals.Die → CreatureLifecycleHub
```

Hidden biology fields are never written. Lethal damage publishes death **once** (`CreatureBiology.Die` is idempotent). The event manager must not also call `Die` after `ApplyDamage`.

## Event kinds

| Kind | Wire name | Typical effect |
|------|-----------|----------------|
| Drought | `drought` | Plant regen × 0.25, water recharge × 0.25 |
| Wildfire | `wildfire` | Deplete plants, damage pulse + optional DPS |
| Heat wave | `heat_wave` | Temperature up, regen down, DPS |
| Food boom | `food_boom` | Add plant food, regen × 2 |
| Disease pressure | `disease_pressure` | DPS through vitals APIs |
| Predator introduction | `predator_introduction` | `SpawnRole(Predator, n)` |
| Predator removal | `predator_removal` | `RemoveRole(Predator, n)` via `Die` |

Defaults live on `EnvironmentalEventDefinition.Defaults`. Override them on `EnvironmentalEventConfig`.

## Triggers

- **Manual:** `EnvironmentalEventManager.Trigger(kind)` or `Trigger(definition)`
- **Scheduled:** `ScheduledEnvironmentalEvent` list on the config. `EnvironmentalEventScheduler` fires each entry once when simulation time reaches `AtSimulationTime`. Same schedule + same time steps are deterministic.
- **Duration:** `DurationSeconds > 0` stays in `ActiveEvents` until end; modifiers are removed on end. Instant events (`DurationSeconds == 0`) apply start effects, fire start then end, and do not remain active.
- **Start / end:** `EventStarted` / `EventEnded`. Query with `ActiveEvents` and `HasActiveEvent`.

Simulation time comes from ticks (and optional `ISimulationClock`). It is not wall-clock time.

## Resource modifiers

Active events push a keyed entry onto `EnvironmentModifierStack`:

- regen multiplier (product, optionally biome-filtered)
- water recharge multiplier
- temperature delta (sum, clamped to [0, 1] with biome bias)

On event end the key is removed and plants return to biome baseline regen. Food added by a boom is **not** clawed back; only the regen multiplier is restored.

Wildfire plant loss uses `DepleteByFraction` on existing nodes. New plant GameObjects are not spawned or destroyed for that effect.

## Creature effects

`EnvironmentalCreatureBridge` (Simulation):

- Copies live instances from `CreatureLifecycleHub` (not a second population registry)
- Calls `CreatureVitals.ApplyDamage` for pulses and DPS
- Spawns through `CreatureSpawner` (registers tracker + lifecycle hub)
- Removes through `CreatureVitals.Die(DeathCause.Environmental)` so the hub observes death

If the bridge is missing, resource events still run; damage/spawn/remove become no-ops.

## Analytics

Each `IReadOnlyEnvironmentalEvent` exposes kind, start time, end time, and active flag. `IReadOnlyEnvironmentState` includes the active list plus resource abundance. No dashboard is provided.

## Tests

`EnvironmentalEventTests` covers drought regen reduction, food-boom availability, modifier restore on end, wildfire single death, deterministic schedules, manager-not-owning-biology, and spawn/remove through lifecycle APIs.
