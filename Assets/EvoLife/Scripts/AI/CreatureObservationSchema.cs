using EvoLife.Genetics;

namespace EvoLife.AI
{
    /// <summary>
    /// Explicit, versioned observation layout for EvoLife creatures.
    /// Vector size is not implicit: <see cref="Size"/> and <see cref="Names"/> are the contract
    /// consumed by ML-Agents, Training configs, and EditMode tests.
    ///
    /// Layout v2 (length = 31), all values normalized unless noted:
    /// <list type="number">
    /// <item>health [0,1]</item>
    /// <item>hunger [0,1]</item>
    /// <item>thirst [0,1]</item>
    /// <item>energy [0,1]</item>
    /// <item>age [0,1]</item>
    /// <item>own_role (0 herbivore, 1 predator)</item>
    /// <item>genetics[0..8] CanonicalGenomeSchema order, [0,1]</item>
    /// <item>nearest food dir X [-1,1] (agent-local)</item>
    /// <item>nearest food dir Z [-1,1]</item>
    /// <item>nearest food distance [0,1]</item>
    /// <item>nearest food present (0/1)</item>
    /// <item>nearest water dir X [-1,1]</item>
    /// <item>nearest water dir Z [-1,1]</item>
    /// <item>nearest water distance [0,1]</item>
    /// <item>nearest water present (0/1)</item>
    /// <item>nearest herbivore dir X [-1,1]</item>
    /// <item>nearest herbivore dir Z [-1,1]</item>
    /// <item>nearest herbivore distance [0,1]</item>
    /// <item>nearest herbivore present (0/1)</item>
    /// <item>nearest predator dir X [-1,1]</item>
    /// <item>nearest predator dir Z [-1,1]</item>
    /// <item>nearest predator distance [0,1]</item>
    /// <item>nearest predator present (0/1)</item>
    /// </list>
    /// Missing optional sensors write zeros for their block.
    /// Herbivore and predator channels are independent: a nearer same-role creature
    /// cannot hide the other role.
    /// </summary>
    public static class CreatureObservationSchema
    {
        public const int Version = 2;

        public const int VitalCount = 5;
        public const int RoleCount = 1;

        /// <summary>
        /// Must equal <see cref="CanonicalGenomeSchema.TraitCount"/>. Tests fail if genetics layout drifts.
        /// </summary>
        public const int GeneticCount = 9;

        public const int ResourceChannelCount = 4;
        public const int ResourceKindCount = 2;
        public const int ResourceCount = ResourceChannelCount * ResourceKindCount;
        public const int NearbyRoleChannelCount = 2;
        public const int NearbyCreatureCount = ResourceChannelCount * NearbyRoleChannelCount;

        public const int Size =
            VitalCount + RoleCount + GeneticCount + ResourceCount + NearbyCreatureCount;

        public const int IndexHealth = 0;
        public const int IndexHunger = 1;
        public const int IndexThirst = 2;
        public const int IndexEnergy = 3;
        public const int IndexAge = 4;
        public const int IndexRole = 5;
        public const int IndexGenetics = 6;
        public const int IndexFood = IndexGenetics + GeneticCount;
        public const int IndexWater = IndexFood + ResourceChannelCount;
        public const int IndexHerbivore = IndexWater + ResourceChannelCount;
        public const int IndexPredator = IndexHerbivore + ResourceChannelCount;

        public const int OffsetDirX = 0;
        public const int OffsetDirZ = 1;
        public const int OffsetDistance = 2;
        public const int OffsetPresent = 3;

        /// <summary>
        /// Default sensory radius (world units) when phenotype/genome vision is unavailable.
        /// Matches CanonicalGenomeSchema vision_range default.
        /// </summary>
        public const float DefaultSenseRange = 12f;

        public static readonly string[] Names =
        {
            "health",
            "hunger",
            "thirst",
            "energy",
            "age",
            "own_role",
            "gene_base_movement_speed",
            "gene_sprint_speed",
            "gene_vision_range",
            "gene_maximum_energy",
            "gene_metabolism_rate",
            "gene_body_size",
            "gene_aggression",
            "gene_reproduction_threshold",
            "gene_maximum_age",
            "nearest_food_dir_x",
            "nearest_food_dir_z",
            "nearest_food_distance",
            "nearest_food_present",
            "nearest_water_dir_x",
            "nearest_water_dir_z",
            "nearest_water_distance",
            "nearest_water_present",
            "nearest_herbivore_dir_x",
            "nearest_herbivore_dir_z",
            "nearest_herbivore_distance",
            "nearest_herbivore_present",
            "nearest_predator_dir_x",
            "nearest_predator_dir_z",
            "nearest_predator_distance",
            "nearest_predator_present"
        };

        public static void ValidateAgainstGenetics()
        {
            if (GeneticCount != CanonicalGenomeSchema.TraitCount)
            {
                throw new System.InvalidOperationException(
                    "CreatureObservationSchema.GeneticCount must equal CanonicalGenomeSchema.TraitCount.");
            }

            if (Names.Length != Size)
            {
                throw new System.InvalidOperationException(
                    "CreatureObservationSchema.Names length must equal Size.");
            }

            if (Size != 31)
            {
                throw new System.InvalidOperationException(
                    "CreatureObservationSchema v2 Size must be 31.");
            }
        }
    }
}
