using System;
using UnityEngine;
using EvoLife.Environment;

namespace EvoLife.AI
{
    /// <summary>
    /// Read-only proximity sensor over <see cref="ResourceRegistry.FindNearest"/>.
    /// Does not create a second environment system.
    /// </summary>
    public sealed class ResourceRegistryProximitySensor : IResourceProximitySensor
    {
        readonly Func<Vector3, ResourceKind, float, IResourceNode> findNearest;
        readonly Func<Vector3> origin;
        readonly Func<Quaternion> rotation;
        readonly Func<float> senseRange;

        public ResourceRegistryProximitySensor(
            ResourceRegistry registry,
            Transform transform,
            Func<float> senseRange)
            : this(
                registry != null ? registry.FindNearest : (Func<Vector3, ResourceKind, float, IResourceNode>)null,
                transform != null ? (Func<Vector3>)(() => transform.position) : null,
                senseRange,
                transform != null ? (Func<Quaternion>)(() => transform.rotation) : null)
        {
        }

        public ResourceRegistryProximitySensor(
            Func<Vector3, ResourceKind, float, IResourceNode> findNearest,
            Func<Vector3> origin,
            Func<float> senseRange,
            Func<Quaternion> rotation = null)
        {
            this.findNearest = findNearest;
            this.origin = origin;
            this.senseRange = senseRange;
            this.rotation = rotation ?? (() => Quaternion.identity);
        }

        public void WriteNearestFood(float[] buffer, int offset) =>
            WriteKind(buffer, offset, ResourceKind.Plant);

        public void WriteNearestWater(float[] buffer, int offset) =>
            WriteKind(buffer, offset, ResourceKind.Water);

        void WriteKind(float[] buffer, int offset, ResourceKind kind)
        {
            var range = senseRange != null ? senseRange() : 0f;
            if (findNearest == null || origin == null || range <= 0f)
            {
                ObservationMath.WriteZeros(buffer, offset, CreatureObservationSchema.ResourceChannelCount);
                return;
            }

            var position = origin();
            var node = findNearest(position, kind, range);
            var present = node != null && !node.IsDepleted;
            ObservationMath.WriteLocalProximity(
                buffer,
                offset,
                position,
                rotation(),
                present ? node.Position : position,
                range,
                present);
        }
    }
}
