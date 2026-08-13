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
}
