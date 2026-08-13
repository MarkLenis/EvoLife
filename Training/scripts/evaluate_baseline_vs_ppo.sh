#!/usr/bin/env bash
# EVALUATION workflow: compare a trained ONNX policy against the scripted baseline.
# This is not training and not a presentation/demo pass.
set -euo pipefail
echo "Mode: EVALUATION (not training, not presentation)"
echo "1. Keep trained .onnx under Training/results/ or a local folder. Do not commit binaries unless required."
echo "2. On creature prefabs, set Behavior Parameters to Inference Only and assign the model."
echo "3. Set SimulationConfig / ExperimentConfiguration policies to LearnedPpo vs ScriptedBaseline."
echo "   Scripted and PPO must not run on the same creature at the same time."
echo "4. Pick a starter scenario (see Docs/EXPERIMENTS.md), e.g. normal_control, with a fixed randomSeed."
echo "5. Run Bootstrap with ExperimentOrchestrator + ExperimentSession + backend uploads."
echo "6. Compare population-series / survival / policy-comparison in the analytics API."
echo "See Docs/EXPERIMENTS.md, Docs/SCRIPTED_BASELINE.md, and Docs/ANALYTICS.md."
