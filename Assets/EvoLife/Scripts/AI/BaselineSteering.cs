using UnityEngine;

namespace EvoLife.AI
{
    /// <summary>
    /// Converts a selected motive plus local sensor directions into CreatureActionSchema v2
    /// locomotion (forward + turn + sprint/effort). Does not write Transform positions and
    /// does not output world X/Z steering.
    /// </summary>
    public static class BaselineSteering
    {
        const float DeadZone = 0.0001f;

        public static BaselineLocomotion Compute(
            BaselineMotive motive,
            in BaselineSensedWorld world,
            BaselineMemory memory,
            ScriptedBaselineSettings settings)
        {
            settings = settings ?? ScriptedBaselineSettings.HerbivoreDefaults();
            float dirX;
            float dirZ;
            float scale;
            float sprint;

            switch (motive)
            {
                case BaselineMotive.Flee:
                    dirX = -world.PredatorDirX;
                    dirZ = -world.PredatorDirZ;
                    scale = settings.FleeMoveScale;
                    sprint = 1f;
                    if (dirX * dirX + dirZ * dirZ <= DeadZone)
                    {
                        HeadingFromMemory(memory, out dirX, out dirZ);
                    }

                    break;
                case BaselineMotive.SeekWater:
                    dirX = world.WaterDirX;
                    dirZ = world.WaterDirZ;
                    scale = settings.SeekMoveScale;
                    sprint = 0f;
                    break;
                case BaselineMotive.SeekFood:
                    dirX = world.FoodDirX;
                    dirZ = world.FoodDirZ;
                    scale = settings.SeekMoveScale;
                    sprint = 0f;
                    break;
                case BaselineMotive.Hunt:
                    dirX = world.HerbivoreDirX;
                    dirZ = world.HerbivoreDirZ;
                    scale = settings.SeekMoveScale;
                    sprint = 1f;
                    break;
                case BaselineMotive.Rest:
                    return BaselineLocomotion.Zero;
                default:
                    HeadingFromMemory(memory, out dirX, out dirZ);
                    scale = settings.WanderMoveScale;
                    sprint = 0f;
                    break;
            }

            return FromLocalDirection(dirX, dirZ, scale, sprint);
        }

        public static BaselineLocomotion FromLocalDirection(float dirX, float dirZ, float scale, float sprint)
        {
            var magnitude = Mathf.Sqrt(dirX * dirX + dirZ * dirZ);
            if (magnitude <= DeadZone)
            {
                return BaselineLocomotion.Zero;
            }

            dirX /= magnitude;
            dirZ /= magnitude;
            var forward = Mathf.Clamp(dirZ * scale, -1f, 1f);
            var turn = Mathf.Clamp(dirX * scale, -1f, 1f);
            return new BaselineLocomotion(forward, turn, Mathf.Clamp01(sprint));
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
    }

    public readonly struct BaselineLocomotion
    {
        public BaselineLocomotion(float forward, float turn, float sprintOrEffort)
        {
            Forward = forward;
            Turn = turn;
            SprintOrEffort = sprintOrEffort;
        }

        public float Forward { get; }
        public float Turn { get; }
        public float SprintOrEffort { get; }

        public static BaselineLocomotion Zero => default;
    }
}
