# Reproduction & Ecosystem Lifecycle

Simulation owns mating, birth, population caps, founder spawning, and optional training respawn. AI only **requests** reproduction. Genetics only supplies crossover, mutation, and clamping. Creatures only expose vitals APIs.

Related: [ARCHITECTURE.md](ARCHITECTURE.md), [AGENT_BOUNDARIES.md](AGENT_BOUNDARIES.md), [GENETICS.md](GENETICS.md), [AI_ML_AGENTS.md](AI_ML_AGENTS.md), [SCRIPTED_BASELINE.md](SCRIPTED_BASELINE.md), [ANALYTICS.md](ANALYTICS.md), [ENVIRONMENT.md](ENVIRONMENT.md).

## Authority

```
Policy (scripted or PPO)
    → CreatureActionSchema.reproduce_request
    → IReproductionRequestHandler (Common)
    → CreatureReproductionBridge (Simulation, on the creature)
    → ReproductionSystem.TryReproduce
            ├─ ReproductionEligibility (alive, mature, health, energy, cooldown)
            ├─ local mate search (same species, same role, in mateRange)
            ├─ species population cap
            ├─ CreatureVitals.ConsumeEnergy / optional ApplyDamage
            ├─ OffspringComposer → IGeneticOperators.Crossover + Mutate
            └─ CreatureSpawner.Spawn (new CreatureId, lineage, genome)
                    ├─ PopulationTracker.Register
                    └─ CreatureLifecycleHub.RegisterSpawned
```

- A request with no eligible local mate is a **safe no-op**.
- One request creates **at most one** offspring.
- Reproduction does **not** run every `FixedUpdate`. It runs only when a policy requests it, and biological cooldown then blocks repeats.
- Mating logic is **not** inside `CreatureVitals` / `CreatureBiology`. Costs go through public Creatures APIs.

## Mating conditions

Both the requester and the chosen mate must satisfy all of:

| Condition | Source |
|-----------|--------|
| Alive | `IReadOnlyVitalState.IsAlive` |
| Mature | `Age >= MaturityAgeSeconds` and optional `Age / MaxAge >= MinAgeFraction` |
| Healthy enough | `Health / MaxHealth >= MinHealthRatio` |
| Energy threshold | `Energy / MaxEnergy >= genome reproduction_threshold` (canonical trait, else phenotype × default) |
| Can pay the cost | `Energy >= EnergyCost` |
| Cooldown ready | no prior birth, or `now - lastReproduction >= CooldownSeconds` |
| Compatible mate in range | same `SpeciesId`, same `CreatureRole`, planar distance `<= MateRange`, mate also eligible |
| Population cap | living count for that role `< MaxHerbivores` / `MaxPredators` (cap `<= 0` means unlimited) |

`reproduction_threshold` is the genome trait “fraction of max energy required to reproduce” (`CanonicalGenomeSchema` / `TraitId.ReproductionThreshold`). Simulation reads the genome (or the phenotype multiplier × trait default). It does not invent a fitness score.

Incompatible species, dead partners, underage partners, and mates outside `MateRange` fail locally. There is no global instant mating.

## Offspring

On success:

1. Parent genomes are selected (requester = parent A, nearest eligible mate = parent B).
2. Canonical Genetics `Crossover` then `Mutate` run (`DefaultGeneticOperators.CreateOffspring` / configured `GeneticsConfig`).
3. Traits are clamped by `CanonicalGenomeSchema` hard bounds.
4. `CreatureSpawner` assigns a new `CreatureId`.
5. `generation = max(parent generations) + 1`.
6. `ParentA` / `ParentB` are stored on `CreatureIdentity` (`ICreatureLineage`).
7. Species id and role are copied from the parents.
8. Policy kind is inherited from the requester (`IPolicyKindOwner`), else the experiment role default.
9. Spawn goes through `CreatureSpawner` → `PopulationTracker` + `CreatureLifecycleHub`.
10. Analytics observes lineage from those existing contracts.

