# Scripted Baseline Policy

The scripted baseline is a **credible non-learning survival controller**. It is the experimental control group for PPO, not an optimal ecosystem policy.

Related: [ARCHITECTURE.md](ARCHITECTURE.md), [AGENT_BOUNDARIES.md](AGENT_BOUNDARIES.md), [AI_ML_AGENTS.md](AI_ML_AGENTS.md), [ANALYTICS.md](ANALYTICS.md), [GENETICS.md](GENETICS.md).

## Purpose

1. Functional creatures before PPO training succeeds
2. A benchmark for learned policies
3. A way to validate simulation mechanics (hunger, thirst, sensing, locomotion, consumption)
4. Controlled scripted-vs-learned experiments

It does **not** claim to be the best possible heuristic. Thresholds are experimental defaults. This document does not claim PPO is better than the baseline.

## Architecture

The baseline lives in **AI**. It replaces the previous placeholder `ScriptedBaselinePolicy`; there is no second competing framework.

```
IObservationSource  (CreatureObservationSchema v2, same sensors as PPO)
        │
        v
BaselineSensedWorld          parsed local snapshot
        │
        v
BaselineMotiveEvaluator      pure priority / utility + hysteresis
        │
        v
BaselineSteering             local forward + turn + sprint/effort
        │
        v
IActionExecutor (PlanarMoveActionExecutor + LocalCreatureInteractor)
```

Preserved contracts:

- `ICreaturePolicy`
- `CreatureBrain` exclusive control (`AgentPolicyKind`, `CreatureControlMode`)
- `ScriptedBaselinePolicy` as the scripted implementation

Decision logic is **not** in `CreatureVitals` or `SimulationRunner`.

The baseline does **not** call `ICreatureInteractor` directly. Eat/drink/attack/rest/reproduce_request are requested as CreatureActionSchema v2 discrete actions and execute through the same canonical path as PPO.

Core types:

| Type | Role |
|------|------|
| `ScriptedBaselinePolicy` | `ICreaturePolicy` glue; reuses observation + action schemas |
| `BaselineMotiveEvaluator` | Pure motive selection (unit-tested) |
| `BaselineSensedWorld` | Observation parse; missing slots are zeros |
| `BaselineSteering` | Local target direction → forward / turn / sprint |
| `BaselineMemory` | Target stickiness, wander heading, chase timers |
| `ScriptedBaselineSettings` / `ScriptedBaselineProfile` | Thresholds (not genetics) |
| `LocalCreatureInteractor` | Shared local `TryConsume` + Creatures APIs (wired on the executor) |

## Sensory inputs

The baseline reads the **same** `CreatureObservationSchema` v2 vector as PPO (size **31**):

| Block | Indices | Used for |
|-------|---------|----------|
| Vitals | 0–4 | Hunger, thirst, energy, health (normalized by per-creature maxima) |
| Role | 5 | Herbivore vs predator policy branch |
| Genetics | 6–14 | Present in the vector; **not** decoded into extra omniscient range |
| Food | 15–18 | Local plant direction / distance / presence |
| Water | 19–22 | Local water direction / distance / presence |
| Nearest herbivore | 23–26 | Independent prey channel |
| Nearest predator | 27–30 | Independent threat channel |

A nearby herbivore cannot hide a predator from a herbivore. A nearby predator cannot hide prey from a predator.

Sensors are the Agent 4 contracts:

- `ResourceRegistryProximitySensor` → `ResourceRegistry.FindNearest` within sense range
- `PhysicsCreatureProximitySensor` → one physics overlap within sense range, both roles

Sense range = `CreatureObservationSchema.DefaultSenseRange` (12) × `CreatureCapabilityMotor.SensoryRangeMultiplier`. Genetic `vision_range` therefore changes what the baseline can perceive, the same way it changes PPO observations.

The baseline does **not**:

- Read `PopulationTracker` or other global population lists
- Teleport to food, water, or prey
- See depleted resource nodes (`FindNearest` already skips them)
- Invent targets when presence flags are 0
- Use a privileged interaction bypass unavailable to PPO

## Action outputs (CreatureActionSchema v2)

PPO and the baseline share:

