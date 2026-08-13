# EvoLife ML-Agents Integration

This document describes the AI module’s Unity ML-Agents PPO wiring. Rewards, observations, and actions live in **AI**. Creature vitals and genomes stay in **Creatures** / **Genetics**.

Related: [ARCHITECTURE.md](ARCHITECTURE.md), [AGENT_BOUNDARIES.md](AGENT_BOUNDARIES.md), [Training/README.md](../Training/README.md).

## Agent class

`EvoLife.AI.EvoLifeCreatureAgent` is the ML-Agents `Agent` subclass (compiled when `com.unity.ml-agents` ≥ 2.0.0 defines `EVOLIFE_MLAGENTS`).

It bridges:

```
ML-Agents Academy
    → CollectObservations (CreatureObservationSchema)
    → OnActionReceived (CreatureActionSchema)
    → IActionExecutor (PlanarMoveActionExecutor)
    → IEpisodeRewardCalculator (TrainingRewardCalculator)
    → existing CreatureVitals / Environment query APIs
```

It does **not** mutate `CreatureVitals` fields. Locomotion uses `IActionExecutor`. Activity is set through the existing `CreatureVitals.CurrentActivity` API so biology can account for walking.

`PpoPolicyAdapter` is **not** a second PPO trainer. It is an idle fallback when the Agent component or ML-Agents package is missing, so a `LearnedPpo` creature never also runs the scripted baseline.

## Behavior names

| Role | Constant | YAML / Behavior Parameters |
|------|----------|----------------------------|
| Herbivore | `MlAgentsBehaviorNames.Herbivore` | `EvoLifeHerbivore` |
| Predator | `MlAgentsBehaviorNames.Predator` | `EvoLifePredator` |

`EvoLifeCreatureAgent.Initialize` writes these onto `BehaviorParameters` from `CreatureIdentity.Role`. Set the prefab identity role to match the intended behavior **before** Play, because ML-Agents registers the name at agent initialization.

## Observation vector (exact order)

Contract: `CreatureObservationSchema` (version **1**, size **28**). Tests fail if `Names.Length`, genetics trait count, or indices drift.

| Index | Name | Range | Source |
|------:|------|-------|--------|
| 0 | `health` | [0,1] | `IReadOnlyVitalState` / max health |
| 1 | `hunger` | [0,1] | vitals / `MaxHunger` (not a hard-coded 100) |
| 2 | `thirst` | [0,1] | vitals / `MaxThirst` |
| 3 | `energy` | [0,1] | vitals / `MaxEnergy` |
| 4 | `age` | [0,1] | vitals / `MaxAge` |
| 5 | `role` | 0 or 1 | `ICreatureIdentity` (0 herbivore, 1 predator) |
| 6–14 | `gene_*` | [0,1] | `GeneticObservationProvider` / CanonicalGenomeSchema v1 order |
| 15 | `food_dir_x` | [-1,1] | nearest plant, agent-local X |
| 16 | `food_dir_z` | [-1,1] | nearest plant, agent-local Z |
| 17 | `food_distance` | [0,1] | distance / sense range |
| 18 | `food_present` | 0 or 1 | 1 if a non-depleted plant was found |
| 19 | `water_dir_x` | [-1,1] | nearest water |
| 20 | `water_dir_z` | [-1,1] | |
| 21 | `water_distance` | [0,1] | |
| 22 | `water_present` | 0 or 1 | |
| 23 | `nearby_dir_x` | [-1,1] | nearest other `CreatureIdentity` |
| 24 | `nearby_dir_z` | [-1,1] | |
| 25 | `nearby_distance` | [0,1] | |
| 26 | `nearby_role` | 0 or 1 | other creature role |
| 27 | `nearby_present` | 0 or 1 | |

Missing optional sensors (no `ResourceRegistry`, no physics colliders, null genome) write **zeros** for their block. Presence=0 with distance=0 means “nothing sensed”, not “standing on the target”.

Sense range defaults to 12 (canonical `vision_range` default) times `CreatureCapabilityMotor.SensoryRangeMultiplier`.

`VitalObservationSource` remains a 5-float vitals-only building block for tests and simpler consumers. Training uses `CompositeObservationSource`.

Genetics are **read-only** via `GeneticObservationProvider`. AI never calls crossover or mutation.

## Action vector (exact order)

Contract: `CreatureActionSchema` (version **1**, 2 continuous actions). Incoming values are clamped to [-1, 1].

| Index | Name | Meaning |
|------:|------|---------|
| 0 | `move_x` | Horizontal / strafe |
| 1 | `move_z` | Forward / back |

