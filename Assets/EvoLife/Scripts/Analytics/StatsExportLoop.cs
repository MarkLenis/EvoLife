using System.Collections.Generic;
using UnityEngine;

namespace EvoLife.Analytics
{
    /// <summary>
    /// Periodically captures and batches statistics. Never posts every frame.
    /// Backend failures are ignored so the simulation can continue.
    /// </summary>
    public sealed class StatsExportLoop : MonoBehaviour
    {
        [SerializeField] PopulationStatisticCollector collector;
        [SerializeField] CreatureLifetimeRecorder lifetimeRecorder;
        [SerializeField] GenerationAnalyticsCollector generationCollector;
        [SerializeField] ExperimentSession experimentSession;
        [SerializeField] BackendClient backendClient;
        [SerializeField] float intervalSeconds = 5f;
        [SerializeField] int snapshotBatchSize = 1;
        [SerializeField] bool uploadToBackend;
        [SerializeField] bool useExtendedRunApi = true;
        [SerializeField] bool uploadGenerationSummaries = true;

        float elapsed;
        readonly List<SnapshotCreateDto> snapshotBuffer = new List<SnapshotCreateDto>();
        bool generationDirty = true;

        void Update()
        {
            if (collector == null || intervalSeconds <= 0f)
            {
                return;
            }

            elapsed += Time.unscaledDeltaTime;
            if (elapsed < intervalSeconds)
            {
                return;
            }

            elapsed = 0f;
            CaptureAndMaybeUpload();
        }

        void OnDisable()
        {
            if (uploadToBackend)
            {
                Flush(force: true);
            }
        }

        bool CanUseExtendedApi =>
            useExtendedRunApi && experimentSession != null && experimentSession.RunReady;

        void CaptureAndMaybeUpload()
        {
            if (lifetimeRecorder != null)
            {
                collector.SetLiveViews(lifetimeRecorder.LiveViews);
            }

            var snapshot = collector.Capture();
            if (!uploadToBackend || backendClient == null)
            {
                return;
            }

            if (experimentSession != null && !experimentSession.StartupComplete)
            {
                return;
            }

            if (CanUseExtendedApi)
            {
                snapshotBuffer.Add(AnalyticsDtoMapper.ToSnapshotDto(snapshot));
                Flush(force: snapshotBuffer.Count >= Mathf.Max(1, snapshotBatchSize));
            }
            else
            {
                _ = backendClient.PostSnapshotAsync(snapshot);
            }
        }

        void Flush(bool force)
        {
            if (!CanUseExtendedApi || backendClient == null)
            {
                snapshotBuffer.Clear();
                return;
            }

            var runId = experimentSession.RunId;
            if (snapshotBuffer.Count > 0 && (force || snapshotBuffer.Count >= Mathf.Max(1, snapshotBatchSize)))
            {
                var batch = new List<SnapshotCreateDto>(snapshotBuffer);
                snapshotBuffer.Clear();
                _ = backendClient.PostSnapshotBatchAsync(runId, batch);
            }

            if (lifetimeRecorder != null)
            {
                var records = lifetimeRecorder.DrainCompleted();
                if (records.Count > 0)
                {
                    var dtos = new List<CreatureLifeRecordDto>(records.Count);
                    for (var i = 0; i < records.Count; i++)
                    {
                        dtos.Add(AnalyticsDtoMapper.ToCreatureDto(records[i]));
                    }

                    _ = backendClient.PostCreatureRecordsAsync(runId, dtos);
                    generationDirty = true;
                }
            }

            if (uploadGenerationSummaries && generationCollector != null && (force || generationDirty))
            {
                var summaries = generationCollector.CaptureUploadSummaries();
                if (summaries.Count > 0)
                {
                    _ = backendClient.PostGenerationSummariesAsync(runId, summaries);
                }

                generationDirty = false;
            }
        }
    }
}
