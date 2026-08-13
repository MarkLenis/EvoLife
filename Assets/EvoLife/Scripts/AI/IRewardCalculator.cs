using EvoLife.Common;

namespace EvoLife.AI
{
    /// <summary>
    /// Result of one reward evaluation, including whether the RL episode should end.
    /// </summary>
    public readonly struct RewardSignal
    {
        public RewardSignal(float reward, bool terminateEpisode)
        {
            Reward = reward;
            TerminateEpisode = terminateEpisode;
        }

        public float Reward { get; }
        public bool TerminateEpisode { get; }

        public static RewardSignal None => new RewardSignal(0f, false);
    }

    /// <summary>
    /// Produces scalar rewards for RL. Reward shaping lives here — not in Creatures.
    /// </summary>
    public interface IRewardCalculator
    {
        float CalculateReward(IReadOnlyVitalState vitals, bool episodeEnded);
    }

    /// <summary>
    /// Stateful training rewards with episode-reset and termination flags.
    /// </summary>
    public interface IEpisodeRewardCalculator : IRewardCalculator
    {
        RewardSignal Evaluate(IReadOnlyVitalState vitals);
        void OnEpisodeBegin();
    }
}
