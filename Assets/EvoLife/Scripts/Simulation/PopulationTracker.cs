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

        public int HerbivoreCount => herbivores.Count;
        public int PredatorCount => predators.Count;
        public int TotalAlive => herbivores.Count + predators.Count;

        public void Register(CreatureId id, CreatureRole role)
        {
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
        }

        public void Unregister(CreatureId id)
        {
            herbivores.Remove(id);
            predators.Remove(id);
        }
    }
}
