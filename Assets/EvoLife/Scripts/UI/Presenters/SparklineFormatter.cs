using System.Globalization;
using System.Text;

namespace EvoLife.UI
{
    /// <summary>
    /// Compact sparkline text for sampled chart series. Not redrawn unless samples change.
    /// </summary>
    public static class SparklineFormatter
    {
        const string Blocks = "▁▂▃▄▅▆▇█";

        public static string Format(float[] chronological, int count)
        {
            if (chronological == null || count <= 0)
            {
                return "—";
            }

            var length = count < chronological.Length ? count : chronological.Length;
            if (length <= 0)
            {
                return "—";
            }

            var min = chronological[0];
            var max = chronological[0];
            for (var i = 1; i < length; i++)
            {
                var value = chronological[i];
                if (value < min)
                {
                    min = value;
                }

                if (value > max)
                {
                    max = value;
                }
            }

            var range = max - min;
            var builder = new StringBuilder(length);
            for (var i = 0; i < length; i++)
            {
                int bucket;
                if (range <= 0.0001f)
                {
                    bucket = 0;
                }
                else
                {
                    var t = (chronological[i] - min) / range;
                    if (t < 0f)
                    {
                        t = 0f;
                    }
                    else if (t > 1f)
                    {
                        t = 1f;
                    }

                    bucket = (int)(t * (Blocks.Length - 1) + 0.5f);
                    if (bucket >= Blocks.Length)
                    {
                        bucket = Blocks.Length - 1;
                    }
                }

                builder.Append(Blocks[bucket]);
            }

            return builder.ToString();
        }

        public static string FormatWithLatest(float[] chronological, int count)
        {
            var spark = Format(chronological, count);
            if (count <= 0 || chronological == null)
            {
                return spark;
            }

            var latest = chronological[count - 1];
            return spark + "  " + latest.ToString("0.##", CultureInfo.InvariantCulture);
        }
    }
}
