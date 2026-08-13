#!/usr/bin/env bash
# Train EvoLifeHerbivore PPO.
# Usage:
#   1. pip install mlagents  # compatible with com.unity.ml-agents 2.0.x
#   2. Start this script; it waits for a Unity trainer connection.
#   3. Open Unity, load a herbivore training experiment (LearnedPpo herbivores), press Play.
# Optional:
#   RUN_ID=my_run FORCE=1 ./Training/scripts/train_herbivore.sh
#   CURRICULUM=1 STAGE=2 ./Training/scripts/train_herbivore.sh
# This is training, not evaluation or a presentation demo.
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
if [[ "${CURRICULUM:-0}" == "1" ]]; then
  CONFIG="${CONFIG:-${ROOT}/Training/configs/curriculum/herbivore.yaml}"
else
  CONFIG="${CONFIG:-${ROOT}/Training/configs/herbivore_ppo.yaml}"
fi
STAGE_SUFFIX=""
if [[ -n "${STAGE:-}" ]]; then
  STAGE_SUFFIX="_stage${STAGE}"
fi
RUN_ID="${RUN_ID:-herbivore_ppo_dev${STAGE_SUFFIX}}"
RESULTS_DIR="${ROOT}/Training/results"
FORCE_ARGS=()
if [[ "${FORCE:-0}" == "1" ]]; then
  FORCE_ARGS+=(--force)
fi

echo "Mode: TRAINING (not evaluation, not presentation)"
echo "Starting ML-Agents training with ${CONFIG}"
echo "Behavior name: EvoLifeHerbivore"
echo "Observation size: 31 | Actions: 3 continuous + discrete branch size 6"
if [[ -n "${STAGE:-}" ]]; then
  echo "Curriculum stage: ${STAGE} — load TrainingCurriculum stage${STAGE} herbivore config in Unity."
fi
echo "Ensure Unity is ready to connect (Play mode or a training build)."
mkdir -p "${RESULTS_DIR}"
mlagents-learn "${CONFIG}" --run-id="${RUN_ID}" --time-scale=20 --results-dir="${RESULTS_DIR}" "${FORCE_ARGS[@]}"
