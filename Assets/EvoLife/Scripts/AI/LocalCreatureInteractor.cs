using System;
using UnityEngine;
using EvoLife.Common;
using EvoLife.Creatures;
using EvoLife.Environment;

namespace EvoLife.AI
{
    /// <summary>
    /// Local eat / drink / attack / rest / reproduce-request using the same sense radius
    /// as PPO sensors. Shared by learned and scripted policies through the canonical
    /// action executor. Does not consult <c>PopulationTracker</c> or other global registries.
    /// </summary>
    public sealed class LocalCreatureInteractor : ICreatureInteractor
    {
        const int ColliderBufferSize = 32;

        readonly CreatureVitals vitals;
        readonly Transform origin;
        readonly ResourceRegistry resources;
        readonly ICreatureIdentity self;
        readonly Func<float> senseRange;
        readonly ScriptedBaselineSettings settings;
        readonly IReproductionRequestHandler reproduction;
        readonly Collider[] colliderBuffer = new Collider[ColliderBufferSize];

        public LocalCreatureInteractor(
            CreatureVitals vitals,
            Transform origin,
            ResourceRegistry resources,
            ICreatureIdentity self,
            Func<float> senseRange,
            ScriptedBaselineSettings settings,
            IReproductionRequestHandler reproduction = null)
        {
            this.vitals = vitals;
            this.origin = origin;
            this.resources = resources;
            this.self = self;
            this.senseRange = senseRange;
            this.settings = settings ?? ScriptedBaselineSettings.HerbivoreDefaults();
            this.reproduction = reproduction;
        }

        public bool TryEat() => ConsumeKind(ResourceKind.Plant, settings.FoodConsumeRequest, eaten =>
        {
            var energy = settings.FoodConsumeRequest > 0.0001f
                ? settings.FoodEnergyGain * (eaten / settings.FoodConsumeRequest)
                : 0f;
            vitals.ConsumeFood(eaten, energy);
        });

        public bool TryDrink() => ConsumeKind(ResourceKind.Water, settings.DrinkRequest, taken =>
        {
            vitals.Drink(taken);
        });

        public bool TryAttack()
        {
            if (vitals == null || origin == null || !vitals.IsAlive)
            {
                return false;
            }

            if (self != null && self.Role != CreatureRole.Predator)
            {
                return false;
            }

            var range = ResolveRange();
            var attackRange = range * Mathf.Clamp01(settings.AttackDistance);
            if (attackRange <= 0f)
            {
                return false;
            }

            var count = Physics.OverlapSphereNonAlloc(origin.position, attackRange, colliderBuffer);
            CreatureVitals best = null;
            var bestSqr = attackRange * attackRange;

            for (var i = 0; i < count; i++)
            {
                var col = colliderBuffer[i];
                if (col == null)
                {
                    continue;
                }

                var identity = col.GetComponentInParent<CreatureIdentity>();
                if (identity == null || ReferenceEquals(identity, self))
                {
                    continue;
                }

                if (identity.Role != CreatureRole.Herbivore)
                {
                    continue;
                }

                var targetVitals = col.GetComponentInParent<CreatureVitals>();
                if (targetVitals == null || !targetVitals.IsAlive || ReferenceEquals(targetVitals, vitals))
                {
                    continue;
                }

                var sqr = (col.transform.position - origin.position).sqrMagnitude;
                if (sqr <= bestSqr)
                {
                    bestSqr = sqr;
                    best = targetVitals;
                }
            }

            if (best == null)
            {
                return false;
            }

            best.ApplyDamage(Mathf.Max(0f, settings.AttackDamage), DeathCause.Predation);
            vitals.CurrentActivity = ActivityLevel.Attacking;
            return true;
        }

        public void SetResting()
        {
            if (vitals != null)
            {
                vitals.CurrentActivity = ActivityLevel.Resting;
            }
        }

        public void RequestReproduce()
        {
            reproduction?.HandleReproduceRequest();
        }

        bool ConsumeKind(ResourceKind kind, float requested, Action<float> applyTaken)
        {
            if (vitals == null || origin == null || resources == null || applyTaken == null || !vitals.IsAlive)
            {
                return false;
            }

            var range = ResolveRange();
            var interactRange = range * Mathf.Clamp01(settings.InteractDistance);
            if (interactRange <= 0f)
            {
                return false;
            }

            var node = resources.FindNearest(origin.position, kind, range);
            if (node == null || node.IsDepleted)
            {
                return false;
            }

            var offset = node.Position - origin.position;
            offset.y = 0f;
            if (offset.sqrMagnitude > interactRange * interactRange)
            {
                return false;
            }

            var taken = node.TryConsume(Mathf.Max(0f, requested));
            if (taken <= 0f)
            {
                return false;
            }

            applyTaken(taken);
            return true;
        }

        float ResolveRange()
        {
            var range = senseRange != null ? senseRange() : 0f;
            return range > 0f ? range : 0f;
        }
    }
}
