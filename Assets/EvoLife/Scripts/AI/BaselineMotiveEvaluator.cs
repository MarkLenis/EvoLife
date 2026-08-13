using System;
using UnityEngine;
using EvoLife.Common;

namespace EvoLife.AI
{
    /// <summary>
    /// Pure utility / priority evaluator for the scripted baseline.
    /// Reads a <see cref="BaselineSensedWorld"/> (same local sensors as PPO) and does not
    /// mutate vitals or query omniscient registries.
    /// </summary>
    public sealed class BaselineMotiveEvaluator
    {
        readonly System.Random random;

        public BaselineMotiveEvaluator(int seed = 1)
        {
            random = new System.Random(seed);
        }

        public BaselineDecision Evaluate(
            in BaselineSensedWorld world,
            BaselineMemory memory,
            ScriptedBaselineSettings settings,
            CreatureRole role,
            float deltaTime)
        {
            if (memory == null)
            {
                throw new ArgumentNullException(nameof(memory));
            }

            settings = settings ?? ScriptedBaselineSettings.ForRole(role);
            var dt = deltaTime > 0f ? deltaTime : 0f;

            TickTimers(memory, dt);
            UpdateWanderHeading(memory, settings);

            var best = role == CreatureRole.Predator
                ? BestPredatorMotive(world, memory, settings)
                : BestHerbivoreMotive(world, settings);

            var selected = ApplyHysteresis(best, world, memory, settings, role);
            UpdateHuntTracking(selected, world, memory, settings, dt);

            var move = BaselineSteering.Compute(selected, world, memory, settings);
            var inInteract = settings.InteractDistance;
            var inAttack = settings.AttackDistance;
            var canInteract = memory.InteractCooldownRemaining <= 0f;

            var interaction = CreatureActionSchema.InteractionNone;
            if (selected == BaselineMotive.Rest)
            {
                interaction = CreatureActionSchema.InteractionRest;
            }
            else if (canInteract
                     && selected == BaselineMotive.SeekFood
                     && world.FoodPresent
                     && world.FoodDistance <= inInteract)
            {
                interaction = CreatureActionSchema.InteractionEat;
            }
            else if (canInteract
                     && selected == BaselineMotive.SeekWater
                     && world.WaterPresent
                     && world.WaterDistance <= inInteract)
            {
                interaction = CreatureActionSchema.InteractionDrink;
            }
            else if (canInteract
                     && selected == BaselineMotive.Hunt
                     && world.HerbivorePresent
                     && world.HerbivoreDistance <= inAttack)
            {
                interaction = CreatureActionSchema.InteractionAttack;
            }
            else if (canInteract && selected == BaselineMotive.Wander && ShouldRequestReproduce(world, settings, role))
            {
                interaction = CreatureActionSchema.InteractionReproduceRequest;
            }

            return new BaselineDecision(
                selected,
                move.Forward,
                move.Turn,
                move.SprintOrEffort,
                interaction);
        }

        static void TickTimers(BaselineMemory memory, float dt)
        {
            if (dt <= 0f)
            {
                return;
            }

            memory.MotiveHoldSeconds += dt;
            memory.WanderElapsedSeconds += dt;
            if (memory.HuntCooldownRemaining > 0f)
            {
                memory.HuntCooldownRemaining = Mathf.Max(0f, memory.HuntCooldownRemaining - dt);
            }

            if (memory.InteractCooldownRemaining > 0f)
            {
                memory.InteractCooldownRemaining = Mathf.Max(0f, memory.InteractCooldownRemaining - dt);
            }
        }

        void UpdateWanderHeading(BaselineMemory memory, ScriptedBaselineSettings settings)
        {
            var interval = Mathf.Max(0.05f, settings.WanderUpdateIntervalSeconds);
            var due = !memory.HasWanderHeading || memory.WanderElapsedSeconds >= interval;
            if (!due)
            {
                return;
            }

            memory.WanderElapsedSeconds = 0f;
            var angle = (float)(random.NextDouble() * Math.PI * 2.0);
            memory.WanderHeadingX = (float)Math.Cos(angle);
            memory.WanderHeadingZ = (float)Math.Sin(angle);
            memory.HasWanderHeading = true;
        }

