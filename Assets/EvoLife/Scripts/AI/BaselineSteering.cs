using UnityEngine;

namespace EvoLife.AI
{
    /// <summary>
    /// Converts a selected motive plus local sensor directions into legal locomotion.
    /// Does not write Transform positions.
    /// </summary>
    public static class BaselineSteering
    {
        const float DeadZone = 0.0001f;

        public static Vector2 Compute(
            BaselineMotive motive,
            in BaselineSensedWorld world,
            BaselineMemory memory,
            ScriptedBaselineSettings settings)
        {
            settings = settings ?? ScriptedBaselineSettings.HerbivoreDefaults();
            float x;
            float z;
            float scale;

            switch (motive)
            {
                case BaselineMotive.Flee:
                    x = -world.NearbyDirX;
                    z = -world.NearbyDirZ;
                    scale = settings.FleeMoveScale;
                    if (x * x + z * z <= DeadZone)
                    {
                        HeadingFromMemory(memory, out x, out z);
                    }

                    break;
                case BaselineMotive.SeekWater:
                    x = world.WaterDirX;
                    z = world.WaterDirZ;
                    scale = settings.SeekMoveScale;
                    break;
                case BaselineMotive.SeekFood:
                    x = world.FoodDirX;
                    z = world.FoodDirZ;
                    scale = settings.SeekMoveScale;
                    break;
                case BaselineMotive.Hunt:
                    x = world.NearbyDirX;
                    z = world.NearbyDirZ;
                    scale = settings.SeekMoveScale;
                    break;
                case BaselineMotive.Rest:
                    return Vector2.zero;
                default:
                    HeadingFromMemory(memory, out x, out z);
                    scale = settings.WanderMoveScale;
                    break;
            }

            return ClampNormalized(x, z, scale);
        }

        static void HeadingFromMemory(BaselineMemory memory, out float x, out float z)
        {
            if (memory != null && memory.HasWanderHeading)
            {
                x = memory.WanderHeadingX;
                z = memory.WanderHeadingZ;
                return;
            }

            x = 0f;
            z = 1f;
        }

        static Vector2 ClampNormalized(float x, float z, float scale)
        {
            var magnitude = Mathf.Sqrt(x * x + z * z);
            if (magnitude <= DeadZone)
            {
                return Vector2.zero;
            }

            x = x / magnitude * scale;
            z = z / magnitude * scale;
            return new Vector2(Mathf.Clamp(x, -1f, 1f), Mathf.Clamp(z, -1f, 1f));
        }
    }
}
