using System;

namespace EvoLife.Genetics
{
    /// <summary>
    /// Stable ML-facing contract: normalized genetic features in CanonicalGenomeSchema order.
    /// AI consumes this vector; it must not implement genome operators.
    /// </summary>
    public static class GeneticObservationProvider
    {
        public static int ObservationSize => CanonicalGenomeSchema.TraitCount;

        public static string[] ObservationSchema => CanonicalGenomeSchema.CanonicalNames();

        public static float[] GetObservationVector(Genome genome)
        {
            if (genome == null)
            {
                return new float[ObservationSize];
            }

            return genome.ToNormalizedArray();
        }

        public static void WriteObservations(Genome genome, float[] buffer)
        {
            if (buffer == null || buffer.Length < ObservationSize)
            {
                return;
            }

            var source = GetObservationVector(genome);
            Array.Copy(source, buffer, ObservationSize);
        }
    }
}
