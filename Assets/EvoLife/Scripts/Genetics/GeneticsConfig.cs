using System;

namespace EvoLife.Genetics
{
    public enum CrossoverMode
    {
        Average = 0,
        RandomParent = 1,
        Weighted = 2
    }

    public sealed class CrossoverConfig
    {
        public CrossoverConfig(CrossoverMode mode = CrossoverMode.Weighted, float parentAWeight = 0.5f)
        {
            if (parentAWeight < 0f || parentAWeight > 1f)
            {
                throw new ArgumentOutOfRangeException(nameof(parentAWeight), "parentAWeight must be in [0, 1].");
            }

            Mode = mode;
            ParentAWeight = parentAWeight;
        }

        public CrossoverMode Mode { get; }
        public float ParentAWeight { get; }

        public static CrossoverConfig Default { get; } = new CrossoverConfig();
    }

    public sealed class MutationConfig
    {
        public MutationConfig(float probability = 0.15f, float magnitudeScale = 1f)
        {
            if (probability < 0f || probability > 1f)
            {
                throw new ArgumentOutOfRangeException(nameof(probability), "probability must be in [0, 1].");
            }

            if (magnitudeScale < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(magnitudeScale), "magnitudeScale must be non-negative.");
            }

            Probability = probability;
            MagnitudeScale = magnitudeScale;
        }

        public float Probability { get; }
        public float MagnitudeScale { get; }

        public static MutationConfig Default { get; } = new MutationConfig();
        public static MutationConfig Disabled { get; } = new MutationConfig(probability: 0f, magnitudeScale: 0f);
    }

    public sealed class GeneticsConfig
    {
        public GeneticsConfig(CrossoverConfig crossover = null, MutationConfig mutation = null)
        {
            Crossover = crossover ?? CrossoverConfig.Default;
            Mutation = mutation ?? MutationConfig.Default;
        }

        public CrossoverConfig Crossover { get; }
        public MutationConfig Mutation { get; }

        public static GeneticsConfig Default { get; } = new GeneticsConfig();

        public static GeneticsConfig NoMutation() =>
            new GeneticsConfig(CrossoverConfig.Default, MutationConfig.Disabled);
    }
}
