#!/usr/bin/env bash
# Curriculum helper. Does not train a production model.
# Usage:
#   ROLE=herbivore STAGE=1 ./Training/scripts/train_curriculum.sh
#   ROLE=predator STAGE=3 ./Training/scripts/train_curriculum.sh
#   ROLE=combined STAGE=6 ./Training/scripts/train_curriculum.sh
# Load the matching TrainingCurriculum stage in Unity before pressing Play.
# Combined YAML requires LearnedPpo on both roles.
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
ROLE="${ROLE:-herbivore}"
STAGE="${STAGE:-1}"
export CURRICULUM=1
export STAGE
case "${ROLE}" in
  herbivore)
    exec "${ROOT}/Training/scripts/train_herbivore.sh"
    ;;
  predator)
    exec "${ROOT}/Training/scripts/train_predator.sh"
    ;;
  combined)
    exec "${ROOT}/Training/scripts/train_evolife.sh"
    ;;
  *)
    echo "ROLE must be herbivore, predator, or combined." >&2
    exit 1
    ;;
esac
