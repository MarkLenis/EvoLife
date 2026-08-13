using System.Collections.Generic;
using EvoLife.Common;

namespace EvoLife.Analytics
{
    public sealed class GenerationAggregate
    {
        public string Species;
        public string Role;
        public string PolicyKind;
        public int Generation;
        public int PopulationCount;
        public float AverageLifespan;
        public Dictionary<string, float> AverageTraits = new Dictionary<string, float>();
        public Dictionary<string, float> TraitVariance = new Dictionary<string, float>();
        public Dictionary<string, float> TraitMin = new Dictionary<string, float>();
        public Dictionary<string, float> TraitMax = new Dictionary<string, float>();
        public int MaxGenerationReached;
    }

    public sealed class CreatureTraitSample
    {
        public string Species;
        public string Role;
        public string PolicyKind;
        public int Generation;
        public float Lifespan;
        public Dictionary<string, float> Traits = new Dictionary<string, float>();
    }

    /// <summary>
    /// Aggregates trait distributions by generation, species/role, and policy.
    /// Empty groups produce count 0 and 0.0 means — never divide-by-zero.
    /// There is no global fitness score.
    /// </summary>
    public static class GenerationAggregator
    {
        public static List<GenerationAggregate> Aggregate(IReadOnlyList<CreatureTraitSample> samples)
        {
            var result = new List<GenerationAggregate>();
            if (samples == null || samples.Count == 0)
            {
                return result;
            }

            var maxGeneration = 0;
            var groups = new Dictionary<string, SampleGroup>();
            for (var i = 0; i < samples.Count; i++)
            {
                var sample = samples[i];
                if (sample == null)
                {
                    continue;
                }

                if (sample.Generation > maxGeneration)
                {
                    maxGeneration = sample.Generation;
                }

                Add(groups, SliceKey(sample.Species, sample.Role, string.Empty, sample.Generation), sample, string.Empty);
                Add(groups, SliceKey(sample.Species, sample.Role, sample.PolicyKind, sample.Generation), sample, sample.PolicyKind);
            }

            foreach (var pair in groups)
            {
                result.Add(Build(pair.Value.Items, maxGeneration, pair.Value.PolicyKind));
            }

            return result;
        }

        public static CreatureTraitSample FromView(IAnalyticsCreatureView view, float lifespan)
        {
            if (view == null)
            {
                return null;
            }

            var identity = view.Identity;
            var lineage = view.Lineage;
            return new CreatureTraitSample
            {
                Species = identity != null ? identity.SpeciesId : "unspecified",
                Role = identity != null ? CreatureRoleNames.ToWireName(identity.Role) : "herbivore",
                PolicyKind = PolicyClassifier.Classify(view),
                Generation = lineage != null ? lineage.Generation : 0,
                Lifespan = lifespan < 0f ? 0f : lifespan,
                Traits = GenomeTraitMaps.Copy(view.GenomeTraits)
            };
        }

        public static CreatureTraitSample FromLifetime(CreatureLifetimeRecord record)
        {
            if (record == null)
            {
                return null;
            }

            return new CreatureTraitSample
            {
                Species = record.Species,
                Role = record.Role,
                PolicyKind = record.PolicyKind,
                Generation = record.Generation,
                Lifespan = record.Lifetime,
                Traits = record.GenomeTraits != null
                    ? new Dictionary<string, float>(record.GenomeTraits)
                    : new Dictionary<string, float>()
            };
        }

        static void Add(
            Dictionary<string, SampleGroup> groups,
            string key,
            CreatureTraitSample sample,
            string policyKind)
        {
            if (!groups.TryGetValue(key, out var group))
            {
                group = new SampleGroup
                {
                    PolicyKind = policyKind ?? string.Empty,
                    Items = new List<CreatureTraitSample>()
                };
                groups[key] = group;
            }

            group.Items.Add(sample);
        }

        static string SliceKey(string species, string role, string policy, int generation) =>
            (species ?? "unspecified") + "|" + (role ?? "") + "|" + (policy ?? "") + "|" + generation;

        static GenerationAggregate Build(List<CreatureTraitSample> group, int maxGeneration, string policyKind)
        {
            var first = group[0];
            var lifespans = new List<float>(group.Count);
            var traitValues = new Dictionary<string, List<float>>();
            for (var i = 0; i < group.Count; i++)
            {
                lifespans.Add(group[i].Lifespan);
                if (group[i].Traits == null)
                {
                    continue;
                }

                foreach (var trait in group[i].Traits)
                {
                    if (!traitValues.TryGetValue(trait.Key, out var values))
                    {
                        values = new List<float>();
                        traitValues[trait.Key] = values;
                    }

                    values.Add(trait.Value);
                }
            }

            var aggregate = new GenerationAggregate
            {
                Species = first.Species,
                Role = first.Role,
                PolicyKind = policyKind ?? string.Empty,
                Generation = first.Generation,
                PopulationCount = group.Count,
                AverageLifespan = TraitStatistics.Mean(lifespans),
                MaxGenerationReached = maxGeneration
            };

            foreach (var trait in traitValues)
            {
                aggregate.AverageTraits[trait.Key] = TraitStatistics.Mean(trait.Value);
                aggregate.TraitVariance[trait.Key] = TraitStatistics.Variance(trait.Value);
                aggregate.TraitMin[trait.Key] = TraitStatistics.Min(trait.Value);
                aggregate.TraitMax[trait.Key] = TraitStatistics.Max(trait.Value);
            }

            return aggregate;
        }

        sealed class SampleGroup
        {
            public string PolicyKind;
            public List<CreatureTraitSample> Items;
        }
    }
}
