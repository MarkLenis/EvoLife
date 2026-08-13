namespace EvoLife.Creatures
{
    /// <summary>
    /// Runtime multipliers applied on top of <see cref="MetabolicRates"/>.
    /// Genetics and temporary effects adjust these without mutating base rates.
    /// Each creature must own an independent instance — never share a singleton.
    /// </summary>
    public sealed class MetabolicModifiers
    {
        public MetabolicModifiers(
            float maxHealthMultiplier = 1f,
            float maxEnergyMultiplier = 1f,
            float maxAgeMultiplier = 1f,
            float hungerRateMultiplier = 1f,
            float thirstRateMultiplier = 1f,
            float energyConsumptionMultiplier = 1f,
            float restingRecoveryMultiplier = 1f,
            float starvationDamageMultiplier = 1f,
            float dehydrationDamageMultiplier = 1f)
        {
            MaxHealthMultiplier = maxHealthMultiplier;
            MaxEnergyMultiplier = maxEnergyMultiplier;
            MaxAgeMultiplier = maxAgeMultiplier;
            HungerRateMultiplier = hungerRateMultiplier;
            ThirstRateMultiplier = thirstRateMultiplier;
            EnergyConsumptionMultiplier = energyConsumptionMultiplier;
            RestingRecoveryMultiplier = restingRecoveryMultiplier;
            StarvationDamageMultiplier = starvationDamageMultiplier;
            DehydrationDamageMultiplier = dehydrationDamageMultiplier;
        }

        public float MaxHealthMultiplier { get; }
        public float MaxEnergyMultiplier { get; }
        public float MaxAgeMultiplier { get; }
        public float HungerRateMultiplier { get; }
        public float ThirstRateMultiplier { get; }
        public float EnergyConsumptionMultiplier { get; }
        public float RestingRecoveryMultiplier { get; }
        public float StarvationDamageMultiplier { get; }
        public float DehydrationDamageMultiplier { get; }

        /// <summary>
        /// Fresh identity multipliers (all 1). Callers always receive a new instance.
        /// </summary>
        public static MetabolicModifiers CreateIdentity() => new MetabolicModifiers();

        public MetabolicModifiers Clone() =>
            new MetabolicModifiers(
                MaxHealthMultiplier,
                MaxEnergyMultiplier,
                MaxAgeMultiplier,
                HungerRateMultiplier,
                ThirstRateMultiplier,
                EnergyConsumptionMultiplier,
                RestingRecoveryMultiplier,
                StarvationDamageMultiplier,
                DehydrationDamageMultiplier);

        public MetabolicModifiers With(
            float? maxHealthMultiplier = null,
            float? maxEnergyMultiplier = null,
            float? maxAgeMultiplier = null,
            float? hungerRateMultiplier = null,
            float? thirstRateMultiplier = null,
            float? energyConsumptionMultiplier = null,
            float? restingRecoveryMultiplier = null,
            float? starvationDamageMultiplier = null,
            float? dehydrationDamageMultiplier = null) =>
            new MetabolicModifiers(
                maxHealthMultiplier ?? MaxHealthMultiplier,
                maxEnergyMultiplier ?? MaxEnergyMultiplier,
                maxAgeMultiplier ?? MaxAgeMultiplier,
                hungerRateMultiplier ?? HungerRateMultiplier,
                thirstRateMultiplier ?? ThirstRateMultiplier,
                energyConsumptionMultiplier ?? EnergyConsumptionMultiplier,
                restingRecoveryMultiplier ?? RestingRecoveryMultiplier,
                starvationDamageMultiplier ?? StarvationDamageMultiplier,
                dehydrationDamageMultiplier ?? DehydrationDamageMultiplier);
    }
}