Continuous:

- `forward` [-1, 1] — local forward
- `turn` [-1, 1] — yaw
- `sprint_or_effort` [0, 1] — walk-to-sprint scale

Discrete interaction branch:

- none, eat, drink, attack, rest, reproduce_request

`reproduce_request` is reserved for a later reproduction system and is a no-op until that executor is attached.

`BaselineSteering` converts agent-local target directions into forward + turn. It does not output world X/Z locomotion.

## Decision priorities

Motives: `Flee`, `SeekWater`, `SeekFood`, `Hunt`, `Rest`, `Wander`.

### Herbivore

1. **Flee** if a sensed predator is inside `FleeDistance` (fraction of sense range). Overrides ordinary food/water seeking.
2. **Rest** if energy ≤ `CriticalEnergyThreshold` (cannot usefully seek).
3. **Seek water** if thirst ≥ `ThirstSeekThreshold` and water is present.
4. **Seek food** if hunger ≥ `HungerSeekThreshold` and food is present.
5. If both food and water qualify, pick the **higher normalized need**, with stickiness so the choice does not flip every frame.
6. **Rest** if energy ≤ `RestEnergyThreshold` and nothing above applies.
7. **Wander / explore** otherwise (persistent heading, refreshed on `WanderUpdateIntervalSeconds`).

Missing or depleted resources are treated as absent (`present = 0`) and that seek motive is dropped immediately.

### Predator

1. **Rest** if energy is critical and prey is not already in attack range.
2. **Seek water** if thirst ≥ `UrgentThirstThreshold` and water is present — **overrides hunting**.
3. **Hunt** if a sensed herbivore is present and hunger ≥ `HungerSeekThreshold` (or prey is already in attack range).
4. **Seek water** at the ordinary thirst threshold when not hunting.
5. **Rest** at `RestEnergyThreshold` when idle.
6. **Wander** if no prey and no urgent need.

Predators do **not** graze plants. They do not hunt other predators. Dead/missing herbivores (`present = 0`) drop the hunt target immediately. A chase that stays near the edge of sense range for `ChaseAbandonSeconds` is abandoned, with a short hunt cooldown so the predator explores instead of re-locking the same unreachable target.

### Stability

- Food ↔ water switches use `MinMotiveHoldSeconds` and `MotiveStickiness`.
- Threats, lost targets, and leaving wander apply immediately.
- Standing on a resource (direction 0, presence 1) does not spin; locomotion is zero and an interact is requested.
- Interact requests are rate-limited (`InteractCooldownSeconds`) so plants are not drained every physics tick.
- Continuous actions are always clamped with `CreatureActionSchema`.
- Flee and hunt request sprint/effort = 1.

The evaluator is deterministic given the same observations, memory, settings, role, dt, and RNG seed.

## Configuration

Create **EvoLife → AI → Scripted Baseline Profile** (`ScriptedBaselineProfile`) and assign it on `CreatureBrain`. Different prefabs/species can use different profiles.

If no profile is assigned, `CreatureBrain` uses `ScriptedBaselineSettings.ForRole` (herbivore vs predator defaults). Optional inspector override: enable `useInlineBaselineSettings`.

| Setting | Meaning |
|---------|---------|
| `HungerSeekThreshold` | Normalized hunger that starts food seeking / hunting |
| `ThirstSeekThreshold` | Normalized thirst that starts water seeking |
| `UrgentThirstThreshold` | Predator thirst that overrides hunting |
| `RestEnergyThreshold` | Rest when otherwise idle |
| `CriticalEnergyThreshold` | Rest even if hungry/thirsty (unless fleeing / in attack range) |
| `FleeDistance` | Predator must be this close (0–1 of sense range) to trigger flee |
| `AttackDistance` / `InteractDistance` | Normalized ranges for attack / eat / drink (shared with PPO via the interactor) |
| `ChaseAbandonDistance` / `ChaseAbandonSeconds` | Give up inefficient pursuits |
| `MotiveStickiness` / `MinMotiveHoldSeconds` | Damp food/water oscillation |
| `WanderUpdateIntervalSeconds` | How often wander heading is resampled |
| `SeekMoveScale` / `WanderMoveScale` / `FleeMoveScale` | Locomotion magnitudes (still clamped) |
| `FoodConsumeRequest` / `FoodEnergyGain` / `DrinkRequest` / `AttackDamage` | Passed to owner APIs |

