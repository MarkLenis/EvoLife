using EvoLife.Common;
using EvoLife.Genetics;

namespace EvoLife.Simulation
{
    /// <summary>
    /// Snapshot used by Simulation eligibility. Mating rules stay out of CreatureVitals.
    /// </summary>
    public readonly struct ReproductionEligibilityInput
    {
        public ReproductionEligibilityInput(
            bool isAlive,
            float age,
            float maxAge,
            float energy,
            float maxEnergy,
            float health,
            float maxHealth,
            float reproductionThreshold,
            bool hasReproduced,
            float lastReproductionTime)
        {
            IsAlive = isAlive;
            Age = age;
            MaxAge = maxAge;
            Energy = energy;
            MaxEnergy = maxEnergy;
            Health = health;
            MaxHealth = maxHealth;
            ReproductionThreshold = reproductionThreshold;
            HasReproduced = hasReproduced;
            LastReproductionTime = lastReproductionTime;
        }

        public bool IsAlive { get; }
        public float Age { get; }
        public float MaxAge { get; }
        public float Energy { get; }
        public float MaxEnergy { get; }
        public float Health { get; }
        public float MaxHealth { get; }
        public float ReproductionThreshold { get; }
        public bool HasReproduced { get; }
        public float LastReproductionTime { get; }

        public static ReproductionEligibilityInput FromVitals(
            IReadOnlyVitalState vitals,
            float reproductionThreshold,
            bool hasReproduced,
            float lastReproductionTime)
        {
            if (vitals == null)
            {
                return new ReproductionEligibilityInput(
                    false,
                    0f,
                    0f,
                    0f,
                    0f,
                    0f,
                    0f,
                    reproductionThreshold,
                    hasReproduced,
                    lastReproductionTime);
            }

            return new ReproductionEligibilityInput(
                vitals.IsAlive,
                vitals.Age,
                vitals.MaxAge,
                vitals.Energy,
                vitals.MaxEnergy,
                vitals.Health,
                vitals.MaxHealth,
                reproductionThreshold,
                hasReproduced,
                lastReproductionTime);
        }
    }

    /// <summary>
    /// Pure reproduction eligibility. AI may request; this decides whether Simulation
    /// will even look for a mate.
    /// </summary>
    public static class ReproductionEligibility
    {
        public static float ResolveReproductionThreshold(Genome genome, IReadOnlyPhenotype phenotype)
        {
            var trait = CanonicalGenomeSchema.Get(TraitId.ReproductionThreshold);
            if (genome != null)
            {
                return trait.Clamp(genome.Get(TraitId.ReproductionThreshold));
            }

            if (phenotype != null)
            {
                return trait.Clamp(trait.Default * phenotype.ReproductionThresholdMultiplier);
            }

            return trait.Default;
        }

        public static bool IsEligible(
            in ReproductionEligibilityInput input,
            ReproductionSettings settings,
            float simulationTimeSeconds)
        {
            if (settings == null || !input.IsAlive)
            {
                return false;
            }

            if (input.Age < settings.MaturityAgeSeconds)
            {
                return false;
            }

            if (settings.MinAgeFraction > 0f && input.MaxAge > 0f
                && input.Age / input.MaxAge < settings.MinAgeFraction)
            {
                return false;
            }

            if (input.MaxHealth <= 0f || input.Health / input.MaxHealth < settings.MinHealthRatio)
            {
                return false;
            }

            if (input.MaxEnergy <= 0f)
            {
                return false;
            }

            var energyRatio = input.Energy / input.MaxEnergy;
            if (energyRatio < input.ReproductionThreshold)
            {
                return false;
            }

            if (input.Energy < settings.EnergyCost)
            {
                return false;
            }

            if (input.HasReproduced
                && simulationTimeSeconds - input.LastReproductionTime < settings.CooldownSeconds)
            {
                return false;
            }

            return true;
        }

        public static bool AreSpeciesCompatible(string speciesA, string speciesB, CreatureRole roleA, CreatureRole roleB)
        {
            if (roleA != roleB)
            {
                return false;
            }

            if (string.IsNullOrEmpty(speciesA) || string.IsNullOrEmpty(speciesB))
            {
                return false;
            }

            return string.Equals(speciesA, speciesB, System.StringComparison.Ordinal);
        }
    }
}
