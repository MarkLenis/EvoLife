# EvoLife ML-Agents Training

Unity package: `com.unity.ml-agents` **4.1.0**. Python: `mlagents==1.1.0` (matches ML-Agents 4.1.x).

This folder is for **training**. Evaluation and presentation/demo are documented elsewhere:

| Mode | Where |
|------|--------|
| **Training** | this README, [Docs/TRAINING_CURRICULUM.md](../Docs/TRAINING_CURRICULUM.md), `Training/scripts/train_*.sh` |
| **Evaluation** | [Docs/EXPERIMENTS.md](../Docs/EXPERIMENTS.md), `Training/scripts/evaluate_baseline_vs_ppo.sh` |
| **Presentation / demo** | Play a scene with HUD/camera. Do not treat demo playback as training or evaluation |

Do not commit generated results, checkpoints, or `.onnx` files.

## Behavior names

| Role | Behavior name |
|------|----------------|
| Herbivore | `EvoLifeHerbivore` |
| Predator | `EvoLifePredator` |

These strings must match `MlAgentsBehaviorNames` and the Unity **Behavior Parameters** component on `EvoLifeCreatureAgent`.

Observation vector size: **31** (`CreatureObservationSchema` v2). Action space: **3 continuous** (`forward`, `turn`, `sprint_or_effort`) plus **1 discrete branch of size 6** (`none`, `eat`, `drink`, `attack`, `rest`, `reproduce_request`). See [Docs/AI_ML_AGENTS.md](../Docs/AI_ML_AGENTS.md).

Configs:

| File | Roles in YAML |
|------|----------------|
| `Training/configs/herbivore_ppo.yaml` | Herbivore |
| `Training/configs/predator_ppo.yaml` | Predator |
| `Training/configs/evolife_ppo.yaml` | Both (scene must use `LearnedPpo` for both) |
| `Training/configs/curriculum/*.yaml` | Same names, shorter `max_steps` |

A YAML behavior that is not present as `LearnedPpo` in Unity will never connect.

## Start training

```bash
pip install mlagents==1.1.0   # matches com.unity.ml-agents 4.1.x
chmod +x Training/scripts/*.sh

# Terminal 1 — trainer waits for Unity
./Training/scripts/train_herbivore.sh
# predator only:
./Training/scripts/train_predator.sh
# both roles (combined scene):
./Training/scripts/train_evolife.sh
# curriculum stage (Unity must load the matching TrainingCurriculum config):
ROLE=herbivore STAGE=1 ./Training/scripts/train_curriculum.sh
```

Then in Unity:

1. Open the project (Unity 6.5). Lightweight training arenas are enough; polished terrain is not required.
2. Apply a `TrainingCurriculum` / `ExperimentConfiguration` stage (see [Docs/TRAINING_CURRICULUM.md](../Docs/TRAINING_CURRICULUM.md)).
3. On the creature prefab/instance: `CreatureBrain` policy = **LearnedPpo** for the roles you are training.
4. Confirm `EvoLifeCreatureAgent` is present. Behavior name is applied from `CreatureIdentity.role`.
5. Press Play. The trainer should connect.

Overwrite a previous run: `FORCE=1 RUN_ID=herbivore_ppo_dev ./Training/scripts/train_herbivore.sh`

Results are written to `Training/results/` (gitignored).

## Switch PPO vs scripted baseline

- `CreatureBrain.policyKind` or `SimulationConfig` / `ExperimentConfiguration` herbivore/predator policy.
- `AgentPolicyKind.ScriptedBaseline` — heuristic survival controller (`ScriptedBaselinePolicy`); Agent component disabled. See [Docs/SCRIPTED_BASELINE.md](../Docs/SCRIPTED_BASELINE.md).
- `AgentPolicyKind.LearnedPpo` — `EvoLifeCreatureAgent` exclusive control.

Analytics can tell them apart (`scripted_baseline` vs `learned_ppo`). Comparing a frozen model to the heuristic is **evaluation**, not training: use `evaluate_baseline_vs_ppo.sh` and [Docs/EXPERIMENTS.md](../Docs/EXPERIMENTS.md).

Starter PPO hyperparameters in `Training/configs/` are **experimental**, not tuned optima. This repository does not ship a final production model.
