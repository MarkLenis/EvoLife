using System;
using UnityEngine;
using EvoLife.Common;
using EvoLife.Creatures;

namespace EvoLife.AI
{
    /// <summary>
    /// Optional nearby-creature sensor using physics overlap. Returns zeros when Physics
    /// finds nothing (no colliders, no other identities). Does not use Simulation registries.
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

        public void WriteNearest(float[] buffer, int offset)
        {
            var range = senseRange != null ? senseRange() : 0f;
            if (origin == null || range <= 0f)
            {
                WriteEmpty(buffer, offset);
                return;
            }

            var count = Physics.OverlapSphereNonAlloc(origin.position, range, colliderBuffer);
            Transform best = null;
            ICreatureIdentity bestIdentity = null;
            var bestSqr = range * range;

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

                var sqr = (col.transform.position - origin.position).sqrMagnitude;
                if (sqr <= bestSqr)
                {
                    bestSqr = sqr;
                    best = col.transform;
                    bestIdentity = identity;
                }
            }

            if (best == null || bestIdentity == null)
            {
                WriteEmpty(buffer, offset);
                return;
            }

            ObservationMath.WriteLocalProximity(
                buffer,
                offset,
                origin.position,
                origin.rotation,
                best.position,
                range,
                present: true);

            if (buffer != null && buffer.Length > offset + CreatureObservationSchema.OffsetCreatureRole)
            {
                buffer[offset + CreatureObservationSchema.OffsetCreatureRole] =
                    ObservationMath.RoleToObservation(bestIdentity.Role);
                buffer[offset + CreatureObservationSchema.OffsetCreaturePresent] = 1f;
            }
        }

        static void WriteEmpty(float[] buffer, int offset) =>
            ObservationMath.WriteZeros(buffer, offset, CreatureObservationSchema.NearbyCreatureCount);
    }

    /// <summary>
    /// Test/stub sensor that writes a provided nearby creature, or zeros when absent.
    /// </summary>
    public sealed class StaticCreatureProximitySensor : ICreatureProximitySensor
    {
        readonly Func<Vector3> origin;
        readonly Func<Quaternion> rotation;
        readonly Func<float> senseRange;
        readonly Func<Vector3?> target;
        readonly Func<CreatureRole?> targetRole;

        public StaticCreatureProximitySensor(
            Func<Vector3> origin,
            Func<float> senseRange,
            Func<Vector3?> target,
            Func<CreatureRole?> targetRole,
            Func<Quaternion> rotation = null)
        {
            this.origin = origin;
            this.senseRange = senseRange;
            this.target = target;
            this.targetRole = targetRole;
            this.rotation = rotation ?? (() => Quaternion.identity);
        }

        public void WriteNearest(float[] buffer, int offset)
        {
            var range = senseRange != null ? senseRange() : 0f;
            var position = origin != null ? origin() : Vector3.zero;
            var targetPos = target?.Invoke();
            var role = targetRole?.Invoke();
            var present = targetPos.HasValue && role.HasValue && range > 0f;

            ObservationMath.WriteLocalProximity(
                buffer,
                offset,
                position,
                rotation(),
                present ? targetPos.Value : position,
                range,
                present);

            if (buffer == null || buffer.Length < offset + CreatureObservationSchema.NearbyCreatureCount)
            {
                return;
            }

            buffer[offset + CreatureObservationSchema.OffsetCreatureRole] =
                present ? ObservationMath.RoleToObservation(role.Value) : 0f;
            buffer[offset + CreatureObservationSchema.OffsetCreaturePresent] = present ? 1f : 0f;
        }
    }
}
