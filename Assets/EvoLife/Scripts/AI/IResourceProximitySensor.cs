namespace EvoLife.AI
{
    /// <summary>
    /// Read-only local resource sensing. Implementations query Environment; they must not own resources.
    /// Missing sensors should write zeros rather than inventing world state.
    /// </summary>
    public interface IResourceProximitySensor
    {
        /// <summary>
        /// Writes 4 floats at <paramref name="offset"/>: dirX, dirZ, distance, present.
        /// </summary>
        void WriteNearestFood(float[] buffer, int offset);

        /// <summary>
        /// Writes 4 floats at <paramref name="offset"/>: dirX, dirZ, distance, present.
        /// </summary>
        void WriteNearestWater(float[] buffer, int offset);
    }
}
