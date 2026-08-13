using EvoLife.Common;
using EvoLife.Creatures;

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

            // Values are already roughly 0–100 in the skeleton; normalize lightly.
            buffer[0] = vitals.Health / 100f;
            buffer[1] = vitals.Hunger / 100f;
            buffer[2] = vitals.Thirst / 100f;
            buffer[3] = vitals.Energy / 100f;
            buffer[4] = vitals.Age / 100f;
        }
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
                          - (vitals.Hunger / 100f) * 0.25f
                          - (vitals.Thirst / 100f) * 0.25f
                          + (vitals.Energy / 100f) * 0.1f;

            return episodeEnded ? comfort : comfort * 0.01f;
        }
    }
}
