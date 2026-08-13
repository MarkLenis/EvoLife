using EvoLife.Common;

namespace EvoLife.Analytics
{
    /// <summary>
    /// Maps <see cref="AgentPolicyKind"/> to backend wire names for experiment comparison.
    /// Does not implement policy behavior.
    /// </summary>
    public static class PolicyClassifier
    {
        public static string Classify(AgentPolicyKind kind) => PolicyKindNames.ToWireName(kind);

        public static string Classify(IPolicyKindOwner owner) =>
            owner != null ? PolicyKindNames.ToWireName(owner.PolicyKind) : PolicyKindNames.ScriptedBaseline;

        public static string Classify(IAnalyticsCreatureView view)
        {
            if (view == null)
            {
                return PolicyKindNames.ScriptedBaseline;
            }

            if (view.Policy != null)
            {
                return PolicyKindNames.ToWireName(view.Policy.PolicyKind);
            }

            if (view.EpisodeMetrics != null)
            {
                return PolicyKindNames.ToWireName(view.EpisodeMetrics.PolicyKind);
            }

            return PolicyKindNames.ScriptedBaseline;
        }

        public static bool IsLearnedPpo(string wireName) =>
            PolicyKindNames.TryParse(wireName, out var kind) && kind == AgentPolicyKind.LearnedPpo;
    }
}
