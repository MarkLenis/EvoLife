#!/usr/bin/env bash
# Train both EvoLifeHerbivore and EvoLifePredator from one Unity scene.
# Both roles MUST be AgentPolicyKind.LearnedPpo or the trainer will wait for the missing behavior.
# Usage: ./Training/scripts/train_evolife.sh
#        CURRICULUM=1 STAGE=3 ./Training/scripts/train_evolife.sh
# This is training, not evaluation or a presentation demo.
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
if [[ "${CURRICULUM:-0}" == "1" ]]; then
  CONFIG="${CONFIG:-${ROOT}/Training/configs/curriculum/combined.yaml}"
else
  CONFIG="${CONFIG:-${ROOT}/Training/configs/evolife_ppo.yaml}"
fi
STAGE_SUFFIX=""
if [[ -n "${STAGE:-}" ]]; then
  STAGE_SUFFIX="_stage${STAGE}"
fi
RUN_ID="${RUN_ID:-evolife_ppo_dev${STAGE_SUFFIX}}"
RESULTS_DIR="${ROOT}/Training/results"
FORCE_ARGS=()
if [[ "${FORCE:-0}" == "1" ]]; then
  FORCE_ARGS+=(--force)
fi

echo "Mode: TRAINING (not evaluation, not presentation)"
echo "Starting ML-Agents training with ${CONFIG}"
echo "Behavior names: EvoLifeHerbivore, EvoLifePredator"
echo "Observation size: 31 | Actions: 3 continuous + discrete branch size 6"
if [[ -n "${STAGE:-}" ]]; then
  echo "Curriculum stage: ${STAGE} — load TrainingCurriculum stage${STAGE} combined config in Unity."
fi
echo "Ensure Unity is ready to connect (Play mode or a training build)."
mkdir -p "${RESULTS_DIR}"
mlagents-learn "${CONFIG}" --run-id="${RUN_ID}" --time-scale=20 --results-dir="${RESULTS_DIR}" "${FORCE_ARGS[@]}"
