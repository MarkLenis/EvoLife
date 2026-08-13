using UnityEngine;
using EvoLife.Common;

namespace EvoLife.Creatures
{
    /// <summary>
    /// Owns mutable biological vitals for one creature. Other modules read via IReadOnlyVitalState.
    /// </summary>
    public sealed class CreatureVitals : MonoBehaviour, IReadOnlyVitalState, ISimulationTickable
    {
        [SerializeField] SpeciesVitalsDefinition definition;

        float health;
        float hunger;
        float thirst;
        float energy;
        float age;
        float metabolismMultiplier = 1f;

        public float Health => health;
        public float Hunger => hunger;
        public float Thirst => thirst;
        public float Energy => energy;
        public float Age => age;
        public bool IsAlive => health > 0f;

        public void Initialize(SpeciesVitalsDefinition vitalsDefinition)
        {
            definition = vitalsDefinition;
            health = definition.MaxHealth;
            hunger = 0f;
            thirst = 0f;
            energy = definition.MaxEnergy;
            age = 0f;
        }

        /// <summary>
        /// Applied by Genetics after phenotype resolution. Creatures never read genomes directly.
        /// </summary>
        public void ApplyMetabolismMultiplier(float multiplier)
        {
            metabolismMultiplier = Mathf.Max(0.01f, multiplier);
        }

        public void Tick(float deltaTimeSeconds)
        {
            if (!IsAlive || definition == null || deltaTimeSeconds <= 0f)
            {
                return;
            }

            var metabolism = metabolismMultiplier;
            hunger = Mathf.Min(definition.MaxHunger, hunger + definition.HungerIncreasePerSecond * metabolism * deltaTimeSeconds);
            thirst = Mathf.Min(definition.MaxThirst, thirst + definition.ThirstIncreasePerSecond * metabolism * deltaTimeSeconds);
            energy = Mathf.Max(0f, energy - definition.EnergyDrainPerSecond * metabolism * deltaTimeSeconds);
            age += definition.AgingPerSecond * deltaTimeSeconds;

            // Placeholder damage model — refined later by design, not here.
            if (hunger >= definition.MaxHunger || thirst >= definition.MaxThirst || energy <= 0f)
            {
                ApplyDamage(5f * deltaTimeSeconds);
            }
        }

        public void ApplyDamage(float amount)
        {
            if (amount <= 0f || !IsAlive)
            {
                return;
            }

            health = Mathf.Max(0f, health - amount);
        }

        public void RestoreHealth(float amount)
        {
            if (definition == null || amount <= 0f || !IsAlive)
            {
                return;
            }

            health = Mathf.Min(definition.MaxHealth, health + amount);
        }

        public void ConsumeFood(float hungerRelief, float energyGain)
        {
            hunger = Mathf.Max(0f, hunger - Mathf.Max(0f, hungerRelief));
            if (definition != null)
            {
                energy = Mathf.Min(definition.MaxEnergy, energy + Mathf.Max(0f, energyGain));
            }
        }

        public void Drink(float thirstRelief)
        {
            thirst = Mathf.Max(0f, thirst - Mathf.Max(0f, thirstRelief));
        }
    }
}
