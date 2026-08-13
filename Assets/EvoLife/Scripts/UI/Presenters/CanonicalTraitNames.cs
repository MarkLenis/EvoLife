namespace EvoLife.UI
{
    /// <summary>
    /// Canonical genome trait names in schema v1 order. Presentation lookup only;
    /// Genetics remains the storage owner.
    /// </summary>
    public static class CanonicalTraitNames
    {
        public const string BaseMovementSpeed = "base_movement_speed";
        public const string SprintSpeed = "sprint_speed";
        public const string VisionRange = "vision_range";
        public const string MaximumEnergy = "maximum_energy";
        public const string MetabolismRate = "metabolism_rate";
        public const string BodySize = "body_size";
        public const string Aggression = "aggression";
        public const string ReproductionThreshold = "reproduction_threshold";
        public const string MaximumAge = "maximum_age";

        public static readonly string[] InSchemaOrder =
        {
            BaseMovementSpeed,
            SprintSpeed,
            VisionRange,
            MaximumEnergy,
            MetabolismRate,
            BodySize,
            Aggression,
            ReproductionThreshold,
            MaximumAge
        };
    }
}
