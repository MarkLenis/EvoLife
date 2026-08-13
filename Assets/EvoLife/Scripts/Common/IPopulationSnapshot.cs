namespace EvoLife.Common
{
    /// <summary>
    /// Population counters exposed for analytics and UI without coupling to spawn logic.
    /// Births/deaths are cumulative registrations/unregistrations for the run.
    /// </summary>
    public interface IPopulationSnapshot
    {
        int HerbivoreCount { get; }
        int PredatorCount { get; }
        int TotalAlive { get; }
        int Births { get; }
        int Deaths { get; }
    }
}
