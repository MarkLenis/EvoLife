using UnityEngine;
using EvoLife.Common;

namespace EvoLife.AI
{
    /// <summary>
    /// Deterministic heuristic baseline for evaluation against PPO.
    /// Hungry → bias forward; thirsty → bias sideways. Placeholder heuristic only.
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

            var hunger = obs.Length > CreatureObservationSchema.IndexHunger
                ? obs[CreatureObservationSchema.IndexHunger]
                : 0f;
            var thirst = obs.Length > CreatureObservationSchema.IndexThirst
                ? obs[CreatureObservationSchema.IndexThirst]
                : 0f;

            var actions = new float[CreatureActionSchema.ContinuousCount];
            actions[CreatureActionSchema.IndexMoveX] = Mathf.Clamp(thirst - 0.3f, -1f, 1f);
            actions[CreatureActionSchema.IndexMoveZ] = Mathf.Clamp(hunger - 0.3f, -1f, 1f);

            actionExecutor.ApplyActions(actions);
            _ = rewardCalculator?.CalculateReward(vitals, vitals != null && !vitals.IsAlive);
        }
    }
}
