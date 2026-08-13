using System;
using UnityEngine;
using EvoLife.Common;

namespace EvoLife.Simulation
{
    /// <summary>
    /// Population and lifecycle experiment settings. Reproduction thresholds live on
    /// <see cref="ReproductionSettings"/>; this type does not implement mating.
    /// </summary>
    [Serializable]
    public sealed class EcosystemSettings
    {
        [SerializeField] EcosystemMode mode = EcosystemMode.Persistent;
        [SerializeField] bool trainingRespawnEnabled;
        [SerializeField] int maxHerbivores = 80;
        [SerializeField] int maxPredators = 24;
        [SerializeField] int minHerbivores = 4;
        [SerializeField] int minPredators = 2;
        [SerializeField] float trainingRespawnIntervalSeconds = 2f;
        [SerializeField] float founderSpawnRadius = 12f;
        [SerializeField] string herbivoreSpeciesId = "herbivore";
        [SerializeField] string predatorSpeciesId = "predator";

        public EcosystemMode Mode
        {
            get => mode;
            set => mode = value;
        }

        public bool TrainingRespawnEnabled
        {
            get => trainingRespawnEnabled;
            set => trainingRespawnEnabled = value;
        }

        public int MaxHerbivores
        {
            get => maxHerbivores;
            set => maxHerbivores = value;
        }

        public int MaxPredators
        {
            get => maxPredators;
            set => maxPredators = value;
        }

        public int MinHerbivores
        {
            get => minHerbivores;
            set => minHerbivores = value;
        }

        public int MinPredators
        {
            get => minPredators;
            set => minPredators = value;
        }

        public float TrainingRespawnIntervalSeconds
        {
            get => trainingRespawnIntervalSeconds;
            set => trainingRespawnIntervalSeconds = Mathf.Max(0f, value);
        }

        public float FounderSpawnRadius
        {
            get => founderSpawnRadius;
            set => founderSpawnRadius = Mathf.Max(0f, value);
        }

        public string HerbivoreSpeciesId
        {
            get => string.IsNullOrEmpty(herbivoreSpeciesId) ? "herbivore" : herbivoreSpeciesId;
            set => herbivoreSpeciesId = value;
        }

        public string PredatorSpeciesId
        {
            get => string.IsNullOrEmpty(predatorSpeciesId) ? "predator" : predatorSpeciesId;
            set => predatorSpeciesId = value;
        }

        public bool AllowsTrainingRespawn =>
            mode == EcosystemMode.TrainingSupport && trainingRespawnEnabled;

        public int CapFor(CreatureRole role)
        {
            switch (role)
            {
                case CreatureRole.Herbivore:
                    return maxHerbivores;
                case CreatureRole.Predator:
                    return maxPredators;
                default:
                    throw new ArgumentOutOfRangeException(nameof(role), role, "Unhandled CreatureRole.");
            }
        }

        public int FloorFor(CreatureRole role)
        {
            switch (role)
            {
                case CreatureRole.Herbivore:
                    return minHerbivores;
                case CreatureRole.Predator:
                    return minPredators;
                default:
                    throw new ArgumentOutOfRangeException(nameof(role), role, "Unhandled CreatureRole.");
            }
        }

        public string SpeciesIdFor(CreatureRole role)
        {
            switch (role)
            {
                case CreatureRole.Herbivore:
                    return HerbivoreSpeciesId;
                case CreatureRole.Predator:
                    return PredatorSpeciesId;
                default:
                    throw new ArgumentOutOfRangeException(nameof(role), role, "Unhandled CreatureRole.");
            }
        }
    }
}
