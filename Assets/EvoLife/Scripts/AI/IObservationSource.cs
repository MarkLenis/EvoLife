namespace EvoLife.AI
{
    /// <summary>
    /// Builds observation vectors for a creature. Reads vitals/environment; does not mutate them.
    /// </summary>
    public interface IObservationSource
    {
        int ObservationSize { get; }
        void WriteObservations(float[] buffer);
    }
}
