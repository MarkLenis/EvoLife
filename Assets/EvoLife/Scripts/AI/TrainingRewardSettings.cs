using System;
using UnityEngine;

namespace EvoLife.AI
{
    /// <summary>
    /// Tunable starter reward weights. Values are experimental development defaults,
    /// not claimed optima. Keep shaping light to avoid exploits.
    /// </summary>
    [Serializable]
    public sealed class TrainingRewardSettings
    {
        [SerializeField] float aliveReward = 0.001f;
        [SerializeField] float deathPenalty = -1f;
        [SerializeField] float hungerReliefScale = 0.4f;
        [SerializeField] float thirstReliefScale = 0.4f;
        [SerializeField] float healthLossScale = 0.2f;
        [SerializeField] float energyMaintenanceScale = 0.0005f;
        [SerializeField] float criticalNeedPenalty = 0.004f;
        [SerializeField] float criticalNeedThreshold = 0.85f;

        public float AliveReward
        {
            get => aliveReward;
            set => aliveReward = value;
        }

        public float DeathPenalty
        {
            get => deathPenalty;
            set => deathPenalty = value;
        }

        public float HungerReliefScale
        {
            get => hungerReliefScale;
            set => hungerReliefScale = value;
        }

        public float ThirstReliefScale
        {
            get => thirstReliefScale;
            set => thirstReliefScale = value;
        }

        public float HealthLossScale
        {
            get => healthLossScale;
            set => healthLossScale = value;
        }

        public float EnergyMaintenanceScale
        {
            get => energyMaintenanceScale;
            set => energyMaintenanceScale = value;
        }

        public float CriticalNeedPenalty
        {
            get => criticalNeedPenalty;
            set => criticalNeedPenalty = value;
        }

        public float CriticalNeedThreshold
        {
            get => criticalNeedThreshold;
            set => criticalNeedThreshold = value;
        }

        public static TrainingRewardSettings CreateDefault() => new TrainingRewardSettings();
    }
}
