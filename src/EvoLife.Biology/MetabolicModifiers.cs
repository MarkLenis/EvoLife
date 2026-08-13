namespace EvoLife.Biology
{
    /// <summary>
    /// Runtime multipliers applied on top of <see cref="MetabolicRates"/>.
    /// Genetics and temporary effects can adjust these without mutating base rates.
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

        public float MaxHealthMultiplier { get; set; }
        public float MaxEnergyMultiplier { get; set; }
        public float MaxAgeMultiplier { get; set; }
        public float HungerRateMultiplier { get; set; }
        public float ThirstRateMultiplier { get; set; }
        public float EnergyConsumptionMultiplier { get; set; }
        public float RestingRecoveryMultiplier { get; set; }
        public float StarvationDamageMultiplier { get; set; }
        public float DehydrationDamageMultiplier { get; set; }

        public static MetabolicModifiers Identity { get; } = new MetabolicModifiers();
    }
}
