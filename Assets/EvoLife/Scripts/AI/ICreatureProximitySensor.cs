namespace EvoLife.AI
{
    /// <summary>
    /// Read-only nearby-creature sensing. Optional; null/missing implementations write zeros.
    /// Herbivore and predator results are independent so a nearer same-role creature cannot
    /// hide the other role. Implementations must not use Simulation population registries.
    /// </summary>
    public interface ICreatureProximitySensor
    {
        /// <summary>
        /// Writes two independent 4-float channels (dirX, dirZ, distance, present) for the
        /// nearest herbivore and nearest predator into <paramref name="buffer"/>.
        /// </summary>
        void WriteNearestRoles(float[] buffer, int herbivoreOffset, int predatorOffset);
    }
}