Founders are generation **0** with no parent ids.

Evolution is survival plus successful reproduction. There is no hand-authored fitness score.

## Reproduction costs

Configured on `ReproductionSettings` / `ReproductionConfig`:

| Cost | API | Default |
|------|-----|---------|
| Energy | `CreatureVitals.ConsumeEnergy` | `15` |
| Cooldown | Simulation timestamp map (not vitals) | `12s` |
| Optional health | `CreatureVitals.ApplyDamage` only if `HealthCost > 0` | `0` |

Simulation never writes private `CreatureBiology` fields.

## Population management

Keep these separate:

| Concern | Type |
|---------|------|
| Mating eligibility + executor | `ReproductionSystem`, `ReproductionEligibility`, `OffspringComposer` |
| Reproduction parameters | `ReproductionSettings` / `ReproductionConfig` |
| Caps, mode, species ids, spawn radius | `EcosystemSettings` on `SimulationConfig` |
| Founder generation 0 placement | `InitialPopulationSpawner` |
| Spawn/death fan-out | `CreatureLifecycleHub` + `PopulationTracker` |
| Training-only refill | `TrainingRespawnController` |
| Thin wiring + extinction report | `EcosystemManager` |

`EcosystemManager` is not a god object. It applies experiment settings, optionally spawns founders, and exposes `CurrentExtinction` from `ExtinctionEvaluator`.

Extinction states: none, herbivores extinct, predators extinct, ecosystem extinct. Derived from alive counts only.

## Training vs persistent ecosystem

| Mode | Wire name | Death | Reproduction | Respawn |
|------|-----------|-------|--------------|---------|
| `Persistent` | `persistent_ecosystem` | Permanent | Creates new generations | Never, even if the respawn flag is set |
| `TrainingSupport` | `training_support` | Creature still dies | Same mating rules | Optional controlled founder respawn when a role is below its floor |

Respawn is **not** hidden in biology. `CreatureVitals.Reinitialize` remains a local PPO episode reset. `TrainingRespawnController` is a Simulation tickable that calls `CreatureSpawner` with generation 0 and no parents.

Analytics records `ecosystem_mode`, `training_respawn_enabled`, `max_herbivores`, and `max_predators` on the run configuration.

## AI request vs Simulation authority

PPO and the scripted baseline share CreatureActionSchema v2. Discrete value `5` (`reproduce_request`) is forwarded by `PlanarMoveActionExecutor` → `LocalCreatureInteractor.RequestReproduce` → `IReproductionRequestHandler`.

Both policies use the **same** eligibility and executor. The baseline may emit the request when idle, energy is high, and a same-role neighbor is in interact range. That is a heuristic **request**, not a mating bypass. PPO is not taught mating rules; it can learn to emit the same action.

Invalid or untimely requests do not mutate biology.

## Configuration

Create **EvoLife → Simulation → Reproduction Config** and assign it on `ReproductionSystem` / `EcosystemManager`.

`ExperimentConfiguration` can override mutation probability/magnitude for a run; `EcosystemManager.ApplyExperimentSettings` copies those onto `ReproductionSettings` and reseeds the reproduction RNG from `DeterministicSeeds.Reproduction`. See [EXPERIMENTS.md](EXPERIMENTS.md).

`SimulationConfig.Ecosystem` holds mode, caps, training floors, spawn radius, and species ids. Prefabs stay on `EcosystemManager` / `ReproductionSystem`.

Add `TrainingRespawnController` to `SimulationRunner`’s tick list when using training-support mode.

Creature prefabs should include `CreatureReproductionBridge` (or allow spawn to add it) so AI can discover the Common request handler after spawn wiring.

## Tests

EditMode: `ReproductionTests`, `EcosystemLifecycleTests` (eligible pair, underage / low-energy / dead / incompatible species, cooldown, generation, parent ids, crossover, zero-mutation stability, mutation bounds, seeded determinism, population tracker, no-mate no-op, single birth per request, training vs persistent respawn).
