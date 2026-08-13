namespace EvoLife.Biology
{
    /// <summary>
    /// Configurable per-creature metabolic parameters. Genetics can clone and adjust these values.
    /// </summary>
    public sealed class MetabolicRates
    {
        public MetabolicRates(
            float maxHealth,
            float maxEnergy,
            float maxAge,
            float hungerIncreaseRate,
            float thirstIncreaseRate,
            float passiveEnergyConsumption,
            float walkingEnergyConsumption,
            float sprintingEnergyConsumption,
            float attackEnergyConsumption,
            float restingRecovery,
            float starvationDamage,
            float dehydrationDamage,
            float hungerCapacity = 100f,
            float thirstCapacity = 100f,
            float starvationThreshold = 100f,
            float dehydrationThreshold = 100f)
        {
            MaxHealth = maxHealth;
            MaxEnergy = maxEnergy;
            MaxAge = maxAge;
            HungerIncreaseRate = hungerIncreaseRate;
            ThirstIncreaseRate = thirstIncreaseRate;
            PassiveEnergyConsumption = passiveEnergyConsumption;
            WalkingEnergyConsumption = walkingEnergyConsumption;
            SprintingEnergyConsumption = sprintingEnergyConsumption;
            AttackEnergyConsumption = attackEnergyConsumption;
            RestingRecovery = restingRecovery;
            StarvationDamage = starvationDamage;
            DehydrationDamage = dehydrationDamage;
            HungerCapacity = hungerCapacity;
            ThirstCapacity = thirstCapacity;
            StarvationThreshold = starvationThreshold;
            DehydrationThreshold = dehydrationThreshold;
        }

        public float MaxHealth { get; }
        public float MaxEnergy { get; }
        public float MaxAge { get; }

        /// <summary>Hunger gained per second when not eating.</summary>
        public float HungerIncreaseRate { get; }

        /// <summary>Thirst gained per second when not drinking.</summary>
        public float ThirstIncreaseRate { get; }

        /// <summary>Energy consumed per second while idle.</summary>
        public float PassiveEnergyConsumption { get; }

        /// <summary>Additional energy consumed per second while walking.</summary>
        public float WalkingEnergyConsumption { get; }

        /// <summary>Additional energy consumed per second while sprinting.</summary>
        public float SprintingEnergyConsumption { get; }

        /// <summary>Additional energy consumed per second while attacking.</summary>
        public float AttackEnergyConsumption { get; }

        /// <summary>Energy restored per second while resting.</summary>
        public float RestingRecovery { get; }

        /// <summary>Health lost per second when hunger is at or above <see cref="StarvationThreshold"/>.</summary>
        public float StarvationDamage { get; }

        /// <summary>Health lost per second when thirst is at or above <see cref="DehydrationThreshold"/>.</summary>
        public float DehydrationDamage { get; }

        /// <summary>Upper bound for hunger accumulation.</summary>
        public float HungerCapacity { get; }

        /// <summary>Upper bound for thirst accumulation.</summary>
        public float ThirstCapacity { get; }

        /// <summary>Hunger level at or above which starvation damage is applied.</summary>
        public float StarvationThreshold { get; }

        /// <summary>Thirst level at or above which dehydration damage is applied.</summary>
        public float DehydrationThreshold { get; }

        /// <summary>Balanced default profile suitable for herbivore-scale creatures.</summary>
        public static MetabolicRates CreateDefault() =>
            new MetabolicRates(
                maxHealth: 100f,
                maxEnergy: 100f,
                maxAge: 600f,
                hungerIncreaseRate: 1f,
                thirstIncreaseRate: 1.5f,
                passiveEnergyConsumption: 0.5f,
                walkingEnergyConsumption: 2f,
                sprintingEnergyConsumption: 6f,
                attackEnergyConsumption: 8f,
                restingRecovery: 3f,
                starvationDamage: 2f,
                dehydrationDamage: 3f);

        public MetabolicRates With(
            float? maxHealth = null,
            float? maxEnergy = null,
            float? maxAge = null,
            float? hungerIncreaseRate = null,
            float? thirstIncreaseRate = null,
            float? passiveEnergyConsumption = null,
            float? walkingEnergyConsumption = null,
            float? sprintingEnergyConsumption = null,
            float? attackEnergyConsumption = null,
            float? restingRecovery = null,
            float? starvationDamage = null,
            float? dehydrationDamage = null,
            float? hungerCapacity = null,
            float? thirstCapacity = null,
            float? starvationThreshold = null,
            float? dehydrationThreshold = null) =>
            new MetabolicRates(
                maxHealth ?? MaxHealth,
                maxEnergy ?? MaxEnergy,
                maxAge ?? MaxAge,
                hungerIncreaseRate ?? HungerIncreaseRate,
                thirstIncreaseRate ?? ThirstIncreaseRate,
                passiveEnergyConsumption ?? PassiveEnergyConsumption,
                walkingEnergyConsumption ?? WalkingEnergyConsumption,
                sprintingEnergyConsumption ?? SprintingEnergyConsumption,
                attackEnergyConsumption ?? AttackEnergyConsumption,
                restingRecovery ?? RestingRecovery,
                starvationDamage ?? StarvationDamage,
                dehydrationDamage ?? DehydrationDamage,
                hungerCapacity ?? HungerCapacity,
                thirstCapacity ?? ThirstCapacity,
                starvationThreshold ?? StarvationThreshold,
                dehydrationThreshold ?? DehydrationThreshold);
    }
}
