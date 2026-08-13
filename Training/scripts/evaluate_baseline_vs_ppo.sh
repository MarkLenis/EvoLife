#!/usr/bin/env bash
# Evaluate a trained ONNX policy against the scripted baseline (manual Unity scene setup).
set -euo pipefail
echo "Evaluation workflow (manual until automation lands):"
echo "1. Place the trained .onnx under Assets/EvoLife/ (or StreamingAssets)."
echo "2. Set SimulationConfig herbivore/predator policy to LearnedPpo vs ScriptedBaseline."
echo "3. Run Bootstrap scene and export stats via Backend."
echo "4. Compare totalAlive / survival curves in the analytics API."
