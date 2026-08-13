using System.Globalization;

namespace EvoLife.UI
{
    /// <summary>
    /// UI-local sampled history for live charts. Bounded, presentation-only.
    /// </summary>
    public sealed class DashboardChartSampler
    {
        public const int DefaultCapacity = 96;
        public const float DefaultIntervalSeconds = 0.5f;

        readonly ChartRingBuffer herbivores;
        readonly ChartRingBuffer predators;
        readonly ChartRingBuffer births;
        readonly ChartRingBuffer deaths;
        readonly ChartRingBuffer plantAbundance;
        readonly ChartRingBuffer selectedTrait;
        readonly float[] copy;
        float nextSampleAt;
        bool hasSampled;

        public DashboardChartSampler(int capacity = DefaultCapacity)
        {
            herbivores = new ChartRingBuffer(capacity);
            predators = new ChartRingBuffer(capacity);
            births = new ChartRingBuffer(capacity);
            deaths = new ChartRingBuffer(capacity);
            plantAbundance = new ChartRingBuffer(capacity);
            selectedTrait = new ChartRingBuffer(capacity);
            copy = new float[capacity];
        }

        public int Capacity => herbivores.Capacity;

        public bool TrySample(float simulationTime, DashboardModel model, float intervalSeconds, float? traitMean)
        {
            if (model == null)
            {
                return false;
            }

            if (hasSampled && simulationTime < nextSampleAt)
            {
                return false;
            }

            herbivores.Push(model.HerbivoresAlive);
            predators.Push(model.PredatorsAlive);
            births.Push(model.Births);
            deaths.Push(model.Deaths);
            var abundance = 0f;
            if (model.PlantAbundance != DashboardPresenter.Unavailable)
            {
                float.TryParse(
                    model.PlantAbundance,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out abundance);
            }

            plantAbundance.Push(abundance);
            selectedTrait.Push(traitMean ?? 0f);
            nextSampleAt = simulationTime + (intervalSeconds > 0.05f ? intervalSeconds : DefaultIntervalSeconds);
            hasSampled = true;
            return true;
        }

        public string HerbivoreSparkline() => Format(herbivores);

        public string PredatorSparkline() => Format(predators);

        public string BirthsSparkline() => Format(births);

        public string DeathsSparkline() => Format(deaths);

        public string AbundanceSparkline() => Format(plantAbundance);

        public string TraitSparkline() => Format(selectedTrait);

        string Format(ChartRingBuffer buffer)
        {
            var count = buffer.CopyChronological(copy);
            return SparklineFormatter.FormatWithLatest(copy, count);
        }
    }
}
