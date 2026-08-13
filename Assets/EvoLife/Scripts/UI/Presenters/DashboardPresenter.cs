using System.Collections.Generic;
using System.Globalization;
using System.Text;
using EvoLife.Analytics;
using EvoLife.Common;

namespace EvoLife.UI
{
    public sealed class TraitMeanDisplay
    {
        public string Name;
        public string Mean;
        public bool HasSamples;
    }

    public sealed class DashboardModel
    {
        public int HerbivoresAlive;
        public int PredatorsAlive;
        public int TotalAlive;
        public int Births;
        public int Deaths;
        public string PredatorPreyRatio;
        public int MaxLivingGeneration;
        public int ScriptedAlive;
        public int PpoAlive;
        public int PlantCount;
        public string PlantFoodRemaining;
        public string PlantAbundance;
        public int WaterSourceCount;
        public string ActiveEvents;
        public string SimulationTime;
        public string DayNight;
        public string ExperimentName;
        public string Scenario;
        public string RandomSeed;
        public string HerbivorePolicy;
        public string PredatorPolicy;
        public string ModelId;
        public string GenerationSummary;
        public TraitMeanDisplay[] TraitMeans;
        public string SummaryText;
    }

    public sealed class DashboardInputs
    {
        public IPopulationSnapshot Population;
        public SimulationStatsSnapshot Stats;
        public IReadOnlyEnvironmentState Environment;
        public ISimulationClock Clock;
        public string ExperimentName;
        public string ScenarioId;
        public int? RandomSeed;
        public AgentPolicyKind? HerbivorePolicy;
        public AgentPolicyKind? PredatorPolicy;
        public string ModelId;
        public IReadOnlyList<IAnalyticsCreatureView> LiveViews;
    }

    /// <summary>
    /// Live dashboard view-model from read-only analytics/simulation/environment contracts.
    /// </summary>
    public static class DashboardPresenter
    {
        public const string Unavailable = "unavailable";

        public static DashboardModel Build(DashboardInputs inputs)
        {
            inputs = inputs ?? new DashboardInputs();
            var stats = inputs.Stats;
            var population = inputs.Population;
            var herb = stats != null ? stats.herbivoreCount : population != null ? population.HerbivoreCount : 0;
            var pred = stats != null ? stats.predatorCount : population != null ? population.PredatorCount : 0;
            var alive = stats != null ? stats.totalAlive : population != null ? population.TotalAlive : herb + pred;
            var births = stats != null ? stats.births : population != null ? population.Births : 0;
            var deaths = stats != null ? stats.deaths : population != null ? population.Deaths : 0;
            var maxGen = stats != null ? stats.maxGeneration : MaxGeneration(inputs.LiveViews);
            var census = inputs.Environment != null ? inputs.Environment.Resources : null;
            var dayNight = inputs.Environment != null ? inputs.Environment.DayNight : null;
            var time = inputs.Clock != null
                ? inputs.Clock.SimulationTimeSeconds
                : stats != null ? stats.simulationTimeSeconds : 0f;

            var model = new DashboardModel
            {
                HerbivoresAlive = herb,
                PredatorsAlive = pred,
                TotalAlive = alive,
                Births = births,
                Deaths = deaths,
                PredatorPreyRatio = RatioFormatter.PredatorPrey(pred, herb),
                MaxLivingGeneration = maxGen,
                ScriptedAlive = stats != null ? stats.scriptedAlive : 0,
                PpoAlive = stats != null ? stats.ppoAlive : 0,
                PlantCount = census != null ? census.PlantCount : 0,
                PlantFoodRemaining = census != null
                    ? census.TotalPlantFoodRemaining.ToString("0.##", CultureInfo.InvariantCulture)
                      + " / "
                      + census.TotalPlantCapacity.ToString("0.##", CultureInfo.InvariantCulture)
                    : Unavailable,
                PlantAbundance = census != null
                    ? census.PlantAbundance.ToString("0.00", CultureInfo.InvariantCulture)
                    : Unavailable,
                WaterSourceCount = census != null ? census.WaterSourceCount : 0,
                ActiveEvents = EventPanelPresenter.FormatActiveList(
                    inputs.Environment != null ? inputs.Environment.ActiveEvents : null,
                    time),
                SimulationTime = SimulationControlPresenter.FormatTime(time),
                DayNight = FormatDayNight(dayNight),
                ExperimentName = string.IsNullOrEmpty(inputs.ExperimentName) ? Unavailable : inputs.ExperimentName,
                Scenario = string.IsNullOrEmpty(inputs.ScenarioId) ? Unavailable : inputs.ScenarioId,
                RandomSeed = inputs.RandomSeed.HasValue
                    ? inputs.RandomSeed.Value.ToString(CultureInfo.InvariantCulture)
                    : Unavailable,
                HerbivorePolicy = inputs.HerbivorePolicy.HasValue
                    ? PolicyDisplayFormatter.FormatKind(inputs.HerbivorePolicy.Value)
                    : Unavailable,
                PredatorPolicy = inputs.PredatorPolicy.HasValue
                    ? PolicyDisplayFormatter.FormatKind(inputs.PredatorPolicy.Value)
                    : Unavailable,
                ModelId = PolicyDisplayFormatter.FormatModelId(inputs.ModelId),
                TraitMeans = BuildTraitMeans(inputs.LiveViews),
                GenerationSummary = FormatGenerationSummary(maxGen, inputs.LiveViews)
            };
            model.SummaryText = BuildSummary(model);
            return model;
        }

