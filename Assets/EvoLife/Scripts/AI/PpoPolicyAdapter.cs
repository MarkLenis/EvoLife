using EvoLife.Common;

namespace EvoLife.AI
{
    /// <summary>
    /// Seam for Unity ML-Agents PPO. When the ML-Agents package is present, replace the
    /// body of Collect/Apply with an Agent subclass (see Docs/ARCHITECTURE.md).
    /// This adapter keeps EvoLife.AI compiling and testable without requiring a trained model.
    /// </summary>
    public sealed class PpoPolicyAdapter : ICreaturePolicy
    {
        readonly float[] lastActions = new float[2];

        public void Step(
            IObservationSource observationSource,
            IActionExecutor actionExecutor,
            IRewardCalculator rewardCalculator,
            IReadOnlyVitalState vitals)
        {
            if (observationSource == null || actionExecutor == null)
            {
                return;
            }

            var obs = new float[observationSource.ObservationSize];
            observationSource.WriteObservations(obs);

#if EVOLIFE_MLAGENTS
            // Integration point: forward obs to ML-Agents Agent.CollectObservations /
            // OnActionReceived. Keep reward calculation in IRewardCalculator.
#endif
            // Until a trained policy is wired, stay idle so baselines remain the evaluation default.
            for (var i = 0; i < lastActions.Length; i++)
            {
                lastActions[i] = 0f;
            }

            actionExecutor.ApplyActions(lastActions);
            _ = rewardCalculator?.CalculateReward(vitals, vitals != null && !vitals.IsAlive);
        }
    }
}
