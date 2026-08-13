# EvoLife ML-Agents Training

Unity package: `com.unity.ml-agents` **2.0.1**. Python: `mlagents` compatible with ML-Agents 2.0.x.

## Behavior names

| Role | Behavior name |
|------|----------------|
| Herbivore | `EvoLifeHerbivore` |
| Predator | `EvoLifePredator` |

These strings must match `MlAgentsBehaviorNames` and the Unity **Behavior Parameters** component on `EvoLifeCreatureAgent`.

Observation vector size: **28**. Action space: **2 continuous** (`move_x`, `move_z`). See [Docs/AI_ML_AGENTS.md](../Docs/AI_ML_AGENTS.md).

## Start training

```bash
pip install mlagents   # version compatible with com.unity.ml-agents 2.0.x
chmod +x Training/scripts/*.sh

# Terminal 1 — trainer waits for Unity
./Training/scripts/train_herbivore.sh
# or both roles:
./Training/scripts/train_evolife.sh
```

Then in Unity:

1. Open the project (Unity 2022.3 LTS).
2. On the creature prefab/instance: `CreatureBrain` policy = **LearnedPpo**.
3. Confirm `EvoLifeCreatureAgent` is present. Behavior name is applied from `CreatureIdentity.role`.
4. Press Play. The trainer should connect.

Overwrite a previous run: `FORCE=1 RUN_ID=herbivore_ppo_dev ./Training/scripts/train_herbivore.sh`

Results are written to `Training/results/` (gitignored). Do not commit `.onnx` / checkpoints unless explicitly required.

## Switch PPO vs scripted baseline

- `CreatureBrain.policyKind` or `SimulationConfig` herbivore/predator policy.
- `AgentPolicyKind.ScriptedBaseline` — heuristic survival controller (`ScriptedBaselinePolicy`); Agent component disabled. See [Docs/SCRIPTED_BASELINE.md](../Docs/SCRIPTED_BASELINE.md).
- `AgentPolicyKind.LearnedPpo` — `EvoLifeCreatureAgent` exclusive control.

Analytics can tell them apart (`scripted_baseline` vs `learned_ppo`). For a full comparison protocol (configs, backend queries, fairness notes), use [Docs/SCRIPTED_BASELINE.md](../Docs/SCRIPTED_BASELINE.md) and [Docs/ANALYTICS.md](../Docs/ANALYTICS.md).

Starter PPO hyperparameters in `Training/configs/` are **experimental**, not tuned optima.
