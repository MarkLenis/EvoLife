using EvoLife.Common;

namespace EvoLife.Creatures
{
    /// <summary>
    /// Immutable snapshot of a creature's biological state for AI and diagnostics.
    /// </summary>
    public readonly struct CreatureState
    {
        public CreatureState(
            float health,
            float maxHealth,
            float hunger,
            float maxHunger,
            float thirst,
            float maxThirst,
            float energy,
            float maxEnergy,
            float age,
            float maxAge,
            bool isAlive,
            DeathCause? deathCause)
        {
            Health = health;
            MaxHealth = maxHealth;
            Hunger = hunger;
            MaxHunger = maxHunger;
            Thirst = thirst;
            MaxThirst = maxThirst;
            Energy = energy;
            MaxEnergy = maxEnergy;
            Age = age;
            MaxAge = maxAge;
            IsAlive = isAlive;
            DeathCause = deathCause;
        }

        public float Health { get; }
        public float MaxHealth { get; }
        public float Hunger { get; }
        public float MaxHunger { get; }
        public float Thirst { get; }
        public float MaxThirst { get; }
        public float Energy { get; }
        public float MaxEnergy { get; }
        public float Age { get; }
        public float MaxAge { get; }
        public bool IsAlive { get; }
        public DeathCause? DeathCause { get; }

        public float HealthRatio => MaxHealth <= 0f ? 0f : Health / MaxHealth;
        public float HungerRatio => MaxHunger <= 0f ? 0f : Hunger / MaxHunger;
        public float ThirstRatio => MaxThirst <= 0f ? 0f : Thirst / MaxThirst;
        public float EnergyRatio => MaxEnergy <= 0f ? 0f : Energy / MaxEnergy;
        public float AgeRatio => MaxAge <= 0f ? 0f : Age / MaxAge;
    }
}
