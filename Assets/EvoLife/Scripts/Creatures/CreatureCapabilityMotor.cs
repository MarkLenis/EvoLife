using UnityEngine;
using EvoLife.Common;

namespace EvoLife.Creatures
{
    /// <summary>
    /// Applies phenotype multipliers to creature systems. Thin adapter — no genetic logic here.
    /// </summary>
    public sealed class CreatureCapabilityMotor : MonoBehaviour
    {
        [SerializeField] float baseMaxSpeed = 3.5f;
        [SerializeField] CreatureVitals vitals;

        float speedMultiplier = 1f;
        float sensoryRangeMultiplier = 1f;

        public float MaxSpeed => baseMaxSpeed * speedMultiplier;
        public float SensoryRangeMultiplier => sensoryRangeMultiplier;

        public void ApplyPhenotype(IReadOnlyPhenotype phenotype)
        {
            if (phenotype == null)
            {
                return;
            }

            speedMultiplier = phenotype.MaxSpeedMultiplier;
            sensoryRangeMultiplier = phenotype.SensoryRangeMultiplier;

            if (vitals != null)
            {
                vitals.ApplyMetabolismMultiplier(phenotype.MetabolismMultiplier);
            }
        }
    }
}
