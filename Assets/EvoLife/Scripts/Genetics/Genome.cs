using System;
using UnityEngine;

namespace EvoLife.Genetics
{
    /// <summary>
    /// Ordered gene values for one individual. Genetics owns mutation/crossover over this data.
    /// </summary>
    [Serializable]
    public sealed class Genome
    {
        [SerializeField] float[] genes;

        public Genome(int geneCount)
        {
            genes = new float[Mathf.Max(1, geneCount)];
            for (var i = 0; i < genes.Length; i++)
            {
                genes[i] = 0.5f;
            }
        }

        public Genome(float[] source)
        {
            if (source == null || source.Length == 0)
            {
                genes = new[] { 0.5f };
                return;
            }

            genes = new float[source.Length];
            Array.Copy(source, genes, source.Length);
        }

        public int Length => genes.Length;

        public float GetGene(int index) => genes[index];

        public void SetGene(int index, float value)
        {
            genes[index] = Mathf.Clamp01(value);
        }

        public float[] ToArray()
        {
            var copy = new float[genes.Length];
            Array.Copy(genes, copy, genes.Length);
            return copy;
        }

        public Genome Clone() => new Genome(genes);
    }
}
