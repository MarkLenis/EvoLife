using System;

namespace EvoLife.UI
{
    /// <summary>
    /// Presentation-only bounded history. Does not own analytics or experiment control.
    /// </summary>
    public sealed class ChartRingBuffer
    {
        readonly float[] values;
        int count;
        int next;

        public ChartRingBuffer(int capacity)
        {
            if (capacity < 1)
            {
                capacity = 1;
            }

            values = new float[capacity];
        }

        public int Capacity => values.Length;

        public int Count => count;

        public void Push(float value)
        {
            values[next] = value;
            next = (next + 1) % values.Length;
            if (count < values.Length)
            {
                count++;
            }
        }

        public void Clear()
        {
            count = 0;
            next = 0;
            Array.Clear(values, 0, values.Length);
        }

        /// <summary>
        /// Copies oldest-to-newest samples into <paramref name="destination"/>.
        /// Returns the number of values written.
        /// </summary>
        public int CopyChronological(float[] destination)
        {
            if (destination == null || destination.Length == 0 || count == 0)
            {
                return 0;
            }

            var written = count < destination.Length ? count : destination.Length;
            var start = count < values.Length ? 0 : next;
            for (var i = 0; i < written; i++)
            {
                destination[i] = values[(start + i) % values.Length];
            }

            return written;
        }

        public float Latest()
        {
            if (count == 0)
            {
                return 0f;
            }

            var index = next == 0 ? values.Length - 1 : next - 1;
            return values[index];
        }
    }
}
