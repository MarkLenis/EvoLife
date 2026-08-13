using UnityEngine;

namespace EvoLife.AI
{
    /// <summary>
    /// Pure local-space locomotion mapping for CreatureActionSchema v2.
    /// Movement is along the creature's local forward; turn is yaw. There is no world X/Z strafe.
    /// </summary>
    public static class LocalLocomotionMath
    {
        public static void Evaluate(
            float forward,
            float turn,
            float sprintOrEffort,
            float maxSpeed,
            float sprintSpeed,
            float turnSpeedDegrees,
            out float forwardSpeed,
            out float yawDegreesPerSecond)
        {
            forward = Mathf.Clamp(forward, -1f, 1f);
            turn = Mathf.Clamp(turn, -1f, 1f);
            var effort = Mathf.Clamp01(sprintOrEffort);
            var walk = Mathf.Max(0f, maxSpeed);
            var sprint = Mathf.Max(walk, sprintSpeed);
            var topSpeed = Mathf.Lerp(walk, sprint, effort);
            forwardSpeed = forward * topSpeed;
            yawDegreesPerSecond = turn * turnSpeedDegrees;
        }

        /// <summary>
        /// Local displacement per second in the creature's local space (+Z is forward).
        /// X is always 0: diagonal √2 world-strafe is not representable.
        /// </summary>
        public static Vector3 LocalDisplacementPerSecond(float forwardSpeed) =>
            new Vector3(0f, 0f, forwardSpeed);
    }
}
