using System.Collections.Generic;

namespace EvoLife.Analytics
{
    /// <summary>
    /// Mean/variance helpers that never divide by zero.
    /// Empty or single-value sets report 0 rather than throwing.
    /// </summary>
    public static class TraitStatistics
    {
        public static float Mean(IList<float> values)
        {
            if (values == null || values.Count == 0)
            {
                return 0f;
            }

            var sum = 0f;
            for (var i = 0; i < values.Count; i++)
            {
                sum += values[i];
            }

            return sum / values.Count;
        }

        public static float Variance(IList<float> values)
        {
            if (values == null || values.Count <= 1)
            {
                return 0f;
            }

            var mean = Mean(values);
            var acc = 0f;
            for (var i = 0; i < values.Count; i++)
            {
                var delta = values[i] - mean;
                acc += delta * delta;
            }

            return acc / values.Count;
        }

        public static float Min(IList<float> values)
        {
            if (values == null || values.Count == 0)
            {
                return 0f;
            }

            var min = values[0];
            for (var i = 1; i < values.Count; i++)
            {
                if (values[i] < min)
                {
                    min = values[i];
                }
            }

            return min;
        }

        public static float Max(IList<float> values)
        {
            if (values == null || values.Count == 0)
            {
                return 0f;
            }

            var max = values[0];
            for (var i = 1; i < values.Count; i++)
            {
                if (values[i] > max)
                {
                    max = values[i];
                }
            }

            return max;
        }
    }
}
