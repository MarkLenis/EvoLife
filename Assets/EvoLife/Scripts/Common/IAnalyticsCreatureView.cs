using System;

namespace EvoLife.Common
{
    /// <summary>
    /// Read-only bundle of contracts Analytics may observe on a creature.
    /// Any property may be null when the owning component is absent.
    /// </summary>
    public interface IAnalyticsCreatureView
    {
        ICreatureIdentity Identity { get; }
        IReadOnlyVitalState Vitals { get; }
        ICreatureLineage Lineage { get; }
        IPolicyKindOwner Policy { get; }
        IReadOnlyGenomeTraits GenomeTraits { get; }
        IEpisodeMetrics EpisodeMetrics { get; }
    }

    /// <summary>
    /// Simulation-owned spawn/death fan-out. Analytics listens; it must not spawn or kill.
    /// </summary>
    public interface ICreatureLifecycleEvents
    {
        event Action<IAnalyticsCreatureView> Spawned;
        event Action<CreatureDeathNotice, IAnalyticsCreatureView> Died;
    }
}
