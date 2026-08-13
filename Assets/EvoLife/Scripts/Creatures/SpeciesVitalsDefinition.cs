using UnityEngine;

namespace EvoLife.Creatures
{
    /// <summary>
    /// Tunable vital ranges and drain rates for a species. Data-only; no runtime logic.
    /// </summary>
    [CreateAssetMenu(fileName = "SpeciesVitals", menuName = "EvoLife/Creatures/Species Vitals")]
    public sealed class SpeciesVitalsDefinition : ScriptableObject
    {
        [SerializeField] float maxHealth = 100f;
        [SerializeField] float maxHunger = 100f;
        [SerializeField] float maxThirst = 100f;
        [SerializeField] float maxEnergy = 100f;
        [SerializeField] float hungerIncreasePerSecond = 1f;
        [SerializeField] float thirstIncreasePerSecond = 1.2f;
        [SerializeField] float energyDrainPerSecond = 0.5f;
        [SerializeField] float agingPerSecond = 0.1f;

        public float MaxHealth => maxHealth;
        public float MaxHunger => maxHunger;
        public float MaxThirst => maxThirst;
        public float MaxEnergy => maxEnergy;
        public float HungerIncreasePerSecond => hungerIncreasePerSecond;
        public float ThirstIncreasePerSecond => thirstIncreasePerSecond;
        public float EnergyDrainPerSecond => energyDrainPerSecond;
        public float AgingPerSecond => agingPerSecond;
    }
}
