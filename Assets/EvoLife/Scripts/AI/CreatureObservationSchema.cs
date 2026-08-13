using EvoLife.Genetics;

namespace EvoLife.AI
{
    /// <summary>
    /// Explicit, versioned observation layout for EvoLife creatures.
    /// Vector size is not implicit: <see cref="Size"/> and <see cref="Names"/> are the contract
    /// consumed by ML-Agents, Training configs, and EditMode tests.
    ///
    /// Layout v1 (length = 28), all values normalized unless noted:
    /// <list type="number">
    /// <item>health [0,1]</item>
    /// <item>hunger [0,1]</item>
    /// <item>thirst [0,1]</item>
    /// <item>energy [0,1]</item>
    /// <item>age [0,1]</item>
    /// <item>role (0 herbivore, 1 predator)</item>
    /// <item>genetics[0..8] CanonicalGenomeSchema order, [0,1]</item>
    /// <item>nearest food dir X [-1,1] (agent-local)</item>
    /// <item>nearest food dir Z [-1,1]</item>
    /// <item>nearest food distance [0,1]</item>
    /// <item>nearest food present (0/1)</item>
    /// <item>nearest water dir X [-1,1]</item>
    /// <item>nearest water dir Z [-1,1]</item>
    /// <item>nearest water distance [0,1]</item>
    /// <item>nearest water present (0/1)</item>
    /// <item>nearest other creature dir X [-1,1]</item>
    /// <item>nearest other creature dir Z [-1,1]</item>
    /// <item>nearest other creature distance [0,1]</item>
    /// <item>nearest other creature role (0/1)</item>
    /// <item>nearest other creature present (0/1)</item>
    /// </list>
    /// Missing optional sensors write zeros for their block.
    /// </summary>
    public static class CreatureObservationSchema
    {
        public const int Version = 1;

        public const int VitalCount = 5;
        public const int RoleCount = 1;

        /// <summary>
        /// Must equal <see cref="CanonicalGenomeSchema.TraitCount"/>. Tests fail if genetics layout drifts.
        /// </summary>
        public const int GeneticCount = 9;

        public const int ResourceChannelCount = 4;
        public const int ResourceKindCount = 2;
        public const int ResourceCount = ResourceChannelCount * ResourceKindCount;
        public const int NearbyCreatureCount = 5;

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
        public const int IndexNearbyCreature = IndexWater + ResourceChannelCount;

        public const int OffsetDirX = 0;
        public const int OffsetDirZ = 1;
        public const int OffsetDistance = 2;
        public const int OffsetPresent = 3;
        public const int OffsetCreatureRole = 3;
        public const int OffsetCreaturePresent = 4;

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
            "role",
            "gene_base_movement_speed",
            "gene_sprint_speed",
            "gene_vision_range",
            "gene_maximum_energy",
            "gene_metabolism_rate",
            "gene_body_size",
            "gene_aggression",
            "gene_reproduction_threshold",
            "gene_maximum_age",
            "food_dir_x",
            "food_dir_z",
            "food_distance",
            "food_present",
            "water_dir_x",
            "water_dir_z",
            "water_distance",
            "water_present",
            "nearby_dir_x",
            "nearby_dir_z",
            "nearby_distance",
            "nearby_role",
            "nearby_present"
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
        }
    }
}
