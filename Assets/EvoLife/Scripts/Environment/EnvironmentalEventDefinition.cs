using System;
using UnityEngine;
using EvoLife.Common;

namespace EvoLife.Environment
{
    /// <summary>
    /// Data-driven ecological event. Effects go through Environment and Simulation ports.
    /// </summary>
    [Serializable]
    public sealed class EnvironmentalEventDefinition
    {
        [SerializeField] EnvironmentalEventKind kind;
        [SerializeField] float durationSeconds = 10f;
        [SerializeField] float plantRegenMultiplier = 1f;
        [SerializeField] float waterRechargeMultiplier = 1f;
        [SerializeField] float plantAvailabilityBoost;
        [SerializeField] float plantDepletionFraction;
        [SerializeField] float damagePulse;
        [SerializeField] float damagePerSecond;
        [SerializeField] float temperatureDelta;
        [SerializeField] int predatorSpawnCount;
        [SerializeField] int predatorRemoveCount;
        [SerializeField] BiomeKind[] affectedBiomes;

        public EnvironmentalEventKind Kind
        {
            get => kind;
            set => kind = value;
        }

        public float DurationSeconds
        {
            get => durationSeconds;
            set => durationSeconds = Mathf.Max(0f, value);
        }

        public float PlantRegenMultiplier
        {
            get => plantRegenMultiplier;
            set => plantRegenMultiplier = Mathf.Max(0f, value);
        }

        public float WaterRechargeMultiplier
        {
            get => waterRechargeMultiplier;
            set => waterRechargeMultiplier = Mathf.Max(0f, value);
        }

        public float PlantAvailabilityBoost
        {
            get => plantAvailabilityBoost;
            set => plantAvailabilityBoost = value;
        }

        public float PlantDepletionFraction
        {
            get => plantDepletionFraction;
            set => plantDepletionFraction = Mathf.Clamp01(value);
        }

        public float DamagePulse
        {
            get => damagePulse;
            set => damagePulse = Mathf.Max(0f, value);
        }

        public float DamagePerSecond
        {
            get => damagePerSecond;
            set => damagePerSecond = Mathf.Max(0f, value);
        }

        public float TemperatureDelta
        {
            get => temperatureDelta;
            set => temperatureDelta = value;
        }

        public int PredatorSpawnCount
        {
            get => predatorSpawnCount;
            set => predatorSpawnCount = Mathf.Max(0, value);
        }

        public int PredatorRemoveCount
        {
            get => predatorRemoveCount;
            set => predatorRemoveCount = Mathf.Max(0, value);
        }

        public BiomeKind[] AffectedBiomes
        {
            get => affectedBiomes;
            set => affectedBiomes = value;
        }

        public EnvironmentalEventDefinition Clone()
        {
            BiomeKind[] biomes = null;
            if (affectedBiomes != null && affectedBiomes.Length > 0)
            {
                biomes = new BiomeKind[affectedBiomes.Length];
                Array.Copy(affectedBiomes, biomes, affectedBiomes.Length);
            }

            return new EnvironmentalEventDefinition
            {
                Kind = kind,
                DurationSeconds = durationSeconds,
                PlantRegenMultiplier = plantRegenMultiplier,
                WaterRechargeMultiplier = waterRechargeMultiplier,
                PlantAvailabilityBoost = plantAvailabilityBoost,
                PlantDepletionFraction = plantDepletionFraction,
                DamagePulse = damagePulse,
                DamagePerSecond = damagePerSecond,
                TemperatureDelta = temperatureDelta,
                PredatorSpawnCount = predatorSpawnCount,
                PredatorRemoveCount = predatorRemoveCount,
                AffectedBiomes = biomes
            };
        }

        public static EnvironmentalEventDefinition Defaults(EnvironmentalEventKind eventKind)
        {
            switch (eventKind)
            {
                case EnvironmentalEventKind.Drought:
                    return new EnvironmentalEventDefinition
                    {
                        Kind = eventKind,
                        DurationSeconds = 30f,
                        PlantRegenMultiplier = 0.25f,
                        WaterRechargeMultiplier = 0.25f,
                        TemperatureDelta = 0.1f
                    };
                case EnvironmentalEventKind.Wildfire:
                    return new EnvironmentalEventDefinition
                    {
                        Kind = eventKind,
                        DurationSeconds = 8f,
                        PlantRegenMultiplier = 0.5f,
                        PlantDepletionFraction = 0.7f,
                        DamagePulse = 40f,
                        DamagePerSecond = 2f
                    };
                case EnvironmentalEventKind.HeatWave:
                    return new EnvironmentalEventDefinition
                    {
                        Kind = eventKind,
                        DurationSeconds = 20f,
                        PlantRegenMultiplier = 0.8f,
                        WaterRechargeMultiplier = 0.7f,
                        DamagePerSecond = 2f,
                        TemperatureDelta = 0.35f
                    };
                case EnvironmentalEventKind.FoodBoom:
                    return new EnvironmentalEventDefinition
                    {
                        Kind = eventKind,
                        DurationSeconds = 20f,
                        PlantRegenMultiplier = 2f,
                        PlantAvailabilityBoost = 12f
                    };
                case EnvironmentalEventKind.DiseasePressure:
                    return new EnvironmentalEventDefinition
                    {
                        Kind = eventKind,
                        DurationSeconds = 16f,
                        DamagePerSecond = 1.5f
                    };
                case EnvironmentalEventKind.PredatorIntroduction:
                    return new EnvironmentalEventDefinition
                    {
                        Kind = eventKind,
                        DurationSeconds = 0f,
                        PredatorSpawnCount = 2
                    };
                case EnvironmentalEventKind.PredatorRemoval:
                    return new EnvironmentalEventDefinition
                    {
                        Kind = eventKind,
                        DurationSeconds = 0f,
                        PredatorRemoveCount = 1
                    };
                default:
                    return Unreachable(eventKind);
            }
        }

        static EnvironmentalEventDefinition Unreachable(EnvironmentalEventKind eventKind)
        {
            throw new ArgumentOutOfRangeException(nameof(eventKind), eventKind, "Unhandled EnvironmentalEventKind.");
        }
    }
}
