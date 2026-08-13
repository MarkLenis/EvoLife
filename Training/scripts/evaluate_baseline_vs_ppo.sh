#!/usr/bin/env bash
# Evaluate a trained ONNX policy against the scripted baseline (manual Unity scene setup).
set -euo pipefail
echo "Evaluation workflow (manual until automation lands):"
echo "1. Place the trained .onnx under Assets/EvoLife/ (or StreamingAssets). Do not commit binaries unless required."
echo "2. On creature prefabs, set Behavior Parameters to Inference Only and assign the model."
echo "3. Set SimulationConfig / CreatureBrain policy to LearnedPpo vs ScriptedBaseline."
echo "   Scripted and PPO must not run on the same creature at the same time."
echo "4. Run Bootstrap scene and export stats via Backend."
echo "5. Compare totalAlive / survival curves in the analytics API."
