using UnityEngine;
using EvoLife.Common;

namespace EvoLife.AI
{
    /// <summary>
    /// Deterministic heuristic baseline for evaluation against PPO.
    /// Uses the same <see cref="IObservationSource"/> / <see cref="CreatureObservationSchema"/>
    /// local sensors as learned agents. Does not mutate vital fields. Locomotion and
    /// eat/drink/attack/rest go through the canonical <see cref="IActionExecutor"/> path
    /// (CreatureActionSchema v2). There is no privileged interactor bypass.
    /// </summary>
    public sealed class ScriptedBaselinePolicy : ICreaturePolicy
    {
        const float DefaultDeltaSeconds = 0.02f;

        readonly ScriptedBaselineSettings settings;
        readonly CreatureRole role;
        readonly BaselineMotiveEvaluator evaluator;
        readonly BaselineMemory memory = new BaselineMemory();
        readonly float[] actions = new float[CreatureActionSchema.ContinuousCount];

        float[] observationScratch;

        /// <summary>
        /// Optional fixed timestep for EditMode tests. When unset, uses <c>Time.fixedDeltaTime</c>
        /// or 0.02s.
        /// </summary>
        public float? DeltaTimeOverride { get; set; }

        public BaselineMotive LastMotive => memory.CurrentMotive;

        public int LastInteraction { get; private set; }

        public ScriptedBaselineSettings Settings => settings;

        public CreatureRole Role => role;

        public ScriptedBaselinePolicy()
            : this(null, CreatureRole.Herbivore)
        {
        }

        public ScriptedBaselinePolicy(
            ScriptedBaselineSettings settings,
            CreatureRole role,
            int seed = 1)
        {
            this.role = role;
            this.settings = settings ?? ScriptedBaselineSettings.ForRole(role);
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

            actions[CreatureActionSchema.IndexForward] = decision.Forward;
            actions[CreatureActionSchema.IndexTurn] = decision.Turn;
            actions[CreatureActionSchema.IndexSprintOrEffort] = decision.SprintOrEffort;
            CreatureActionSchema.ClampTo(actions, actions);
            LastInteraction = decision.Interaction;
            actionExecutor.ApplyActions(actions, decision.Interaction);

            if (decision.TryEat || decision.TryDrink || decision.TryAttack)
            {
                memory.BeginInteractCooldown(settings.InteractCooldownSeconds);
            }

            _ = rewardCalculator?.CalculateReward(vitals, vitals != null && !vitals.IsAlive);
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
