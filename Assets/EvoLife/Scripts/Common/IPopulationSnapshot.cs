namespace EvoLife.Common
{
    /// <summary>
    /// Population counters exposed for analytics and UI without coupling to spawn logic.
    /// </summary>
    public interface IPopulationSnapshot
    {
        int HerbivoreCount { get; }
        int PredatorCount { get; }
        int TotalAlive { get; }
    }
}
