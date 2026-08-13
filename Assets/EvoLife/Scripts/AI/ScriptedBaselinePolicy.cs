using UnityEngine;
using EvoLife.Common;

namespace EvoLife.AI
{
    /// <summary>
    /// Deterministic heuristic baseline for evaluation against PPO.
    /// Uses the same <see cref="IObservationSource"/> / <see cref="CreatureObservationSchema"/>
    /// local sensors as learned agents. Does not mutate vital fields; locomotion goes through
    /// <see cref="IActionExecutor"/>; eat/drink/attack use <see cref="ICreatureInteractor"/>.
    /// </summary>
    public sealed class ScriptedBaselinePolicy : ICreaturePolicy
    {
        const float DefaultDeltaSeconds = 0.02f;

        readonly ScriptedBaselineSettings settings;
        readonly CreatureRole role;
        readonly BaselineMotiveEvaluator evaluator;
        readonly BaselineMemory memory = new BaselineMemory();
        readonly ICreatureInteractor interactor;
        readonly float[] actions = new float[CreatureActionSchema.ContinuousCount];

        float[] observationScratch;

        /// <summary>
        /// Optional fixed timestep for EditMode tests. When unset, uses <c>Time.fixedDeltaTime</c>
        /// or 0.02s.
        /// </summary>
        public float? DeltaTimeOverride { get; set; }

        public BaselineMotive LastMotive => memory.CurrentMotive;

        public ScriptedBaselineSettings Settings => settings;

        public CreatureRole Role => role;

        public ScriptedBaselinePolicy()
            : this(null, CreatureRole.Herbivore)
        {
        }

        public ScriptedBaselinePolicy(
            ScriptedBaselineSettings settings,
            CreatureRole role,
            int seed = 1,
            ICreatureInteractor interactor = null)
        {
            this.role = role;
            this.settings = settings ?? ScriptedBaselineSettings.ForRole(role);
            this.interactor = interactor;
            evaluator = new BaselineMotiveEvaluator(seed);
        }

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

            var size = Mathf.Max(0, observationSource.ObservationSize);
            if (observationScratch == null || observationScratch.Length != size)
            {
                observationScratch = size > 0 ? new float[size] : new float[0];
            }

            if (observationScratch.Length > 0)
            {
                observationSource.WriteObservations(observationScratch);
            }

            var world = BaselineSensedWorld.FromObservations(observationScratch);
            var decision = evaluator.Evaluate(world, memory, settings, role, ResolveDeltaTime());

            actions[CreatureActionSchema.IndexMoveX] = decision.MoveX;
            actions[CreatureActionSchema.IndexMoveZ] = decision.MoveZ;
            CreatureActionSchema.ClampTo(actions, actions);
            actionExecutor.ApplyActions(actions);

            ApplyInteractions(decision);
            _ = rewardCalculator?.CalculateReward(vitals, vitals != null && !vitals.IsAlive);
        }

        void ApplyInteractions(in BaselineDecision decision)
        {
            if (interactor == null)
            {
                return;
            }

            if (decision.Rest)
            {
                interactor.SetResting();
            }

            var consumed = false;
            if (decision.TryEat)
            {
                consumed = interactor.TryEat();
            }
            else if (decision.TryDrink)
            {
                consumed = interactor.TryDrink();
            }
            else if (decision.TryAttack)
            {
                consumed = interactor.TryAttack();
            }

            if (consumed)
            {
                memory.BeginInteractCooldown(settings.InteractCooldownSeconds);
            }
        }

        float ResolveDeltaTime()
        {
            if (DeltaTimeOverride.HasValue && DeltaTimeOverride.Value > 0f)
            {
                return DeltaTimeOverride.Value;
            }

            var fixedDt = Time.fixedDeltaTime;
            return fixedDt > 0.0001f ? fixedDt : DefaultDeltaSeconds;
        }
    }
}
