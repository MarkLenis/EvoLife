using System;
using UnityEngine;
using EvoLife.Common;
using EvoLife.Creatures;

namespace EvoLife.AI
{
    /// <summary>
    /// Optional nearby-creature sensor using one physics overlap. Derives nearest herbivore
    /// and nearest predator from the same local query. Returns zeros when Physics finds
    /// nothing (no colliders, no other identities). Does not use Simulation registries.
    /// </summary>
    public sealed class PhysicsCreatureProximitySensor : ICreatureProximitySensor
    {
        const int ColliderBufferSize = 32;

        readonly Transform origin;
        readonly Func<float> senseRange;
        readonly ICreatureIdentity self;
        readonly Collider[] colliderBuffer = new Collider[ColliderBufferSize];

        public PhysicsCreatureProximitySensor(Transform origin, Func<float> senseRange, ICreatureIdentity self = null)
        {
            this.origin = origin;
            this.senseRange = senseRange;
            this.self = self;
        }

        public void WriteNearestRoles(float[] buffer, int herbivoreOffset, int predatorOffset)
        {
            var range = senseRange != null ? senseRange() : 0f;
            if (origin == null || range <= 0f)
            {
                WriteEmpty(buffer, herbivoreOffset, predatorOffset);
                return;
            }

            var count = Physics.OverlapSphereNonAlloc(origin.position, range, colliderBuffer);
            Transform bestHerbivore = null;
            Transform bestPredator = null;
            var bestHerbivoreSqr = range * range;
            var bestPredatorSqr = range * range;

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

                var targetVitals = col.GetComponentInParent<CreatureVitals>();
                if (targetVitals != null && !targetVitals.IsAlive)
                {
                    continue;
                }

                var sqr = (col.transform.position - origin.position).sqrMagnitude;
                if (identity.Role == CreatureRole.Predator)
                {
                    if (sqr <= bestPredatorSqr)
                    {
                        bestPredatorSqr = sqr;
                        bestPredator = col.transform;
                    }
                }
                else if (sqr <= bestHerbivoreSqr)
                {
                    bestHerbivoreSqr = sqr;
                    bestHerbivore = col.transform;
                }
            }

            WriteTarget(buffer, herbivoreOffset, origin, range, bestHerbivore);
            WriteTarget(buffer, predatorOffset, origin, range, bestPredator);
        }

        static void WriteTarget(
            float[] buffer,
            int offset,
            Transform origin,
            float range,
            Transform target)
        {
            var present = target != null;
            ObservationMath.WriteLocalProximity(
                buffer,
                offset,
                origin.position,
                origin.rotation,
                present ? target.position : origin.position,
                range,
                present);
        }

        static void WriteEmpty(float[] buffer, int herbivoreOffset, int predatorOffset)
        {
            ObservationMath.WriteZeros(buffer, herbivoreOffset, CreatureObservationSchema.ResourceChannelCount);
            ObservationMath.WriteZeros(buffer, predatorOffset, CreatureObservationSchema.ResourceChannelCount);
        }
    }

    /// <summary>
    /// Test/stub sensor that writes independent herbivore and predator targets, or zeros when absent.
    /// </summary>
    public sealed class StaticCreatureProximitySensor : ICreatureProximitySensor
    {
        readonly Func<Vector3> origin;
        readonly Func<Quaternion> rotation;
        readonly Func<float> senseRange;
        readonly Func<Vector3?> herbivoreTarget;
        readonly Func<Vector3?> predatorTarget;

        public StaticCreatureProximitySensor(
            Func<Vector3> origin,
            Func<float> senseRange,
            Func<Vector3?> herbivoreTarget,
            Func<Vector3?> predatorTarget,
            Func<Quaternion> rotation = null)
        {
            this.origin = origin;
            this.senseRange = senseRange;
            this.herbivoreTarget = herbivoreTarget;
            this.predatorTarget = predatorTarget;
            this.rotation = rotation ?? (() => Quaternion.identity);
        }

        public void WriteNearestRoles(float[] buffer, int herbivoreOffset, int predatorOffset)
        {
            var range = senseRange != null ? senseRange() : 0f;
            var position = origin != null ? origin() : Vector3.zero;
            var rot = rotation != null ? rotation() : Quaternion.identity;
            WriteOne(buffer, herbivoreOffset, position, rot, range, herbivoreTarget);
            WriteOne(buffer, predatorOffset, position, rot, range, predatorTarget);
        }

        static void WriteOne(
            float[] buffer,
            int offset,
            Vector3 position,
            Quaternion rotation,
            float range,
            Func<Vector3?> target)
        {
            var targetPos = target?.Invoke();
            var present = targetPos.HasValue && range > 0f;
            ObservationMath.WriteLocalProximity(
                buffer,
                offset,
                position,
                rotation,
                present ? targetPos.Value : position,
                range,
                present);
        }
    }
}
