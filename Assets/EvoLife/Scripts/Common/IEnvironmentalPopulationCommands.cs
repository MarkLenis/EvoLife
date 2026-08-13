namespace EvoLife.Common
{
    /// <summary>
    /// Port for ecological events to spawn or remove creatures.
    /// Simulation implements this through <c>CreatureSpawner</c> and lifecycle death APIs.
    /// Environment must not instantiate or destroy creature objects itself.
    /// </summary>
    public interface IEnvironmentalPopulationCommands
    {
        int SpawnRole(CreatureRole role, int count);

        int RemoveRole(CreatureRole role, int count);
    }
}
