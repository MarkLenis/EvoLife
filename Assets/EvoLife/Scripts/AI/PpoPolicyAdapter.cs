using EvoLife.Common;

namespace EvoLife.AI
{
    /// <summary>
    /// Compile-safe PPO fallback used only when <see cref="EvoLifeCreatureAgent"/> is unavailable
    /// (ML-Agents package missing, or the Agent component was not added).
    /// This is not a second PPO implementation: it applies idle locomotion so a misconfigured
    /// LearnedPpo creature does not also run the scripted baseline.
    /// </summary>
    public sealed class PpoPolicyAdapter : ICreaturePolicy
    {
        readonly float[] lastActions = new float[CreatureActionSchema.ContinuousCount];

        public void Step(
            IObservationSource observationSource,
            IActionExecutor actionExecutor,
            IRewardCalculator rewardCalculator,
            IReadOnlyVitalState vitals)
        {
            if (actionExecutor == null)
            {
                return;
            }

            CreatureActionSchema.ClampTo(null, lastActions);
            actionExecutor.ApplyActions(lastActions, CreatureActionSchema.InteractionNone);
            _ = rewardCalculator?.CalculateReward(vitals, vitals != null && !vitals.IsAlive);
        }
    }
}
