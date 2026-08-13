using UnityEngine;
using EvoLife.Common;
using EvoLife.Creatures;

namespace EvoLife.AI
{
    /// <summary>
    /// Policy entry point attached to a creature. Selects scripted vs PPO adapter.
    /// Does not own vitals or genetics.
    /// </summary>
    public sealed class CreatureBrain : MonoBehaviour
    {
        [SerializeField] AgentPolicyKind policyKind = AgentPolicyKind.ScriptedBaseline;
        [SerializeField] CreatureVitals vitals;
        [SerializeField] PlanarMoveActionExecutor actionExecutor;

        IObservationSource observations;
        IRewardCalculator rewards;
        ICreaturePolicy policy;

        public AgentPolicyKind PolicyKind => policyKind;

        void Awake()
        {
            observations = new VitalObservationSource(vitals);
            rewards = new SurvivalRewardCalculator();
            policy = CreatePolicy(policyKind);
        }

        public void SetPolicyKind(AgentPolicyKind kind)
        {
            policyKind = kind;
            policy = CreatePolicy(kind);
        }

        void FixedUpdate()
        {
            if (policy == null || vitals == null || !vitals.IsAlive)
            {
                return;
            }

            policy.Step(observations, actionExecutor, rewards, vitals);
        }

        static ICreaturePolicy CreatePolicy(AgentPolicyKind kind)
        {
            return kind == AgentPolicyKind.LearnedPpo
                ? (ICreaturePolicy)new PpoPolicyAdapter()
                : new ScriptedBaselinePolicy();
        }
    }

    public interface ICreaturePolicy
    {
        void Step(
            IObservationSource observationSource,
            IActionExecutor actionExecutor,
            IRewardCalculator rewardCalculator,
            IReadOnlyVitalState vitals);
    }
}
