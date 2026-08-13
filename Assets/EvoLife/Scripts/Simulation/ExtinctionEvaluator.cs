using EvoLife.Common;

namespace EvoLife.Simulation
{
    /// <summary>
    /// Derives extinction from live population counts. Not a fitness score.
    /// </summary>
    public static class ExtinctionEvaluator
    {
        public static ExtinctionState Evaluate(IPopulationSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return ExtinctionState.EcosystemExtinct;
            }

            var herbivores = snapshot.HerbivoreCount;
            var predators = snapshot.PredatorCount;
            if (herbivores <= 0 && predators <= 0)
            {
                return ExtinctionState.EcosystemExtinct;
            }

            if (herbivores <= 0)
            {
                return ExtinctionState.HerbivoresExtinct;
            }

            if (predators <= 0)
            {
                return ExtinctionState.PredatorsExtinct;
            }

            return ExtinctionState.None;
        }
    }
}
