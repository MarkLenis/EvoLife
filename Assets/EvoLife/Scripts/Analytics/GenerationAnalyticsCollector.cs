using System.Collections.Generic;
using UnityEngine;

namespace EvoLife.Analytics
{
    /// <summary>
    /// Builds generation / trait distribution summaries from observed creatures.
    /// Selection is not scored here — only survival/reproduction outcomes already recorded.
    /// </summary>
    public sealed class GenerationAnalyticsCollector : MonoBehaviour
    {
        [SerializeField] CreatureLifetimeRecorder lifetimeRecorder;

        public List<GenerationSummaryDto> CaptureUploadSummaries()
        {
            var samples = lifetimeRecorder != null
                ? lifetimeRecorder.CaptureTraitSamples()
                : new List<CreatureTraitSample>();
            return BuildUploadSummaries(GenerationAggregator.Aggregate(samples));
        }

        public static List<GenerationSummaryDto> BuildUploadSummaries(IReadOnlyList<GenerationAggregate> aggregates)
        {
            var result = new List<GenerationSummaryDto>();
            if (aggregates == null || aggregates.Count == 0)
            {
                return result;
            }

            var overall = new Dictionary<string, GenerationAggregate>();
            var policySlices = new Dictionary<string, List<GenerationAggregate>>();

            for (var i = 0; i < aggregates.Count; i++)
            {
                var item = aggregates[i];
                if (item == null)
                {
                    continue;
                }

                var key = (item.Species ?? "unspecified") + "|" + item.Generation;
                if (string.IsNullOrEmpty(item.PolicyKind))
                {
                    overall[key] = item;
                    continue;
                }

                if (!policySlices.TryGetValue(key, out var list))
                {
                    list = new List<GenerationAggregate>();
                    policySlices[key] = list;
                }

                list.Add(item);
            }

            foreach (var pair in overall)
            {
                policySlices.TryGetValue(pair.Key, out var slices);
                result.Add(AnalyticsDtoMapper.ToGenerationDto(pair.Value, slices));
            }

            return result;
        }
    }
}
