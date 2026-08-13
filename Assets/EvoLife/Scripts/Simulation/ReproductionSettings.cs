using System;
using UnityEngine;
using EvoLife.Genetics;

namespace EvoLife.Simulation
{
    /// <summary>
    /// Mating costs, local range, and inheritance operator settings.
    /// Eligibility uses these values plus genome <c>reproduction_threshold</c>.
    /// </summary>
    [Serializable]
    public sealed class ReproductionSettings
    {
        [SerializeField] float mateRange = 4f;
        [SerializeField] float maturityAgeSeconds = 20f;
        [SerializeField] float minAgeFraction;
        [SerializeField] float minHealthRatio = 0.35f;
        [SerializeField] float energyCost = 15f;
        [SerializeField] float healthCost;
        [SerializeField] float cooldownSeconds = 12f;
        [SerializeField] CrossoverMode crossoverMode = CrossoverMode.Weighted;
        [SerializeField] float parentAWeight = 0.5f;
        [SerializeField] float mutationProbability = 0.15f;
        [SerializeField] float mutationMagnitudeScale = 1f;

        public float MateRange
        {
            get => mateRange;
            set => mateRange = Mathf.Max(0f, value);
        }

        public float MaturityAgeSeconds
        {
            get => maturityAgeSeconds;
            set => maturityAgeSeconds = Mathf.Max(0f, value);
        }

        public float MinAgeFraction
        {
            get => minAgeFraction;
            set => minAgeFraction = Mathf.Clamp01(value);
        }

        public float MinHealthRatio
        {
            get => minHealthRatio;
            set => minHealthRatio = Mathf.Clamp01(value);
        }

        public float EnergyCost
        {
            get => energyCost;
            set => energyCost = Mathf.Max(0f, value);
        }

        public float HealthCost
        {
            get => healthCost;
            set => healthCost = Mathf.Max(0f, value);
        }

        public float CooldownSeconds
        {
            get => cooldownSeconds;
            set => cooldownSeconds = Mathf.Max(0f, value);
        }

        public CrossoverMode CrossoverMode
        {
            get => crossoverMode;
            set => crossoverMode = value;
        }

        public float ParentAWeight
        {
            get => parentAWeight;
            set => parentAWeight = Mathf.Clamp01(value);
        }

        public float MutationProbability
        {
            get => mutationProbability;
            set => mutationProbability = Mathf.Clamp01(value);
        }

        public float MutationMagnitudeScale
        {
            get => mutationMagnitudeScale;
            set => mutationMagnitudeScale = Mathf.Max(0f, value);
        }

        public GeneticsConfig ToGeneticsConfig() =>
            new GeneticsConfig(
                new CrossoverConfig(crossoverMode, parentAWeight),
                new MutationConfig(mutationProbability, mutationMagnitudeScale));

        public static ReproductionSettings ForTests(
            float mateRange = 8f,
            float maturityAgeSeconds = 10f,
            float minHealthRatio = 0.2f,
            float energyCost = 10f,
            float healthCost = 0f,
            float cooldownSeconds = 5f,
            CrossoverMode crossoverMode = CrossoverMode.Average,
            float mutationProbability = 0f,
            float mutationMagnitudeScale = 0f)
        {
            return new ReproductionSettings
            {
                MateRange = mateRange,
                MaturityAgeSeconds = maturityAgeSeconds,
                MinAgeFraction = 0f,
                MinHealthRatio = minHealthRatio,
                EnergyCost = energyCost,
                HealthCost = healthCost,
                CooldownSeconds = cooldownSeconds,
                CrossoverMode = crossoverMode,
                ParentAWeight = 0.5f,
                MutationProbability = mutationProbability,
                MutationMagnitudeScale = mutationMagnitudeScale
            };
        }
    }

    [CreateAssetMenu(fileName = "ReproductionConfig", menuName = "EvoLife/Simulation/Reproduction Config")]
    public sealed class ReproductionConfig : ScriptableObject
    {
        [SerializeField] ReproductionSettings settings = new ReproductionSettings();

        public ReproductionSettings Settings
        {
            get
            {
                if (settings == null)
                {
                    settings = new ReproductionSettings();
                }

                return settings;
            }
        }
    }
}
