using EvoLife.Common;
using UnityEngine;

namespace EvoLife.AI
{
    /// <summary>
    /// Placeholder observation pack: normalized vitals only. Expand with sensors later.
    /// Hunger/thirst use per-creature capacities from <see cref="IReadOnlyVitalState"/>.
    /// </summary>
    public sealed class VitalObservationSource : IObservationSource
    {
        readonly IReadOnlyVitalState vitals;

        public VitalObservationSource(IReadOnlyVitalState vitals) => this.vitals = vitals;

        public int ObservationSize => CreatureObservationSchema.VitalCount;

        public void WriteObservations(float[] buffer)
        {
            if (buffer == null || buffer.Length < ObservationSize || vitals == null)
            {
                return;
            }

            buffer[0] = Normalize(vitals.Health, vitals.MaxHealth);
            buffer[1] = Normalize(vitals.Hunger, vitals.MaxHunger);
            buffer[2] = Normalize(vitals.Thirst, vitals.MaxThirst);
            buffer[3] = Normalize(vitals.Energy, vitals.MaxEnergy);
            buffer[4] = Normalize(vitals.Age, vitals.MaxAge);
        }

        static float Normalize(float value, float max) =>
            max <= 0f ? 0f : Mathf.Clamp01(value / max);
    }

    /// <summary>
    /// Survival-oriented stub reward. Do not tune for training yet — architecture only.
    /// </summary>
    public sealed class SurvivalRewardCalculator : IRewardCalculator
    {
        public float CalculateReward(IReadOnlyVitalState vitals, bool episodeEnded)
        {
            if (vitals == null)
            {
                return 0f;
            }

            if (!vitals.IsAlive)
            {
                return -1f;
            }

            var comfort = 1f
                          - Normalize(vitals.Hunger, vitals.MaxHunger) * 0.25f
                          - Normalize(vitals.Thirst, vitals.MaxThirst) * 0.25f
                          + Normalize(vitals.Energy, vitals.MaxEnergy) * 0.1f;

            return episodeEnded ? comfort : comfort * 0.01f;
        }

        static float Normalize(float value, float max) =>
            max <= 0f ? 0f : Mathf.Clamp01(value / max);
    }
}
