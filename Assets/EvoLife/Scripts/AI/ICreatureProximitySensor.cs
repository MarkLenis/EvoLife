namespace EvoLife.AI
{
    /// <summary>
    /// Read-only nearby-creature sensing. Optional; null/missing implementations write zeros.
    /// </summary>
    public interface ICreatureProximitySensor
    {
        /// <summary>
        /// Writes 5 floats at <paramref name="offset"/>: dirX, dirZ, distance, role, present.
        /// </summary>
        void WriteNearest(float[] buffer, int offset);
    }
}
