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
        [SerializeField] float baseSprintSpeed = 7f;
        [SerializeField] CreatureVitals vitals;

        float speedMultiplier = 1f;
        float sprintSpeedMultiplier = 1f;
        float sensoryRangeMultiplier = 1f;

        public float MaxSpeed => baseMaxSpeed * speedMultiplier;
        public float SprintSpeed => baseSprintSpeed * sprintSpeedMultiplier;
        public float SensoryRangeMultiplier => sensoryRangeMultiplier;

        public void ApplyPhenotype(IReadOnlyPhenotype phenotype)
        {
            if (phenotype == null)
            {
                return;
            }

            speedMultiplier = phenotype.MaxSpeedMultiplier;
            sprintSpeedMultiplier = phenotype.SprintSpeedMultiplier;
            sensoryRangeMultiplier = phenotype.SensoryRangeMultiplier;

            if (vitals != null)
            {
                vitals.ApplyPhenotypeModifiers(phenotype);
            }
        }
    }
}
