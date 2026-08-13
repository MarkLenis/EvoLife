using UnityEngine;

namespace EvoLife.AI
{
    /// <summary>
    /// Canonical action layout shared by learned PPO and the scripted baseline.
    ///
    /// Layout v2:
    /// Continuous (3):
    /// <list type="number">
    /// <item>forward — movement along the creature's local forward, expected [-1, 1]</item>
    /// <item>turn — yaw left/right, expected [-1, 1]</item>
    /// <item>sprint_or_effort — scales speed between walk and sprint, expected [0, 1]</item>
    /// </list>
    /// Discrete interaction branch (size 6):
    /// none, eat, drink, attack, rest, reproduce_request.
    ///
    /// <see cref="InteractionReproduceRequest"/> asks Simulation to attempt local mating.
    /// AI never decides success. Missing handlers are a safe no-op.
    /// </summary>
    public static class CreatureActionSchema
    {
        public const int Version = 2;
        public const int ContinuousCount = 3;
        public const int DiscreteBranchCount = 1;
        public const int InteractionBranchSize = 6;

        public const int IndexForward = 0;
        public const int IndexTurn = 1;
        public const int IndexSprintOrEffort = 2;

        public const int InteractionNone = 0;
        public const int InteractionEat = 1;
        public const int InteractionDrink = 2;
        public const int InteractionAttack = 3;
        public const int InteractionRest = 4;
        public const int InteractionReproduceRequest = 5;

        public static readonly string[] Names =
        {
            "forward",
            "turn",
            "sprint_or_effort"
        };

        public static readonly string[] InteractionNames =
        {
            "none",
            "eat",
            "drink",
            "attack",
            "rest",
            "reproduce_request"
        };

        /// <summary>
        /// Clamps incoming continuous actions into a destination buffer of <see cref="ContinuousCount"/>.
        /// Forward/turn are [-1, 1]. Sprint/effort is [0, 1]. Null, short, or invalid sources become zeros.
        /// </summary>
        public static void ClampTo(float[] source, float[] destination)
        {
            if (destination == null || destination.Length < ContinuousCount)
            {
                return;
            }

            var forward = 0f;
            var turn = 0f;
            var effort = 0f;
            if (source != null && source.Length >= ContinuousCount)
            {
                forward = Mathf.Clamp(source[IndexForward], -1f, 1f);
                turn = Mathf.Clamp(source[IndexTurn], -1f, 1f);
                effort = Mathf.Clamp01(source[IndexSprintOrEffort]);
            }

            destination[IndexForward] = forward;
            destination[IndexTurn] = turn;
            destination[IndexSprintOrEffort] = effort;
        }

        public static float[] ClampCopy(float[] source)
        {
            var copy = new float[ContinuousCount];
            ClampTo(source, copy);
            return copy;
        }

        public static int ClampInteraction(int interaction)
        {
            if (interaction < InteractionNone || interaction >= InteractionBranchSize)
            {
                return InteractionNone;
            }

            return interaction;
        }

        public static bool IsValid(float[] actions) =>
            actions != null && actions.Length >= ContinuousCount;
    }
}
