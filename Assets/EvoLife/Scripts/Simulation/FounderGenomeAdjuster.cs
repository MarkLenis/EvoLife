using EvoLife.Common;
using EvoLife.Genetics;

namespace EvoLife.Simulation
{
    /// <summary>
    /// Optional founder-genome bias used by named scenarios. Does not change the schema.
    /// </summary>
    public static class FounderGenomeAdjuster
    {
        public static Genome Apply(Genome genome, CreatureRole role, float predatorSpeedBias)
        {
            if (genome == null || predatorSpeedBias == 0f || role != CreatureRole.Predator)
            {
                return genome;
            }

            var clone = genome.Clone();
            clone.Set(TraitId.BaseMovementSpeed, clone.Get(TraitId.BaseMovementSpeed) + predatorSpeedBias);
            clone.Set(TraitId.SprintSpeed, clone.Get(TraitId.SprintSpeed) + predatorSpeedBias);
            return clone;
        }
    }
}
