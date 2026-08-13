using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace EvoLife.Analytics
{
    /// <summary>
    /// Minimal JSON helper for nested analytics payloads. Avoids JsonUtility's
    /// Dictionary limitation without requiring a Newtonsoft asmdef reference.
    /// </summary>
    public static class AnalyticsJson
    {
        public static string Serialize(object value)
        {
            var builder = new StringBuilder();
            WriteValue(builder, value);
            return builder.ToString();
        }

        static void WriteValue(StringBuilder builder, object value)
        {
            if (value == null)
            {
                builder.Append("null");
                return;
            }

            var type = value.GetType();
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                var hasValue = (bool)type.GetProperty("HasValue").GetValue(value);
                if (!hasValue)
                {
                    builder.Append("null");
                    return;
                }

                WriteValue(builder, type.GetProperty("Value").GetValue(value));
                return;
            }

            switch (value)
            {
                case string text:
                    WriteString(builder, text);
                    return;
                case bool flag:
                    builder.Append(flag ? "true" : "false");
                    return;
                case byte _:
                case sbyte _:
                case short _:
                case ushort _:
                case int _:
                case uint _:
                case long _:
                case ulong _:
                    builder.Append(Convert.ToString(value, CultureInfo.InvariantCulture));
                    return;
                case float number:
                    builder.Append(number.ToString("G9", CultureInfo.InvariantCulture));
                    return;
                case double number:
                    builder.Append(number.ToString("G17", CultureInfo.InvariantCulture));
                    return;
                case IDictionary dictionary:
                    WriteDictionary(builder, dictionary);
                    return;
                case IEnumerable enumerable when !(value is string):
                    WriteArray(builder, enumerable);
                    return;
                default:
                    WriteObject(builder, value);
                    return;
            }
        }

        static void WriteDictionary(StringBuilder builder, IDictionary dictionary)
        {
            builder.Append('{');
            var first = true;
            foreach (DictionaryEntry entry in dictionary)
            {
                if (entry.Value == null)
                {
                    continue;
                }

                if (!first)
                {
                    builder.Append(',');
                }

                first = false;
                WriteString(builder, Convert.ToString(entry.Key, CultureInfo.InvariantCulture) ?? string.Empty);
                builder.Append(':');
                WriteValue(builder, entry.Value);
            }

            builder.Append('}');
        }

        static void WriteArray(StringBuilder builder, IEnumerable enumerable)
        {
            builder.Append('[');
            var first = true;
            foreach (var item in enumerable)
            {
                if (!first)
                {
                    builder.Append(',');
                }

                first = false;
                WriteValue(builder, item);
            }

            builder.Append(']');
        }

        static void WriteObject(StringBuilder builder, object value)
        {
            builder.Append('{');
            var first = true;
            var fields = value.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public);
            for (var i = 0; i < fields.Length; i++)
            {
                var field = fields[i];
                var fieldValue = field.GetValue(value);
                if (fieldValue == null)
                {
                    continue;
                }

                if (!first)
                {
                    builder.Append(',');
                }

                first = false;
                var map = Attribute.GetCustomAttribute(field, typeof(JsonMapAttribute)) as JsonMapAttribute;
                WriteString(builder, map != null ? map.Name : field.Name);
                builder.Append(':');
                WriteValue(builder, fieldValue);
            }

            builder.Append('}');
        }

        static void WriteString(StringBuilder builder, string value)
        {
            builder.Append('"');
            for (var i = 0; i < value.Length; i++)
            {
                var c = value[i];
                switch (c)
                {
                    case '"':
                        builder.Append("\\\"");
                        break;
                    case '\\':
                        builder.Append("\\\\");
                        break;
                    case '\n':
                        builder.Append("\\n");
                        break;
                    case '\r':
                        builder.Append("\\r");
                        break;
                    case '\t':
                        builder.Append("\\t");
                        break;
                    default:
                        builder.Append(c);
                        break;
                }
            }

            builder.Append('"');
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class JsonMapAttribute : Attribute
    {
        public JsonMapAttribute(string name) => Name = name;
        public string Name { get; }
    }

    public sealed class RunCreateDto
    {
        [JsonMap("experiment_name")] public string ExperimentName;
        [JsonMap("random_seed")] public int? RandomSeed;
        [JsonMap("configuration")] public Dictionary<string, object> Configuration;
        [JsonMap("status")] public string Status = "running";
        [JsonMap("metadata")] public Dictionary<string, object> Metadata;
    }

    public sealed class RunCreateResponseDto
    {
        [JsonMap("run_id")] public string RunId;
    }

    public sealed class SnapshotCreateDto
    {
        [JsonMap("simulation_time")] public float SimulationTime;
        [JsonMap("herbivore_population")] public int HerbivorePopulation;
        [JsonMap("predator_population")] public int PredatorPopulation;
        [JsonMap("plant_count")] public int PlantCount;
        [JsonMap("births")] public int Births;
        [JsonMap("deaths")] public int Deaths;
        [JsonMap("extra_metrics")] public Dictionary<string, object> ExtraMetrics;
    }

    public sealed class SnapshotBatchDto
    {
        [JsonMap("snapshots")] public List<SnapshotCreateDto> Snapshots;
    }

    public sealed class CreatureLifeRecordDto
    {
        [JsonMap("creature_id")] public string CreatureId;
        [JsonMap("species")] public string Species;
        [JsonMap("generation")] public int Generation;
        [JsonMap("birth_time")] public float BirthTime;
        [JsonMap("death_time")] public float? DeathTime;
        [JsonMap("cause_of_death")] public string CauseOfDeath;
        [JsonMap("parent_id_1")] public string ParentId1;
        [JsonMap("parent_id_2")] public string ParentId2;
        [JsonMap("offspring_count")] public int OffspringCount;
        [JsonMap("genome_traits")] public Dictionary<string, float> GenomeTraits;
        [JsonMap("policy_kind")] public string PolicyKind;
        [JsonMap("extra_fields")] public Dictionary<string, object> ExtraFields;
    }

    public sealed class CreatureBatchDto
    {
        [JsonMap("records")] public List<CreatureLifeRecordDto> Records;
    }

    public sealed class GenerationSummaryDto
    {
        [JsonMap("species")] public string Species;
        [JsonMap("generation")] public int Generation;
        [JsonMap("population_count")] public int PopulationCount;
        [JsonMap("average_genome_traits")] public Dictionary<string, float> AverageGenomeTraits;
        [JsonMap("average_lifespan")] public float? AverageLifespan;
        [JsonMap("extra_statistics")] public Dictionary<string, object> ExtraStatistics;
    }

    public sealed class GenerationBatchDto
    {
        [JsonMap("summaries")] public List<GenerationSummaryDto> Summaries;
    }

    public static class AnalyticsDtoMapper
    {
        public static SnapshotCreateDto ToSnapshotDto(SimulationStatsSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return null;
            }

            return new SnapshotCreateDto
            {
                SimulationTime = snapshot.simulationTimeSeconds,
                HerbivorePopulation = snapshot.herbivoreCount,
                PredatorPopulation = snapshot.predatorCount,
                PlantCount = 0,
                Births = snapshot.births,
                Deaths = snapshot.deaths,
                ExtraMetrics = new Dictionary<string, object>
                {
                    ["totalAlive"] = snapshot.totalAlive,
                    ["timestampUtcUnix"] = snapshot.timestampUtcUnix,
                    ["population_change"] = snapshot.populationChange,
                    ["scripted_alive"] = snapshot.scriptedAlive,
                    ["ppo_alive"] = snapshot.ppoAlive,
                    ["max_generation"] = snapshot.maxGeneration,
                    ["source"] = "unity_analytics"
                }
            };
        }

        public static CreatureLifeRecordDto ToCreatureDto(CreatureLifetimeRecord record)
        {
            if (record == null)
            {
                return null;
            }

            var extra = new Dictionary<string, object>
            {
                ["role"] = record.Role ?? "herbivore",
                ["lifetime"] = record.Lifetime,
                ["episode_survival_seconds"] = record.EpisodeSurvivalSeconds,
                ["completed_episode_count"] = record.CompletedEpisodeCount
            };

            if (record.HasEpisodeReturn)
            {
                extra["episode_return"] = record.EpisodeReturn;
            }

            return new CreatureLifeRecordDto
            {
                CreatureId = record.CreatureId,
                Species = string.IsNullOrEmpty(record.Species) ? "unspecified" : record.Species,
                Generation = record.Generation,
                BirthTime = record.BirthTime,
                DeathTime = record.DeathTime,
                CauseOfDeath = record.CauseOfDeath,
                ParentId1 = record.ParentId1,
                ParentId2 = record.ParentId2,
                OffspringCount = record.OffspringCount,
                GenomeTraits = record.GenomeTraits ?? new Dictionary<string, float>(),
                PolicyKind = record.PolicyKind,
                ExtraFields = extra
            };
        }

        public static GenerationSummaryDto ToGenerationDto(GenerationAggregate overall, IReadOnlyList<GenerationAggregate> policySlices)
        {
            if (overall == null)
            {
                return null;
            }

            var extra = new Dictionary<string, object>
            {
                ["role"] = overall.Role ?? string.Empty,
                ["trait_variance"] = overall.TraitVariance ?? new Dictionary<string, float>(),
                ["trait_min"] = overall.TraitMin ?? new Dictionary<string, float>(),
                ["trait_max"] = overall.TraitMax ?? new Dictionary<string, float>(),
                ["max_generation"] = overall.MaxGenerationReached
            };

            if (policySlices != null && policySlices.Count > 0)
            {
                var byPolicy = new Dictionary<string, object>();
                for (var i = 0; i < policySlices.Count; i++)
                {
                    var slice = policySlices[i];
                    if (slice == null || string.IsNullOrEmpty(slice.PolicyKind))
                    {
                        continue;
                    }

                    byPolicy[slice.PolicyKind] = new Dictionary<string, object>
                    {
                        ["population_count"] = slice.PopulationCount,
                        ["average_lifespan"] = slice.AverageLifespan,
                        ["average_genome_traits"] = slice.AverageTraits,
                        ["trait_variance"] = slice.TraitVariance
                    };
                }

                if (byPolicy.Count > 0)
                {
                    extra["by_policy"] = byPolicy;
                }
            }

            return new GenerationSummaryDto
            {
                Species = string.IsNullOrEmpty(overall.Species) ? "unspecified" : overall.Species,
                Generation = overall.Generation,
                PopulationCount = overall.PopulationCount,
                AverageGenomeTraits = overall.AverageTraits ?? new Dictionary<string, float>(),
                AverageLifespan = overall.AverageLifespan,
                ExtraStatistics = extra
            };
        }
    }
}
