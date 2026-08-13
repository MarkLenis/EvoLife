namespace EvoLife.Common
{
    /// <summary>
    /// Read-only view of one ecological event occurrence.
    /// </summary>
    public interface IReadOnlyEnvironmentalEvent
    {
        int EventId { get; }

        EnvironmentalEventKind Kind { get; }

        float StartedAtSimulationTime { get; }

        float EndsAtSimulationTime { get; }

        bool IsActive { get; }
    }
}
