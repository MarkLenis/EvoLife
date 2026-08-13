using UnityEngine;
using EvoLife.Common;

namespace EvoLife.Creatures
{
    /// <summary>
    /// Identity façade on a creature GameObject. Does not own vitals, genetics, or AI.
    /// </summary>
    public sealed class CreatureIdentity : MonoBehaviour, ICreatureIdentity
    {
        [SerializeField] string speciesId = "unspecified";
        [SerializeField] CreatureRole role = CreatureRole.Herbivore;

        CreatureId id;

        public CreatureId Id => id;
        public CreatureRole Role => role;
        public string SpeciesId => speciesId;

        public void Assign(CreatureId creatureId, string species, CreatureRole creatureRole)
        {
            id = creatureId;
            speciesId = species;
            role = creatureRole;
        }
    }
}
