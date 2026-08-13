using System.Collections.Generic;
using UnityEngine;
using EvoLife.Common;

namespace EvoLife.Simulation
{
    /// <summary>
    /// Tracks living creatures by role. Spawning/despawning notify this component.
    /// </summary>
    public sealed class PopulationTracker : MonoBehaviour, IPopulationSnapshot
    {
        readonly HashSet<CreatureId> herbivores = new HashSet<CreatureId>();
        readonly HashSet<CreatureId> predators = new HashSet<CreatureId>();
        int births;
        int deaths;

        public int HerbivoreCount => herbivores.Count;
        public int PredatorCount => predators.Count;
        public int TotalAlive => herbivores.Count + predators.Count;
        public int Births => births;
        public int Deaths => deaths;

        public int CountFor(CreatureRole role)
        {
            switch (role)
            {
                case CreatureRole.Herbivore:
                    return HerbivoreCount;
                case CreatureRole.Predator:
                    return PredatorCount;
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(role), role, "Unhandled CreatureRole.");
            }
        }

        public void Register(CreatureId id, CreatureRole role)
        {
            var wasAlive = herbivores.Contains(id) || predators.Contains(id);
            if (role == CreatureRole.Predator)
            {
                predators.Add(id);
                herbivores.Remove(id);
            }
            else
            {
                herbivores.Add(id);
                predators.Remove(id);
            }

            if (!wasAlive)
            {
                births++;
            }
        }

        public void Unregister(CreatureId id)
        {
            var wasAlive = herbivores.Remove(id) | predators.Remove(id);
            if (wasAlive)
            {
                deaths++;
            }
        }
    }
}
