#!/usr/bin/env bash
# Train EvoLifePredator PPO.
# Usage: RUN_ID=predator_dev ./Training/scripts/train_predator.sh
#        CURRICULUM=1 STAGE=3 ./Training/scripts/train_predator.sh
# This is training, not evaluation or a presentation demo.
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
if [[ "${CURRICULUM:-0}" == "1" ]]; then
  CONFIG="${CONFIG:-${ROOT}/Training/configs/curriculum/predator.yaml}"
else
  CONFIG="${CONFIG:-${ROOT}/Training/configs/predator_ppo.yaml}"
fi
STAGE_SUFFIX=""
if [[ -n "${STAGE:-}" ]]; then
  STAGE_SUFFIX="_stage${STAGE}"
fi
RUN_ID="${RUN_ID:-predator_ppo_dev${STAGE_SUFFIX}}"
RESULTS_DIR="${ROOT}/Training/results"
FORCE_ARGS=()
if [[ "${FORCE:-0}" == "1" ]]; then
  FORCE_ARGS+=(--force)
fi

echo "Mode: TRAINING (not evaluation, not presentation)"
echo "Starting ML-Agents training with ${CONFIG}"
echo "Behavior name: EvoLifePredator"
echo "Observation size: 31 | Actions: 3 continuous + discrete branch size 6"
if [[ -n "${STAGE:-}" ]]; then
  echo "Curriculum stage: ${STAGE} — load TrainingCurriculum stage${STAGE} predator config in Unity."
fi
echo "Ensure Unity is ready to connect (Play mode or a training build)."
mkdir -p "${RESULTS_DIR}"
mlagents-learn "${CONFIG}" --run-id="${RUN_ID}" --time-scale=20 --results-dir="${RESULTS_DIR}" "${FORCE_ARGS[@]}"
