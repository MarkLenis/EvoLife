using System;
using UnityEngine;
using EvoLife.Common;

namespace EvoLife.AI
{
    /// <summary>
    /// Tunable heuristic thresholds for <see cref="ScriptedBaselinePolicy"/>.
    /// Values are experimental comparison defaults, not claimed optima.
    /// Genetics are not encoded here; phenotype/sense range come from existing creature systems.
    /// </summary>
    [Serializable]
    public sealed class ScriptedBaselineSettings
    {
        [Header("Need thresholds (normalized 0-1)")]
        [SerializeField] float hungerSeekThreshold = 0.45f;
        [SerializeField] float thirstSeekThreshold = 0.40f;
        [SerializeField] float urgentThirstThreshold = 0.75f;
        [SerializeField] float restEnergyThreshold = 0.22f;
        [SerializeField] float criticalEnergyThreshold = 0.08f;

        [Header("Threat / hunt (distance is fraction of sense range)")]
        [SerializeField] float fleeDistance = 0.70f;
        [SerializeField] float attackDistance = 0.10f;
        [SerializeField] float interactDistance = 0.08f;
        [SerializeField] float chaseAbandonDistance = 0.92f;

        [Header("Stability")]
        [SerializeField] float motiveStickiness = 0.15f;
        [SerializeField] float minMotiveHoldSeconds = 0.35f;
        [SerializeField] float wanderUpdateIntervalSeconds = 1.5f;
        [SerializeField] float chaseAbandonSeconds = 8f;
        [SerializeField] float huntRetryCooldownSeconds = 2f;
        [SerializeField] float interactCooldownSeconds = 0.4f;

        [Header("Locomotion scales (clamped to [-1, 1])")]
        [SerializeField] float seekMoveScale = 1f;
        [SerializeField] float wanderMoveScale = 0.55f;
        [SerializeField] float fleeMoveScale = 1f;

        [Header("Interaction amounts (Creatures / Environment APIs)")]
        [SerializeField] float foodConsumeRequest = 8f;
        [SerializeField] float foodEnergyGain = 4f;
        [SerializeField] float drinkRequest = 8f;
        [SerializeField] float attackDamage = 12f;

        public float HungerSeekThreshold
        {
            get => hungerSeekThreshold;
            set => hungerSeekThreshold = value;
        }

        public float ThirstSeekThreshold
        {
            get => thirstSeekThreshold;
            set => thirstSeekThreshold = value;
        }

        public float UrgentThirstThreshold
        {
            get => urgentThirstThreshold;
            set => urgentThirstThreshold = value;
        }

        public float RestEnergyThreshold
        {
            get => restEnergyThreshold;
            set => restEnergyThreshold = value;
        }

        public float CriticalEnergyThreshold
        {
            get => criticalEnergyThreshold;
            set => criticalEnergyThreshold = value;
        }

        public float FleeDistance
        {
            get => fleeDistance;
            set => fleeDistance = value;
        }

        public float AttackDistance
        {
            get => attackDistance;
            set => attackDistance = value;
        }

        public float InteractDistance
        {
            get => interactDistance;
            set => interactDistance = value;
        }

        public float ChaseAbandonDistance
        {
            get => chaseAbandonDistance;
            set => chaseAbandonDistance = value;
        }

        public float MotiveStickiness
        {
            get => motiveStickiness;
            set => motiveStickiness = value;
        }

        public float MinMotiveHoldSeconds
        {
            get => minMotiveHoldSeconds;
            set => minMotiveHoldSeconds = value;
        }

        public float WanderUpdateIntervalSeconds
        {
            get => wanderUpdateIntervalSeconds;
            set => wanderUpdateIntervalSeconds = value;
        }

        public float ChaseAbandonSeconds
        {
            get => chaseAbandonSeconds;
            set => chaseAbandonSeconds = value;
        }

        public float HuntRetryCooldownSeconds
        {
            get => huntRetryCooldownSeconds;
            set => huntRetryCooldownSeconds = value;
        }

        public float InteractCooldownSeconds
        {
            get => interactCooldownSeconds;
            set => interactCooldownSeconds = value;
        }

        public float SeekMoveScale
        {
            get => seekMoveScale;
            set => seekMoveScale = value;
        }

        public float WanderMoveScale
        {
            get => wanderMoveScale;
            set => wanderMoveScale = value;
        }

        public float FleeMoveScale
        {
            get => fleeMoveScale;
            set => fleeMoveScale = value;
        }

        public float FoodConsumeRequest
        {
            get => foodConsumeRequest;
            set => foodConsumeRequest = value;
        }

        public float FoodEnergyGain
        {
            get => foodEnergyGain;
            set => foodEnergyGain = value;
        }

        public float DrinkRequest
        {
            get => drinkRequest;
            set => drinkRequest = value;
        }

        public float AttackDamage
        {
            get => attackDamage;
            set => attackDamage = value;
        }

        public static ScriptedBaselineSettings HerbivoreDefaults() => new ScriptedBaselineSettings();

        public static ScriptedBaselineSettings PredatorDefaults()
        {
            return new ScriptedBaselineSettings
            {
                HungerSeekThreshold = 0.35f,
                ThirstSeekThreshold = 0.50f,
                UrgentThirstThreshold = 0.72f,
                RestEnergyThreshold = 0.18f,
                CriticalEnergyThreshold = 0.08f,
                FleeDistance = 0.40f,
                AttackDistance = 0.10f,
                InteractDistance = 0.08f,
                ChaseAbandonDistance = 0.92f,
                MotiveStickiness = 0.15f,
                MinMotiveHoldSeconds = 0.35f,
                WanderUpdateIntervalSeconds = 1.25f,
                ChaseAbandonSeconds = 8f,
                HuntRetryCooldownSeconds = 2f,
                InteractCooldownSeconds = 0.35f,
                SeekMoveScale = 1f,
                WanderMoveScale = 0.50f,
                FleeMoveScale = 1f,
                FoodConsumeRequest = 8f,
                FoodEnergyGain = 4f,
                DrinkRequest = 8f,
                AttackDamage = 12f
            };
        }

        public static ScriptedBaselineSettings ForRole(CreatureRole role) =>
            role == CreatureRole.Predator ? PredatorDefaults() : HerbivoreDefaults();
    }
}