Do **not** put gene values in these assets. Genetics change capabilities through phenotype (`CreatureCapabilityMotor`, vitals maxima, sensory range).

## How genetics / phenotype interact

| Trait / phenotype | Effect on baseline |
|-------------------|--------------------|
| `vision_range` / sensory multiplier | Sense radius shared with PPO; unseen targets cannot be selected |
| Movement / sprint multipliers | Speed via `PlanarMoveActionExecutor` / motor, not baseline settings |
| Metabolism / max energy / max age | Change vitals over time; the heuristic reacts to normalized vitals |
| Aggression, body size, reproduction | Observed in the vector; not currently special-cased by the heuristic |

## How to select ScriptedBaseline

Per creature:

1. `CreatureBrain.policyKind = ScriptedBaseline` (default)
2. Or `CreatureBrain.SetPolicyKind(AgentPolicyKind.ScriptedBaseline)`
3. `CreatureSpawner.Spawn(..., policyKind: AgentPolicyKind.ScriptedBaseline)`

Per experiment:

- `SimulationConfig.HerbivorePolicy` / `PredatorPolicy`

`CreatureBrain` then runs `ScriptedBaselinePolicy` in `FixedUpdate` and **disables** `EvoLifeCreatureAgent`. Scripted and PPO never share a creature.

Analytics records `policy_kind = scripted_baseline` via `IPolicyKindOwner` / `PolicyClassifier`. Snapshots include `scriptedAlive` vs `ppoAlive`.

## Comparing ScriptedBaseline vs LearnedPpo

This is a control-group comparison, not a proof that either policy is optimal.

1. Create two `SimulationConfig` assets (or one mixed config):
   - all scripted
   - all PPO
   - mixed (for example herbivores PPO, predators scripted)
2. Use a distinct `experimentName`. Share `randomSeed` if you want paired worlds. Set `trainingModelId` on PPO runs.
3. Scripted creatures: `AgentPolicyKind.ScriptedBaseline`. PPO creatures: `LearnedPpo` plus `EvoLifeCreatureAgent` (see [AI_ML_AGENTS.md](AI_ML_AGENTS.md)).
4. Start the analytics backend, enable `ExperimentSession.createRunOnStart` and `StatsExportLoop.uploadToBackend`.
5. Run each scenario for the same simulation-time budget.
6. Query (see [ANALYTICS.md](ANALYTICS.md)):

```bash
curl -s http://127.0.0.1:8000/api/v1/runs/$RUN/policy-comparison
curl -s "http://127.0.0.1:8000/api/v1/runs/$RUN/survival?policy_kind=scripted_baseline"
curl -s "http://127.0.0.1:8000/api/v1/runs/$RUN/survival?policy_kind=learned_ppo"
curl -s http://127.0.0.1:8000/api/v1/runs/$RUN/population-series
```

Compare mean lifetime, death-cause histograms, and population `scripted_alive` / `ppo_alive`. Episode return exists only for PPO.

Both policies now have the same sensory channels and the same interaction capabilities.

## Tests

EditMode:

- `BaselineMotiveEvaluatorTests` — herbivore/predator priorities, oscillation, dropped targets, determinism, no spin, independent role channels
- `ScriptedBaselinePolicyTests` — legal actions, null/missing targets, no direct vital mutation, canonical action-path interactions, no privileged interactor field

Decision selection is pure (`BaselineMotiveEvaluator`). The policy never casts `IReadOnlyVitalState` to mutate fields.

## Limitations

- Heuristic, not tuned, not claimed optimal
- No pathfinding; steering is greedy along sensor directions
- Nearby sensing still needs colliders (same as PPO)
- `reproduce_request` is reserved and currently a no-op
- No dedicated “unreachable” map test beyond depleted/absent sensors and chase abandonment
- Rest recovery is applied by biology when `CurrentActivity` is `Resting`, not by writing energy fields
