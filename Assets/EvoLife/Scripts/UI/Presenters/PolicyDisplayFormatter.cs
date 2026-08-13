using EvoLife.Common;

namespace EvoLife.UI
{
    /// <summary>
    /// Policy labels for dashboard/inspector. Does not rank ScriptedBaseline vs LearnedPpo.
    /// </summary>
    public static class PolicyDisplayFormatter
    {
        public const string Unavailable = "unavailable";

        public static string FormatKind(AgentPolicyKind kind)
        {
            switch (kind)
            {
                case AgentPolicyKind.ScriptedBaseline:
                    return "ScriptedBaseline";
                case AgentPolicyKind.LearnedPpo:
                    return "LearnedPpo";
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(kind), kind, "Unhandled AgentPolicyKind.");
            }
        }

        public static string FormatKind(IPolicyKindOwner owner) =>
            owner != null ? FormatKind(owner.PolicyKind) : Unavailable;

        public static string FormatWireName(AgentPolicyKind kind) => PolicyKindNames.ToWireName(kind);

        public static string FormatModelId(string modelId)
        {
            if (string.IsNullOrEmpty(modelId))
            {
                return Unavailable;
            }

            return modelId;
        }

        public static string FormatKindAndModel(AgentPolicyKind kind, string modelId)
        {
            var label = FormatKind(kind);
            if (kind != AgentPolicyKind.LearnedPpo)
            {
                return label;
            }

            var model = FormatModelId(modelId);
            return model == Unavailable ? label : label + " (" + model + ")";
        }
    }
}
