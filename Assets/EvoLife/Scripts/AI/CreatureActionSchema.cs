using UnityEngine;

namespace EvoLife.AI
{
    /// <summary>
    /// Explicit continuous action layout for EvoLife locomotion.
    /// Interaction actions (eat/drink/attack/rest) are omitted until dedicated executors exist.
    ///
    /// Layout v1:
    /// <list type="number">
    /// <item>moveX — horizontal / strafe, expected [-1, 1]</item>
    /// <item>moveZ — forward / back, expected [-1, 1]</item>
    /// </list>
    /// </summary>
    public static class CreatureActionSchema
    {
        public const int Version = 1;
        public const int ContinuousCount = 2;

        public const int IndexMoveX = 0;
        public const int IndexMoveZ = 1;

        public static readonly string[] Names =
        {
            "move_x",
            "move_z"
        };

        /// <summary>
        /// Clamps incoming actions into a destination buffer of <see cref="ContinuousCount"/>.
        /// Null, short, or invalid sources become zeros.
        /// </summary>
        public static void ClampTo(float[] source, float[] destination)
        {
            if (destination == null || destination.Length < ContinuousCount)
            {
                return;
            }

            var x = 0f;
            var z = 0f;
            if (source != null && source.Length >= ContinuousCount)
            {
                x = Mathf.Clamp(source[IndexMoveX], -1f, 1f);
                z = Mathf.Clamp(source[IndexMoveZ], -1f, 1f);
            }

            destination[IndexMoveX] = x;
            destination[IndexMoveZ] = z;
        }

        public static float[] ClampCopy(float[] source)
        {
            var copy = new float[ContinuousCount];
            ClampTo(source, copy);
            return copy;
        }

        public static bool IsValid(float[] actions) =>
            actions != null && actions.Length >= ContinuousCount;
    }
}