        static ScoredMotive BestHerbivoreMotive(
            in BaselineSensedWorld world,
            ScriptedBaselineSettings settings)
        {
            var flee = CanFlee(world, settings);
            if (flee)
            {
                return new ScoredMotive(BaselineMotive.Flee, 10f + (1f - world.PredatorDistance));
            }

            if (world.Energy <= settings.CriticalEnergyThreshold)
            {
                return new ScoredMotive(BaselineMotive.Rest, 8f);
            }

            var water = CanSeekWater(world, settings, urgentOnly: false);
            var food = CanSeekFood(world, settings);
            if (water && food)
            {
                return world.Thirst >= world.Hunger
                    ? new ScoredMotive(BaselineMotive.SeekWater, 5f + world.Thirst)
                    : new ScoredMotive(BaselineMotive.SeekFood, 5f + world.Hunger);
            }

            if (water)
            {
                return new ScoredMotive(BaselineMotive.SeekWater, 5f + world.Thirst);
            }

            if (food)
            {
                return new ScoredMotive(BaselineMotive.SeekFood, 5f + world.Hunger);
            }

            if (world.Energy <= settings.RestEnergyThreshold)
            {
                return new ScoredMotive(BaselineMotive.Rest, 3f + (settings.RestEnergyThreshold - world.Energy));
            }

            return new ScoredMotive(BaselineMotive.Wander, 1f);
        }

        static ScoredMotive BestPredatorMotive(
            in BaselineSensedWorld world,
            BaselineMemory memory,
            ScriptedBaselineSettings settings)
        {
            if (world.Energy <= settings.CriticalEnergyThreshold && !PreyInAttackRange(world, settings))
            {
                return new ScoredMotive(BaselineMotive.Rest, 8f);
            }

            var urgentWater = CanSeekWater(world, settings, urgentOnly: true);
            var hunt = CanHunt(world, memory, settings);

            if (urgentWater)
            {
                return new ScoredMotive(BaselineMotive.SeekWater, 7f + world.Thirst);
            }

            if (hunt)
            {
                return new ScoredMotive(BaselineMotive.Hunt, 6f + world.Hunger + (1f - world.HerbivoreDistance) * 0.25f);
            }

            if (CanSeekWater(world, settings, urgentOnly: false))
            {
                return new ScoredMotive(BaselineMotive.SeekWater, 5f + world.Thirst);
            }

            if (world.Energy <= settings.RestEnergyThreshold)
            {
                return new ScoredMotive(BaselineMotive.Rest, 3f + (settings.RestEnergyThreshold - world.Energy));
            }

            return new ScoredMotive(BaselineMotive.Wander, 1f);
        }

        static BaselineMotive ApplyHysteresis(
            ScoredMotive best,
            in BaselineSensedWorld world,
            BaselineMemory memory,
            ScriptedBaselineSettings settings,
            CreatureRole role)
        {
            var current = memory.CurrentMotive;
            var currentValid = role == CreatureRole.Predator
                ? PredatorMotiveValid(current, world, memory, settings)
                : HerbivoreMotiveValid(current, world, settings);

            if (!currentValid)
            {
                memory.ResetMotive(best.Motive);
                return best.Motive;
            }

            if (current == best.Motive)
            {
                return current;
            }

            // Stickiness only damps food ↔ water oscillation. Threats, hunt loss,
            // leaving wander, and urgent thirst switch immediately.
            var foodWaterSwap =
                (current == BaselineMotive.SeekFood && best.Motive == BaselineMotive.SeekWater)
                || (current == BaselineMotive.SeekWater && best.Motive == BaselineMotive.SeekFood);
            if (foodWaterSwap)
            {
                var heldLongEnough = memory.MotiveHoldSeconds >= Mathf.Max(0f, settings.MinMotiveHoldSeconds);
                var currentScore = ScoreCurrent(current, world, settings);
                if (!heldLongEnough || currentScore + settings.MotiveStickiness >= best.Score)
                {
                    return current;
                }
            }

            memory.ResetMotive(best.Motive);
            return best.Motive;
        }

        static void UpdateHuntTracking(
            BaselineMotive selected,
            in BaselineSensedWorld world,
            BaselineMemory memory,
            ScriptedBaselineSettings settings,
            float dt)
        {
            if (selected != BaselineMotive.Hunt)
            {
                memory.HasHuntTarget = false;
                memory.ChaseElapsedSeconds = 0f;
                return;
            }

            memory.HasHuntTarget = world.HerbivorePresent;
            memory.ChaseElapsedSeconds += dt;
            if (ShouldAbandonChase(world, memory, settings))
            {
                memory.HasHuntTarget = false;
                memory.ChaseElapsedSeconds = 0f;
                memory.HuntCooldownRemaining = Mathf.Max(0f, settings.HuntRetryCooldownSeconds);
            }
        }

        static bool ShouldAbandonChase(
            in BaselineSensedWorld world,
            BaselineMemory memory,
            ScriptedBaselineSettings settings)
        {
            if (!world.HerbivorePresent)
            {
                return true;
            }

            return memory.ChaseElapsedSeconds >= settings.ChaseAbandonSeconds
                   && world.HerbivoreDistance >= settings.ChaseAbandonDistance;
        }

