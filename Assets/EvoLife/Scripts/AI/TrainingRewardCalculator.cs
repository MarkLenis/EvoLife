using EvoLife.Common;
using UnityEngine;

namespace EvoLife.AI
{
    /// <summary>
    /// Simple, configurable training reward. Intentionally light shaping:
    /// small alive bonus, relief when hunger/thirst drop, death terminates with a penalty.
    /// Critical need penalty keeps a stationary starving agent from farming alive-reward.
    /// </summary>
    public sealed class TrainingRewardCalculator : IEpisodeRewardCalculator
    {
        readonly TrainingRewardSettings settings;

        float previousHunger = -1f;
        float previousThirst = -1f;
        float previousHealth = -1f;
        bool hasPrevious;

        public TrainingRewardCalculator(TrainingRewardSettings settings = null)
        {
            this.settings = settings ?? TrainingRewardSettings.CreateDefault();
        }

        public TrainingRewardSettings Settings => settings;

        public void OnEpisodeBegin()
        {
            hasPrevious = false;
            previousHunger = -1f;
            previousThirst = -1f;
            previousHealth = -1f;
        }

        public float CalculateReward(IReadOnlyVitalState vitals, bool episodeEnded)
        {
            _ = episodeEnded;
            return Evaluate(vitals).Reward;
        }

        public RewardSignal Evaluate(IReadOnlyVitalState vitals)
        {
            if (vitals == null)
            {
                return RewardSignal.None;
            }

            if (!vitals.IsAlive)
            {
                hasPrevious = false;
                return new RewardSignal(settings.DeathPenalty, terminateEpisode: true);
            }

            var hunger = ObservationMath.Normalize(vitals.Hunger, vitals.MaxHunger);
            var thirst = ObservationMath.Normalize(vitals.Thirst, vitals.MaxThirst);
            var health = ObservationMath.Normalize(vitals.Health, vitals.MaxHealth);
            var energy = ObservationMath.Normalize(vitals.Energy, vitals.MaxEnergy);

            var reward = settings.AliveReward;
            reward += energy * settings.EnergyMaintenanceScale;

            if (hunger >= settings.CriticalNeedThreshold || thirst >= settings.CriticalNeedThreshold)
            {
                reward -= settings.CriticalNeedPenalty;
            }

            if (hasPrevious)
            {
                var hungerRelief = Mathf.Max(0f, previousHunger - hunger);
                var thirstRelief = Mathf.Max(0f, previousThirst - thirst);
                var healthLoss = Mathf.Max(0f, previousHealth - health);

                reward += hungerRelief * settings.HungerReliefScale;
                reward += thirstRelief * settings.ThirstReliefScale;
                reward -= healthLoss * settings.HealthLossScale;
            }

            previousHunger = hunger;
            previousThirst = thirst;
            previousHealth = health;
            hasPrevious = true;

            return new RewardSignal(reward, terminateEpisode: false);
        }
    }
}
