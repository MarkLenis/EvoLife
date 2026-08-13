using System;

namespace EvoLife.Creatures
{
    /// <summary>
    /// Fixed-timestep driver for biological simulation. Keeps metabolism independent of Unity frame callbacks.
    /// </summary>
    public sealed class BiologySimulationClock
    {
        public BiologySimulationClock(float fixedDeltaTime = 0.25f)
        {
            if (fixedDeltaTime <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(fixedDeltaTime));
            }

            FixedDeltaTime = fixedDeltaTime;
        }

        public float FixedDeltaTime { get; }

        public void AccumulateAndStep(CreatureBiology biology, ref float timeDebt, float deltaTime, ActivityLevel activity)
        {
            if (biology == null)
            {
                throw new ArgumentNullException(nameof(biology));
            }

            if (deltaTime <= 0f)
            {
                return;
            }

            timeDebt += deltaTime;

            while (timeDebt >= FixedDeltaTime)
            {
                biology.Tick(FixedDeltaTime, activity);
                timeDebt -= FixedDeltaTime;

                if (!biology.IsAlive)
                {
                    timeDebt = 0f;
                    break;
                }
            }
        }
    }
}
