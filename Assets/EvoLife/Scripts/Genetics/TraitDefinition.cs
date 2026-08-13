namespace EvoLife.Genetics
{
    /// <summary>
    /// Metadata for a single genetic trait. Bounds live here — not in Creatures or AI.
    /// </summary>
    public readonly struct TraitDefinition
    {
        public TraitDefinition(
            TraitId id,
            string canonicalName,
            float hardMin,
            float hardMax,
            float generationMin,
            float generationMax,
            float defaultValue,
            float mutationMagnitude,
            string description)
        {
            Id = id;
            CanonicalName = canonicalName;
            HardMin = hardMin;
            HardMax = hardMax;
            GenerationMin = generationMin;
            GenerationMax = generationMax;
            Default = defaultValue;
            MutationMagnitude = mutationMagnitude;
            Description = description;
        }

        public TraitId Id { get; }
        public string CanonicalName { get; }
        public float HardMin { get; }
        public float HardMax { get; }
        public float GenerationMin { get; }
        public float GenerationMax { get; }
        public float Default { get; }
        public float MutationMagnitude { get; }
        public string Description { get; }

        public float Clamp(float value) =>
            value < HardMin ? HardMin : value > HardMax ? HardMax : value;

        public float Normalize(float value)
        {
            var span = HardMax - HardMin;
            if (span <= 0f)
            {
                return 0f;
            }

            var normalized = (Clamp(value) - HardMin) / span;
            return normalized < 0f ? 0f : normalized > 1f ? 1f : normalized;
        }

        public float SampleGeneration(System.Random random)
        {
            var t = (float)random.NextDouble();
            return GenerationMin + t * (GenerationMax - GenerationMin);
        }
    }
}
