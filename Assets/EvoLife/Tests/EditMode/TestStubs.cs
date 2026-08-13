using EvoLife.Common;

namespace EvoLife.Tests
{
    sealed class StubVitalState : IReadOnlyVitalState
    {
        public float Health { get; set; }
        public float MaxHealth { get; set; } = 100f;
        public float Hunger { get; set; }
        public float MaxHunger { get; set; } = 100f;
        public float Thirst { get; set; }
        public float MaxThirst { get; set; } = 100f;
        public float Energy { get; set; }
        public float MaxEnergy { get; set; } = 100f;
        public float Age { get; set; }
        public float MaxAge { get; set; } = 100f;
        public bool IsAlive { get; set; } = true;
        public DeathCause? CauseOfDeath { get; set; }
    }

    sealed class StubIdentity : ICreatureIdentity
    {
        public CreatureId Id { get; set; }
        public CreatureRole Role { get; set; }
        public string SpeciesId { get; set; } = "stub";
    }

    sealed class StubLineage : ICreatureLineage
    {
        public int Generation { get; set; }
        public CreatureId? ParentA { get; set; }
        public CreatureId? ParentB { get; set; }
    }

    sealed class StubPolicyOwner : IPolicyKindOwner
    {
        public AgentPolicyKind PolicyKind { get; set; } = AgentPolicyKind.ScriptedBaseline;

        public void SetPolicyKind(AgentPolicyKind kind) => PolicyKind = kind;
    }

    sealed class StubGenomeTraits : IReadOnlyGenomeTraits
    {
        readonly string[] names;
        readonly float[] values;

        public StubGenomeTraits(params (string name, float value)[] traits)
        {
            names = new string[traits.Length];
            values = new float[traits.Length];
            for (var i = 0; i < traits.Length; i++)
            {
                names[i] = traits[i].name;
                values[i] = traits[i].value;
            }
        }

        public int TraitCount => names.Length;
        public string GetTraitName(int index) => names[index];
        public float GetTraitValue(int index) => values[index];

        public bool TryGetTrait(string canonicalName, out float value)
        {
            for (var i = 0; i < names.Length; i++)
            {
                if (names[i] == canonicalName)
                {
                    value = values[i];
                    return true;
                }
            }

            value = 0f;
            return false;
        }
    }

    sealed class StubEpisodeMetrics : IEpisodeMetrics
    {
        public AgentPolicyKind PolicyKind { get; set; }
        public float EpisodeSurvivalSeconds { get; set; }
        public bool HasEpisodeReturn { get; set; }
        public float EpisodeReturn { get; set; }
        public int CompletedEpisodeCount { get; set; }
    }

    sealed class StubAnalyticsView : IAnalyticsCreatureView
    {
        public ICreatureIdentity Identity { get; set; }
        public IReadOnlyVitalState Vitals { get; set; }
        public ICreatureLineage Lineage { get; set; }
        public IPolicyKindOwner Policy { get; set; }
        public IReadOnlyGenomeTraits GenomeTraits { get; set; }
        public IEpisodeMetrics EpisodeMetrics { get; set; }
    }

    sealed class StubPopulation : IPopulationSnapshot
    {
        public int HerbivoreCount { get; set; }
        public int PredatorCount { get; set; }
        public int TotalAlive { get; set; }
        public int Births { get; set; }
        public int Deaths { get; set; }
    }
}
