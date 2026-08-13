using UnityEngine;

namespace EvoLife.Creatures
{
    /// <summary>
    /// Tunable vital ranges and drain rates for a species. Data-only; no runtime logic.
    /// </summary>
    [CreateAssetMenu(fileName = "SpeciesVitals", menuName = "EvoLife/Creatures/Species Vitals")]
    public sealed class SpeciesVitalsDefinition : ScriptableObject
    {
        [Header("Caps")]
        [SerializeField] float maxHealth = 100f;
        [SerializeField] float maxEnergy = 100f;
        [SerializeField] float maxAge = 600f;
        [SerializeField] float maxHunger = 100f;
        [SerializeField] float maxThirst = 100f;

        [Header("Needs increase (per second)")]
        [SerializeField] float hungerIncreasePerSecond = 1f;
        [SerializeField] float thirstIncreasePerSecond = 1.5f;

        [Header("Energy consumption (per second)")]
        [SerializeField] float passiveEnergyConsumption = 0.5f;
        [SerializeField] float walkingEnergyConsumption = 2f;
        [SerializeField] float sprintingEnergyConsumption = 6f;
        [SerializeField] float attackEnergyConsumption = 8f;
        [SerializeField] float restingRecovery = 3f;

        [Header("Damage (per second at threshold)")]
        [SerializeField] float starvationDamage = 2f;
        [SerializeField] float dehydrationDamage = 3f;
        [SerializeField] float starvationThreshold = 100f;
        [SerializeField] float dehydrationThreshold = 100f;

        public float MaxHealth => maxHealth;
        public float MaxEnergy => maxEnergy;
        public float MaxAge => maxAge;
        public float MaxHunger => maxHunger;
        public float MaxThirst => maxThirst;
        public float HungerIncreasePerSecond => hungerIncreasePerSecond;
        public float ThirstIncreasePerSecond => thirstIncreasePerSecond;
        public float PassiveEnergyConsumption => passiveEnergyConsumption;
        public float WalkingEnergyConsumption => walkingEnergyConsumption;
        public float SprintingEnergyConsumption => sprintingEnergyConsumption;
        public float AttackEnergyConsumption => attackEnergyConsumption;
        public float RestingRecovery => restingRecovery;
        public float StarvationDamage => starvationDamage;
        public float DehydrationDamage => dehydrationDamage;
        public float StarvationThreshold => starvationThreshold;
        public float DehydrationThreshold => dehydrationThreshold;

        public MetabolicRates ToMetabolicRates() =>
            new MetabolicRates(
                maxHealth,
                maxEnergy,
                maxAge,
                hungerIncreasePerSecond,
                thirstIncreasePerSecond,
                passiveEnergyConsumption,
                walkingEnergyConsumption,
                sprintingEnergyConsumption,
                attackEnergyConsumption,
                restingRecovery,
                starvationDamage,
                dehydrationDamage,
                maxHunger,
                maxThirst,
                starvationThreshold,
                dehydrationThreshold);
    }
}
