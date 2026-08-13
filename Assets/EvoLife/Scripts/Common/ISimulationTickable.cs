namespace EvoLife.Common
{
    /// <summary>
    /// Marker for systems that advance with simulation time (not necessarily Unity Update).
    /// </summary>
    public interface ISimulationTickable
    {
        void Tick(float deltaTimeSeconds);
    }
}
