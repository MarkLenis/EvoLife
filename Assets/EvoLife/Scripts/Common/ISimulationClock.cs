namespace EvoLife.Common
{
    /// <summary>
    /// Simulation time source. Simulation owns the clock; other modules only read it.
    /// </summary>
    public interface ISimulationClock
    {
        float SimulationTimeSeconds { get; }
        float DeltaTimeSeconds { get; }
        float TimeScale { get; }
        bool IsPaused { get; }
    }
}
