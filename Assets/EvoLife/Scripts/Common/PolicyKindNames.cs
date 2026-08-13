using System;

namespace EvoLife.Common
{
    /// <summary>
    /// Wire names shared with the FastAPI experiment/run configuration.
    /// </summary>
    public static class PolicyKindNames
    {
        public const string ScriptedBaseline = "scripted_baseline";
        public const string LearnedPpo = "learned_ppo";

        public static string ToWireName(AgentPolicyKind kind) =>
            kind == AgentPolicyKind.LearnedPpo ? LearnedPpo : ScriptedBaseline;

        public static bool TryParse(string wireName, out AgentPolicyKind kind)
        {
            if (string.Equals(wireName, LearnedPpo, StringComparison.OrdinalIgnoreCase)
                || string.Equals(wireName, "LearnedPpo", StringComparison.OrdinalIgnoreCase))
            {
                kind = AgentPolicyKind.LearnedPpo;
                return true;
            }

            if (string.Equals(wireName, ScriptedBaseline, StringComparison.OrdinalIgnoreCase)
                || string.Equals(wireName, "ScriptedBaseline", StringComparison.OrdinalIgnoreCase))
            {
                kind = AgentPolicyKind.ScriptedBaseline;
                return true;
            }

            kind = AgentPolicyKind.ScriptedBaseline;
            return false;
        }
    }

    public static class DeathCauseNames
    {
        public static string ToWireName(DeathCause cause)
        {
            switch (cause)
            {
                case DeathCause.Predation:
                    return "predation";
                case DeathCause.Starvation:
                    return "starvation";
                case DeathCause.Dehydration:
                    return "dehydration";
                case DeathCause.OldAge:
                    return "old_age";
                case DeathCause.Environmental:
                    return "environmental";
                default:
                    return "unknown";
            }
        }
    }

    public static class CreatureRoleNames
    {
        public static string ToWireName(CreatureRole role) =>
            role == CreatureRole.Predator ? "predator" : "herbivore";
    }
}
