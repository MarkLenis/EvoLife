# EvoLife Training Curriculum

This document describes **training** stages for herbivore, predator, and combined PPO. It is not an evaluation protocol and not a presentation/demo guide.

Related: [EXPERIMENTS.md](EXPERIMENTS.md), [AI_ML_AGENTS.md](AI_ML_AGENTS.md), [Training/README.md](../Training/README.md).

## Modes

| Mode | Use |
|------|-----|
| **Training** | `TrainingCurriculum` + `mlagents-learn`. Respawn is allowed. Checkpoints stay gitignored |
| **Evaluation** | Frozen weights, starter scenarios, analytics comparisons. See [EXPERIMENTS.md](EXPERIMENTS.md) |
| **Presentation / demo** | Watchable scene. Not a training run and not an experiment result |

These stages do **not** claim that a policy will reach any performance level. They only set lightweight scene/config knobs. Polished terrain is not required.

## Contracts (do not drift)

| Item | Value |
|------|-------|
| Herbivore behavior | `EvoLifeHerbivore` |
| Predator behavior | `EvoLifePredator` |
| Observations | `CreatureObservationSchema` v2, size **31** |
| Actions | 3 continuous (`forward`, `turn`, `sprint_or_effort`) + discrete branch size **6** |

YAML under `Training/configs/` must keep those names and sizes. Time-of-day is **not** in the v2 vector.

YAML must list only behaviors that are `LearnedPpo` in the scene. A scripted role will never connect, and the trainer will wait for it.

## Stages

`TrainingCurriculum.Create(stage, focus)` / `evolife.experiments.curriculum`.

Focus: `Herbivore` (PPO herbivores, scripted predators if present), `Predator` (the reverse), `Combined` (both PPO).

| Stage | Id | Intent | Typical knobs |
|------:|----|--------|----------------|
| 1 | `stage1_movement` | Movement / orientation | Few agents, abundant plants, no events, training respawn. Herbivore focus spawns no predators |
| 2 | `stage2_food_water` | Food / water acquisition | Moderate abundance, still no (or few) predators for herbivore focus |
| 3 | `stage3_predator_prey` | Predator / prey interaction | Both roles present, default abundance |
| 4 | `stage4_resource_scarcity` | Scarcer plants / slower regen | Same roles as stage 3, lower abundance |
| 5 | `stage5_persistent_ecosystem` | Persistent ecosystem | No training respawn; extinction can end an eval-style episode |
| 6 | `stage6_reproduction_events` | Reproduction + environmental events | Default mutation; scheduled drought then food boom |

Stages 1–4 use `EcosystemMode.TrainingSupport` with respawn so PPO episodes can continue. Stages 5–6 use persistent mode and default extinction+time stop rules.

JSON copies: `Training/experiments/curriculum/`.

## How to run

```bash
# Terminal 1 — trainer (training only)
ROLE=herbivore STAGE=1 ./Training/scripts/train_curriculum.sh
# ROLE=predator STAGE=3 ./Training/scripts/train_curriculum.sh
# ROLE=combined STAGE=6 ./Training/scripts/train_curriculum.sh
```

In Unity:

1. Apply `TrainingCurriculum.Create(stage, focus)` onto `SimulationConfig` (or assign an `ExperimentConfigurationAsset`).
2. Set prefab `CreatureBrain` policies to match the focus (`LearnedPpo` only for roles listed in the YAML).
3. Press Play so agents connect.

Overwrite a previous trainer run: `FORCE=1 ROLE=herbivore STAGE=1 ./Training/scripts/train_curriculum.sh`.

Results: `Training/results/` (gitignored). This repository does not include a trained production model.

## After training

Export/evaluate with [Training/scripts/evaluate_baseline_vs_ppo.sh](../Training/scripts/evaluate_baseline_vs_ppo.sh) and [EXPERIMENTS.md](EXPERIMENTS.md). Do not use a live trainer during evaluation.
