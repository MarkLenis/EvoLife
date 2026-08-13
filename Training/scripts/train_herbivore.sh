#!/usr/bin/env bash
# Train herbivore PPO via ML-Agents once the Unity Agent behavior is wired.
# Usage: ./Training/scripts/train_herbivore.sh
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
CONFIG="${ROOT}/Training/configs/herbivore_ppo.yaml"
RUN_ID="${RUN_ID:-herbivore_ppo_dev}"

echo "Starting ML-Agents training with ${CONFIG}"
echo "Ensure the Unity editor/player is ready to connect before continuing."
mlagents-learn "${CONFIG}" --run-id="${RUN_ID}" --time-scale=20
