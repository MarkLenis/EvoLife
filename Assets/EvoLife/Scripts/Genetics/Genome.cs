using System;
using UnityEngine;

namespace EvoLife.Genetics
{
    /// <summary>
    /// Canonical genome: named trait values with schema v1 bounds.
    /// Storage order matches <see cref="CanonicalGenomeSchema"/> / <see cref="TraitId"/> ordinals.
    /// Access traits by <see cref="TraitId"/> — do not hard-code magic indices in other modules.
    /// </summary>
    [Serializable]
    public sealed class Genome
    {
        [SerializeField] int schemaVersion = CanonicalGenomeSchema.Version;
        [SerializeField] float[] genes;

        public Genome()
            : this(CanonicalGenomeSchema.DefaultValues())
        {
        }

        public Genome(float[] source)
        {
            schemaVersion = CanonicalGenomeSchema.Version;
            genes = CreateClampedBuffer(source);
        }

        public int SchemaVersion => schemaVersion;
        public int Length => genes.Length;

        public float Get(TraitId id) => genes[(int)id];

        public void Set(TraitId id, float value)
        {
            var trait = CanonicalGenomeSchema.Get(id);
            genes[(int)id] = trait.Clamp(value);
        }

        public float GetNormalized(TraitId id) =>
            CanonicalGenomeSchema.Get(id).Normalize(Get(id));

        public float[] ToArray()
        {
            var copy = new float[genes.Length];
            Array.Copy(genes, copy, genes.Length);
            return copy;
        }

        /// <summary>
        /// Normalized [0, 1] vector in canonical schema order for future ML observations.
        /// </summary>
        public float[] ToNormalizedArray()
        {
            var normalized = new float[genes.Length];
            for (var i = 0; i < genes.Length; i++)
            {
                normalized[i] = CanonicalGenomeSchema.Get(i).Normalize(genes[i]);
            }

            return normalized;
        }

        public Genome Clone() => new Genome(genes);

        public static Genome CreateDefault() => new Genome(CanonicalGenomeSchema.DefaultValues());

        public static Genome FromTraitValues(params (TraitId id, float value)[] values)
        {
            var genome = CreateDefault();
            if (values == null)
            {
                return genome;
            }

            for (var i = 0; i < values.Length; i++)
            {
                genome.Set(values[i].id, values[i].value);
            }

            return genome;
        }

        static float[] CreateClampedBuffer(float[] source)
        {
            var buffer = CanonicalGenomeSchema.DefaultValues();
            if (source == null)
            {
                return buffer;
            }

            var count = Math.Min(source.Length, buffer.Length);
            for (var i = 0; i < count; i++)
            {
                buffer[i] = CanonicalGenomeSchema.Get(i).Clamp(source[i]);
            }

            return buffer;
        }
    }
}