Executed by `PlanarMoveActionExecutor`. Speed comes from `CreatureCapabilityMotor.MaxSpeed` when present.

Eat / drink / attack / rest are **not** in the action space. Those simulation mechanics are not yet exposed as dedicated AI executors. Do not invent fake interaction buttons.

## Reward components

Owned by AI: `TrainingRewardCalculator` + `TrainingRewardSettings` (serialized on `CreatureBrain` / `EvoLifeCreatureAgent`).

Starter signals (experimental, not tuned):

| Signal | Default | Notes |
|--------|---------|-------|
| Alive | `+0.001` / step | Small survival bonus |
| Energy maintenance | `+0.0005 * energy[0,1]` | Tiny |
| Hunger relief | `+0.4 * Δhunger` | When hunger ratio drops |
| Thirst relief | `+0.4 * Δthirst` | When thirst ratio drops |
| Health loss | `-0.2 * Δhealth` | When health ratio drops |
| Critical need | `-0.004` | If hunger or thirst ratio ≥ 0.85 |
| Death | `-1.0` and **end episode** | |

Critical-need penalty keeps a stationary starving agent from farming the alive bonus. Sitting still still leads to death because biology continues to drain needs.

`SurvivalRewardCalculator` remains as a simpler stub used by existing tests.

## Episode termination

| Event | Behavior |
|-------|----------|
| Creature death | Death penalty; `EndEpisode()`. Does not reset the whole ecosystem. |
| `Agent.MaxStep` (default 5000) | ML-Agents time-limit end. |
| Experimental local reset | `resetLocalPoseOnEpisodeBegin` / `reinitializeVitalsOnEpisodeBegin` on the Agent — **off** by default. Local pose/vitals only. |

Multi-agent / persistent ecosystem training remains possible: each Agent ends its own episode; nothing here calls a global world reset.

## Policy selection

`CreatureBrain` owns exclusive control:

| `AgentPolicyKind` | `CreatureControlMode` | Who drives locomotion |
|-------------------|-----------------------|------------------------|
| `ScriptedBaseline` | `ScriptedBaseline` | `ScriptedBaselinePolicy` via `FixedUpdate`; Agent disabled |
| `LearnedPpo` | `LearnedPpo` | `EvoLifeCreatureAgent` only |
| `LearnedPpo` without Agent/package | `PpoFallbackIdle` | `PpoPolicyAdapter` idle move; **not** the scripted heuristic |

Switch at runtime: `CreatureBrain.SetPolicyKind`. Simulation can pass `AgentPolicyKind` into `CreatureSpawner.Spawn` via `IPolicyKindOwner` (Common), without Simulation referencing the AI assembly.

`SimulationConfig.HerbivorePolicy` / `PredatorPolicy` remain the experiment-level defaults.

## PPO config

| File | Behaviors |
|------|-----------|
| `Training/configs/herbivore_ppo.yaml` | `EvoLifeHerbivore` |
| `Training/configs/predator_ppo.yaml` | `EvoLifePredator` |
| `Training/configs/evolife_ppo.yaml` | both |

Starter trainer values: PPO, 128 hidden units × 2 layers, batch 1024, buffer 10240, lr `3e-4`, γ `0.99`, λ `0.95`, β `5e-3`, time horizon 64, max steps 1e6, checkpoint every 50k. **Not claimed optimal.**

## Training command

See [Training/README.md](../Training/README.md):

```bash
./Training/scripts/train_herbivore.sh
# Unity: CreatureBrain = LearnedPpo, press Play
```

## Prefab checklist

On each trainable creature:

1. `CreatureIdentity`, `CreatureVitals`, `CreatureGenome`, `CreatureCapabilityMotor`
2. `CreatureBrain` + `PlanarMoveActionExecutor`
3. `EvoLifeCreatureAgent` (Behavior Parameters added by ML-Agents; Decision Requester is added when PPO control is enabled)
4. Collider if nearby-creature observations should be non-zero
5. Scene `ResourceRegistry` plus `PlantResource` / `WaterSource` (they register on enable)

## Experimental / tunable settings

Do not treat these as production values:

- All `TrainingRewardSettings` fields
- `EvoLifeCreatureAgent.maxEpisodeSteps`
- Local pose / vitals reset flags
- PPO YAML hyperparameters
- `PlanarMoveActionExecutor` fallback `moveSpeed` (phenotype motor speed is preferred)

## Known limitations

- Locomotion-only actions: agents cannot eat/drink/attack until interaction executors exist.
- Nearby-creature sensing needs colliders; otherwise those five slots stay zero.
- No trained ONNX is shipped. Do not commit model binaries unless required.
- Unity Editor is required to compile/run ML-Agents PlayMode tests.