        public static float? MeanTrait(IReadOnlyList<IAnalyticsCreatureView> liveViews, string traitName)
        {
            if (liveViews == null || string.IsNullOrEmpty(traitName))
            {
                return null;
            }

            var sum = 0f;
            var count = 0;
            for (var i = 0; i < liveViews.Count; i++)
            {
                var genome = liveViews[i]?.GenomeTraits;
                if (genome == null)
                {
                    continue;
                }

                if (!genome.TryGetTrait(traitName, out var value))
                {
                    continue;
                }

                sum += value;
                count++;
            }

            if (count == 0)
            {
                return null;
            }

            return sum / count;
        }

        static TraitMeanDisplay[] BuildTraitMeans(IReadOnlyList<IAnalyticsCreatureView> liveViews)
        {
            var names = CanonicalTraitNames.InSchemaOrder;
            var result = new TraitMeanDisplay[names.Length];
            for (var i = 0; i < names.Length; i++)
            {
                var mean = MeanTrait(liveViews, names[i]);
                result[i] = new TraitMeanDisplay
                {
                    Name = names[i],
                    HasSamples = mean.HasValue,
                    Mean = mean.HasValue
                        ? mean.Value.ToString("0.###", CultureInfo.InvariantCulture)
                        : Unavailable
                };
            }

            return result;
        }

        static int MaxGeneration(IReadOnlyList<IAnalyticsCreatureView> liveViews)
        {
            if (liveViews == null)
            {
                return 0;
            }

            var max = 0;
            for (var i = 0; i < liveViews.Count; i++)
            {
                var generation = liveViews[i]?.Lineage != null ? liveViews[i].Lineage.Generation : 0;
                if (generation > max)
                {
                    max = generation;
                }
            }

            return max;
        }

        static string FormatGenerationSummary(int maxGeneration, IReadOnlyList<IAnalyticsCreatureView> liveViews)
        {
            var living = liveViews != null ? liveViews.Count : 0;
            return "max gen " + maxGeneration.ToString(CultureInfo.InvariantCulture)
                + ", living sampled " + living.ToString(CultureInfo.InvariantCulture);
        }

        static string FormatDayNight(IReadOnlyDayNightState dayNight)
        {
            if (dayNight == null)
            {
                return Unavailable;
            }

            var phase = dayNight.IsNight ? "night" : "day";
            return phase + "  t=" + dayNight.NormalizedTimeOfDay.ToString("0.00", CultureInfo.InvariantCulture);
        }

        static string BuildSummary(DashboardModel model)
        {
            var builder = new StringBuilder(768);
            builder.AppendLine("POPULATION");
            builder.AppendLine("  Herbivores: " + model.HerbivoresAlive);
            builder.AppendLine("  Predators: " + model.PredatorsAlive);
            builder.AppendLine("  Total alive: " + model.TotalAlive);
            builder.AppendLine("  Births: " + model.Births);
            builder.AppendLine("  Deaths: " + model.Deaths);
            builder.AppendLine("  Predator/prey: " + model.PredatorPreyRatio);
            builder.AppendLine("  Max generation: " + model.MaxLivingGeneration);
            builder.AppendLine("  Scripted alive: " + model.ScriptedAlive);
            builder.AppendLine("  PPO alive: " + model.PpoAlive);
            builder.AppendLine("ENVIRONMENT");
            builder.AppendLine("  Plants: " + model.PlantCount);
            builder.AppendLine("  Plant food: " + model.PlantFoodRemaining);
            builder.AppendLine("  Abundance: " + model.PlantAbundance);
            builder.AppendLine("  Water sources: " + model.WaterSourceCount);
            builder.AppendLine("  Events: " + model.ActiveEvents);
            builder.AppendLine("  Sim time: " + model.SimulationTime);
            builder.AppendLine("  Day/night: " + model.DayNight);
            builder.AppendLine("EXPERIMENT");
            builder.AppendLine("  Name: " + model.ExperimentName);
            builder.AppendLine("  Scenario: " + model.Scenario);
            builder.AppendLine("  Seed: " + model.RandomSeed);
            builder.AppendLine("  Herbivore policy: " + model.HerbivorePolicy);
            builder.AppendLine("  Predator policy: " + model.PredatorPolicy);
            builder.AppendLine("  PPO model: " + model.ModelId);
            builder.AppendLine("EVOLUTION");
            builder.AppendLine("  " + model.GenerationSummary);
            if (model.TraitMeans != null)
            {
                for (var i = 0; i < model.TraitMeans.Length; i++)
                {
                    var trait = model.TraitMeans[i];
                    builder.AppendLine("  " + trait.Name + " mean: " + trait.Mean);
                }
            }

            return builder.ToString();
        }
    }
}
