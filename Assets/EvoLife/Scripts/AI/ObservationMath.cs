using UnityEngine;

namespace EvoLife.AI
{
    /// <summary>
    /// Shared observation helpers. Pure and testable.
    /// </summary>
    public static class ObservationMath
    {
        public static float Normalize(float value, float max) =>
            max <= 0f ? 0f : Mathf.Clamp01(value / max);

        public static float RoleToObservation(Common.CreatureRole role) =>
            role == Common.CreatureRole.Predator ? 1f : 0f;

        public static void WriteZeros(float[] buffer, int offset, int count)
        {
            if (buffer == null)
            {
                return;
            }

            var end = Mathf.Min(buffer.Length, offset + count);
            for (var i = Mathf.Max(0, offset); i < end; i++)
            {
                buffer[i] = 0f;
            }
        }

        /// <summary>
        /// Writes agent-local horizontal direction, normalized distance, and presence.
        /// When <paramref name="present"/> is false, all four slots are zero.
        /// </summary>
        public static void WriteLocalProximity(
            float[] buffer,
            int offset,
            Vector3 origin,
            Quaternion originRotation,
            Vector3 target,
            float senseRange,
            bool present)
        {
            if (buffer == null || buffer.Length < offset + CreatureObservationSchema.ResourceChannelCount)
            {
                return;
            }

            if (!present || senseRange <= 0f)
            {
                WriteZeros(buffer, offset, CreatureObservationSchema.ResourceChannelCount);
                return;
            }

            var world = target - origin;
            world.y = 0f;
            var local = Quaternion.Inverse(originRotation) * world;
            var magnitude = local.magnitude;
            if (magnitude <= 0.0001f)
            {
                buffer[offset + CreatureObservationSchema.OffsetDirX] = 0f;
                buffer[offset + CreatureObservationSchema.OffsetDirZ] = 0f;
                buffer[offset + CreatureObservationSchema.OffsetDistance] = 0f;
                buffer[offset + CreatureObservationSchema.OffsetPresent] = 1f;
                return;
            }

            buffer[offset + CreatureObservationSchema.OffsetDirX] = Mathf.Clamp(local.x / magnitude, -1f, 1f);
            buffer[offset + CreatureObservationSchema.OffsetDirZ] = Mathf.Clamp(local.z / magnitude, -1f, 1f);
            buffer[offset + CreatureObservationSchema.OffsetDistance] = Mathf.Clamp01(magnitude / senseRange);
            buffer[offset + CreatureObservationSchema.OffsetPresent] = 1f;
        }
    }
}
