using EvoLife.Common;
using EvoLife.Creatures;
using UnityEngine;

namespace EvoLife.AI
{
    /// <summary>
    /// Placeholder observation pack: normalized vitals only. Expand with sensors later.
    /// </summary>
    public sealed class VitalObservationSource : IObservationSource
    {
        readonly CreatureVitals vitals;

        public VitalObservationSource(CreatureVitals vitals) => this.vitals = vitals;

        public int ObservationSize => 5;

        public void WriteObservations(float[] buffer)
        {
            if (buffer == null || buffer.Length < ObservationSize || vitals == null)
            {
                return;
            }

            buffer[0] = Normalize(vitals.Health, vitals.MaxHealth);
            buffer[1] = Normalize(vitals.Hunger, 100f);
            buffer[2] = Normalize(vitals.Thirst, 100f);
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
                          - Normalize(vitals.Hunger, 100f) * 0.25f
                          - Normalize(vitals.Thirst, 100f) * 0.25f
                          + Normalize(vitals.Energy, vitals.MaxEnergy) * 0.1f;

            return episodeEnded ? comfort : comfort * 0.01f;
        }

        static float Normalize(float value, float max) =>
            max <= 0f ? 0f : Mathf.Clamp01(value / max);
    }
}
