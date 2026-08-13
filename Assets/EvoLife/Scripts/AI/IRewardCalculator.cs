using EvoLife.Common;

namespace EvoLife.AI
{
    /// <summary>
    /// Produces scalar rewards for RL. Reward shaping lives here — not in Creatures.
    /// </summary>
    public interface IRewardCalculator
    {
        float CalculateReward(IReadOnlyVitalState vitals, bool episodeEnded);
    }
}
