using UnityEngine;
using EvoLife.Common;

namespace EvoLife.Creatures
{
    /// <summary>
    /// Identity façade on a creature GameObject. Does not own vitals, genetics, or AI.
    /// </summary>
    public sealed class CreatureIdentity : MonoBehaviour, ICreatureIdentity, ICreatureLineage
    {
        [SerializeField] string speciesId = "unspecified";
        [SerializeField] CreatureRole role = CreatureRole.Herbivore;
        [SerializeField] int generation;

        CreatureId id;
        CreatureId? parentA;
        CreatureId? parentB;

        public CreatureId Id => id;
        public CreatureRole Role => role;
        public string SpeciesId => speciesId;
        public int Generation => generation;
        public CreatureId? ParentA => parentA;
        public CreatureId? ParentB => parentB;

        public void Assign(CreatureId creatureId, string species, CreatureRole creatureRole)
        {
            Assign(creatureId, species, creatureRole, generationNumber: 0, parentAId: null, parentBId: null);
        }

        public void Assign(
            CreatureId creatureId,
            string species,
            CreatureRole creatureRole,
            int generationNumber,
            CreatureId? parentAId,
            CreatureId? parentBId)
        {
            id = creatureId;
            speciesId = species;
            role = creatureRole;
            generation = generationNumber < 0 ? 0 : generationNumber;
            parentA = parentAId;
            parentB = parentBId;
        }
    }
}