        static bool HerbivoreMotiveValid(BaselineMotive motive, in BaselineSensedWorld world, ScriptedBaselineSettings settings)
        {
            switch (motive)
            {
                case BaselineMotive.Flee:
                    return CanFlee(world, settings);
                case BaselineMotive.SeekWater:
                    return CanSeekWater(world, settings, urgentOnly: false);
                case BaselineMotive.SeekFood:
                    return CanSeekFood(world, settings);
                case BaselineMotive.Rest:
                    return world.Energy <= settings.RestEnergyThreshold && !CanFlee(world, settings);
                case BaselineMotive.Hunt:
                    return false;
                default:
                    return true;
            }
        }

        static bool PredatorMotiveValid(
            BaselineMotive motive,
            in BaselineSensedWorld world,
            BaselineMemory memory,
            ScriptedBaselineSettings settings)
        {
            switch (motive)
            {
                case BaselineMotive.SeekWater:
                    return CanSeekWater(world, settings, urgentOnly: false);
                case BaselineMotive.Hunt:
                    return CanHunt(world, memory, settings);
                case BaselineMotive.Rest:
                    return world.Energy <= settings.RestEnergyThreshold;
                case BaselineMotive.SeekFood:
                    return false;
                case BaselineMotive.Flee:
                    return false;
                default:
                    return true;
            }
        }

        static float ScoreCurrent(BaselineMotive motive, in BaselineSensedWorld world, ScriptedBaselineSettings settings)
        {
            switch (motive)
            {
                case BaselineMotive.Flee:
                    return 10f + (1f - world.PredatorDistance);
                case BaselineMotive.SeekWater:
                    return 5f + world.Thirst;
                case BaselineMotive.SeekFood:
                    return 5f + world.Hunger;
                case BaselineMotive.Hunt:
                    return 6f + world.Hunger;
                case BaselineMotive.Rest:
                    return 3f + Mathf.Max(0f, settings.RestEnergyThreshold - world.Energy);
                default:
                    return 1f;
            }
        }

        static bool CanFlee(in BaselineSensedWorld world, ScriptedBaselineSettings settings) =>
            world.PredatorPresent && world.PredatorDistance <= settings.FleeDistance;

        static bool CanSeekWater(in BaselineSensedWorld world, ScriptedBaselineSettings settings, bool urgentOnly)
        {
            if (!world.WaterPresent)
            {
                return false;
            }

            var threshold = urgentOnly ? settings.UrgentThirstThreshold : settings.ThirstSeekThreshold;
            return world.Thirst >= threshold;
        }

        static bool CanSeekFood(in BaselineSensedWorld world, ScriptedBaselineSettings settings) =>
            world.FoodPresent && world.Hunger >= settings.HungerSeekThreshold;

        static bool CanHunt(in BaselineSensedWorld world, BaselineMemory memory, ScriptedBaselineSettings settings)
        {
            if (memory.HuntCooldownRemaining > 0f)
            {
                return false;
            }

            if (!world.HerbivorePresent)
            {
                return false;
            }

            if (world.Hunger < settings.HungerSeekThreshold && !PreyInAttackRange(world, settings))
            {
                return false;
            }

            if (ShouldAbandonChase(world, memory, settings) && memory.ChaseElapsedSeconds > 0f)
            {
                memory.HuntCooldownRemaining = Mathf.Max(
                    memory.HuntCooldownRemaining,
                    settings.HuntRetryCooldownSeconds);
                return false;
            }

            return true;
        }

        static bool PreyInAttackRange(in BaselineSensedWorld world, ScriptedBaselineSettings settings) =>
            world.HerbivorePresent && world.HerbivoreDistance <= settings.AttackDistance;

        static bool ShouldRequestReproduce(
            in BaselineSensedWorld world,
            ScriptedBaselineSettings settings,
            CreatureRole role)
        {
            if (world.Energy < settings.ReproduceEnergyThreshold)
            {
                return false;
            }

            switch (role)
            {
                case CreatureRole.Predator:
                    return world.PredatorPresent && world.PredatorDistance <= settings.InteractDistance;
                case CreatureRole.Herbivore:
                    return world.HerbivorePresent && world.HerbivoreDistance <= settings.InteractDistance;
                default:
                    throw new ArgumentOutOfRangeException(nameof(role), role, "Unhandled CreatureRole.");
            }
        }

        readonly struct ScoredMotive
        {
            public ScoredMotive(BaselineMotive motive, float score)
            {
                Motive = motive;
                Score = score;
            }

            public BaselineMotive Motive { get; }
            public float Score { get; }
        }
    }
}
