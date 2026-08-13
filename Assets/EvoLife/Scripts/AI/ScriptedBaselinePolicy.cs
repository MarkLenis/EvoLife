using UnityEngine;
using EvoLife.Common;

namespace EvoLife.AI
{
    /// <summary>
    /// Deterministic heuristic baseline for evaluation against PPO.
    /// </summary>
    public sealed class ScriptedBaselinePolicy : ICreaturePolicy
    {
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

            // Hungry → bias forward; thirsty → bias sideways. Placeholder heuristic only.
            var hunger = obs.Length > 1 ? obs[1] : 0f;
            var thirst = obs.Length > 2 ? obs[2] : 0f;

            var actions = new float[actionExecutor.ActionSize];
            if (actions.Length >= 2)
            {
                actions[0] = Mathf.Clamp(thirst - 0.3f, -1f, 1f);
                actions[1] = Mathf.Clamp(hunger - 0.3f, -1f, 1f);
            }

            actionExecutor.ApplyActions(actions);
            _ = rewardCalculator?.CalculateReward(vitals, false);
        }
    }
}
