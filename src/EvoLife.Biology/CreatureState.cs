namespace EvoLife.Biology
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
            float thirst,
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
            Thirst = thirst;
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
        public float Thirst { get; }
        public float Energy { get; }
        public float MaxEnergy { get; }
        public float Age { get; }
        public float MaxAge { get; }
        public bool IsAlive { get; }
        public DeathCause? DeathCause { get; }

        public float HealthRatio => MaxHealth <= 0f ? 0f : Health / MaxHealth;
        public float EnergyRatio => MaxEnergy <= 0f ? 0f : Energy / MaxEnergy;
        public float AgeRatio => MaxAge <= 0f ? 0f : Age / MaxAge;

        public CreatureState With(
            float? health = null,
            float? maxHealth = null,
            float? hunger = null,
            float? thirst = null,
            float? energy = null,
            float? maxEnergy = null,
            float? age = null,
            float? maxAge = null,
            bool? isAlive = null,
            DeathCause? deathCause = null) =>
            new CreatureState(
                health ?? Health,
                maxHealth ?? MaxHealth,
                hunger ?? Hunger,
                thirst ?? Thirst,
                energy ?? Energy,
                maxEnergy ?? MaxEnergy,
                age ?? Age,
                maxAge ?? MaxAge,
                isAlive ?? IsAlive,
                deathCause ?? DeathCause);
    }
}
