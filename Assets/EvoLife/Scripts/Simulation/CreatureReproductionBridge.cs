using UnityEngine;
using EvoLife.Common;

namespace EvoLife.Simulation
{
    /// <summary>
    /// Simulation-owned request target on a creature. AI finds this via
    /// <see cref="IReproductionRequestHandler"/>; this class does not evaluate eligibility.
    /// </summary>
    public sealed class CreatureReproductionBridge : MonoBehaviour, IReproductionRequestHandler
    {
        ReproductionSystem system;
        CreatureId id;

        public CreatureId Id => id;

        public void Bind(ReproductionSystem owner, CreatureId creatureId)
        {
            system = owner;
            id = creatureId;
        }

        public void HandleReproduceRequest()
        {
            system?.TryReproduce(id);
        }
    }
}
