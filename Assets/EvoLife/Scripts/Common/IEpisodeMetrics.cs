namespace EvoLife.Common
{
    /// <summary>
    /// Optional RL episode counters exposed by AI for analytics.
    /// Missing values mean the policy does not publish a return — do not invent one.
    /// </summary>
    public interface IEpisodeMetrics
    {
        AgentPolicyKind PolicyKind { get; }
        float EpisodeSurvivalSeconds { get; }
        bool HasEpisodeReturn { get; }
        float EpisodeReturn { get; }
        int CompletedEpisodeCount { get; }
    }
}
