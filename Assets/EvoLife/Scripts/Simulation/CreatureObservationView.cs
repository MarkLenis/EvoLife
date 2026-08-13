using UnityEngine;
using EvoLife.Common;

namespace EvoLife.Simulation
{
    /// <summary>
    /// Adapts a spawned creature's Common contracts for Analytics observers.
    /// Holds component references only — it does not copy or mutate creature state.
    /// </summary>
    public sealed class CreatureObservationView : IAnalyticsCreatureView
    {
        public CreatureObservationView(GameObject instance)
        {
            Identity = instance != null ? instance.GetComponent<ICreatureIdentity>() : null;
            Vitals = instance != null ? instance.GetComponent<IReadOnlyVitalState>() : null;
            Lineage = instance != null ? instance.GetComponent<ICreatureLineage>() : null;
            Policy = instance != null ? instance.GetComponent<IPolicyKindOwner>() : null;
            GenomeTraits = instance != null ? instance.GetComponent<IReadOnlyGenomeTraits>() : null;
            EpisodeMetrics = instance != null ? instance.GetComponent<IEpisodeMetrics>() : null;
        }

        public ICreatureIdentity Identity { get; }
        public IReadOnlyVitalState Vitals { get; }
        public ICreatureLineage Lineage { get; }
        public IPolicyKindOwner Policy { get; }
        public IReadOnlyGenomeTraits GenomeTraits { get; }
        public IEpisodeMetrics EpisodeMetrics { get; }
    }
}
