using UnityEngine;

namespace EvoLife.Presentation
{
    /// <summary>
    /// Maps canonical <c>body_size</c> phenotype to a visual-only scale.
    /// Does not modify root colliders, motors, or biology.
    /// </summary>
    public static class PhenotypeVisualScale
    {
        public const float Minimum = 0.75f;
        public const float Maximum = 1.35f;
        public const float Neutral = 1f;

        public static float ForBodySize(float bodySizeMultiplier)
        {
            if (float.IsNaN(bodySizeMultiplier) || float.IsInfinity(bodySizeMultiplier))
            {
                return Neutral;
            }

            if (bodySizeMultiplier < Minimum)
            {
                return Minimum;
            }

            return bodySizeMultiplier > Maximum ? Maximum : bodySizeMultiplier;
        }
    }
}
