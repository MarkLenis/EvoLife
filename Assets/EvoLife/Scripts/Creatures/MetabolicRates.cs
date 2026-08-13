using System;

namespace EvoLife.Creatures
{
    /// <summary>
    /// Configurable per-creature metabolic parameters. Built from species data or genetics overrides.
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
        public float HungerIncreaseRate { get; }
        public float ThirstIncreaseRate { get; }
        public float PassiveEnergyConsumption { get; }
        public float WalkingEnergyConsumption { get; }
        public float SprintingEnergyConsumption { get; }
        public float AttackEnergyConsumption { get; }
        public float RestingRecovery { get; }
        public float StarvationDamage { get; }
        public float DehydrationDamage { get; }
        public float HungerCapacity { get; }
        public float ThirstCapacity { get; }
        public float StarvationThreshold { get; }
        public float DehydrationThreshold { get; }

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
