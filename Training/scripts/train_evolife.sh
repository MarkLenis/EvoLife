#!/usr/bin/env bash
# Train both EvoLifeHerbivore and EvoLifePredator from one Unity scene.
# Usage: ./Training/scripts/train_evolife.sh
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
CONFIG="${ROOT}/Training/configs/evolife_ppo.yaml"
RUN_ID="${RUN_ID:-evolife_ppo_dev}"
RESULTS_DIR="${ROOT}/Training/results"
FORCE_ARGS=()
if [[ "${FORCE:-0}" == "1" ]]; then
  FORCE_ARGS+=(--force)
fi

echo "Starting ML-Agents training with ${CONFIG}"
echo "Behavior names: EvoLifeHerbivore, EvoLifePredator"
echo "Ensure Unity is ready to connect (Play mode or a training build)."
mkdir -p "${RESULTS_DIR}"
mlagents-learn "${CONFIG}" --run-id="${RUN_ID}" --time-scale=20 --results-dir="${RESULTS_DIR}" "${FORCE_ARGS[@]}"
