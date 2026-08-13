using System.Collections.Generic;
using UnityEngine;

namespace EvoLife.Analytics
{
    /// <summary>
    /// Periodically captures and batches statistics. Never posts every frame.
    /// Pending records are retained until a POST is confirmed successful. Temporary backend
    /// failures do not discard data; overflow drops the oldest queued records when the
    /// bounded backlog is full. Simulation continues if FastAPI is unavailable.
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
        [SerializeField] int maxPendingSnapshots = 64;
        [SerializeField] int maxPendingLifetimeRecords = 256;

        float elapsed;
        bool generationDirty = true;
        float lastWarningUnscaledTime = -999f;
        AnalyticsExportController exportController;

        public AnalyticsExportController ExportController =>
            exportController ?? (exportController = CreateController());

        void Awake()
        {
            exportController = CreateController();
        }

        AnalyticsExportController CreateController() =>
            new AnalyticsExportController(maxPendingSnapshots, maxPendingLifetimeRecords, maxPendingSnapshots);

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
                EnqueuePendingUploads();
                _ = FlushAsync(force: true);
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

            EnqueuePendingUploads(snapshot);
            _ = FlushAsync(force: false);
        }

        void EnqueuePendingUploads(SimulationStatsSnapshot snapshot = null)
        {
            if (!uploadToBackend || backendClient == null)
            {
                return;
            }

            if (snapshot != null)
            {
                if (CanUseExtendedApi)
                {
                    ExportController.EnqueueSnapshot(AnalyticsDtoMapper.ToSnapshotDto(snapshot));
                }
                else
                {
                    ExportController.EnqueueV1Snapshot(snapshot);
                }
            }

            if (lifetimeRecorder != null)
            {
                var records = lifetimeRecorder.DrainCompleted();
                if (records != null && records.Count > 0)
                {
                    var dtos = new List<CreatureLifeRecordDto>(records.Count);
                    for (var i = 0; i < records.Count; i++)
                    {
                        dtos.Add(AnalyticsDtoMapper.ToCreatureDto(records[i]));
                    }

                    ExportController.EnqueueLifetimes(dtos);
                    generationDirty = true;
                }
            }

            if (uploadGenerationSummaries && generationCollector != null && generationDirty)
            {
                var summaries = generationCollector.CaptureUploadSummaries();
                if (summaries != null && summaries.Count > 0)
                {
                    ExportController.ReplaceGenerations(summaries);
                }

                generationDirty = false;
            }
        }

        async System.Threading.Tasks.Task FlushAsync(bool force)
        {
            if (!uploadToBackend || backendClient == null)
            {
                return;
            }

            if (ExportController.FlushInFlight)
            {
                return;
            }

            if (!force
                && CanUseExtendedApi
                && ExportController.PendingSnapshotCount < Mathf.Max(1, snapshotBatchSize)
                && ExportController.PendingLifetimeCount == 0
                && ExportController.PendingGenerationCount == 0)
            {
                return;
            }

            var batch = ExportController.TryBeginFlush();
            if (batch == null)
            {
                return;
            }

            var snapshotsOk = true;
            var lifetimesOk = true;
            var generationsOk = true;
            var v1SuccessCount = 0;

            try
            {
                if (CanUseExtendedApi)
                {
                    var runId = experimentSession.RunId;
                    if (batch.Snapshots != null && batch.Snapshots.Count > 0)
                    {
                        snapshotsOk = await backendClient.PostSnapshotBatchAsync(runId, batch.Snapshots);
                    }

                    if (batch.Lifetimes != null && batch.Lifetimes.Count > 0)
                    {
                        lifetimesOk = await backendClient.PostCreatureRecordsAsync(runId, batch.Lifetimes);
                    }

                    if (batch.Generations != null && batch.Generations.Count > 0)
                    {
                        generationsOk = await backendClient.PostGenerationSummariesAsync(runId, batch.Generations);
                    }
                }
                else if (batch.V1Snapshots != null)
                {
                    for (var i = 0; i < batch.V1Snapshots.Count; i++)
                    {
                        var posted = await backendClient.PostSnapshotAsync(batch.V1Snapshots[i]);
                        if (!posted)
                        {
                            break;
                        }

                        v1SuccessCount++;
                    }
                }
            }
            catch (System.Exception ex)
            {
                snapshotsOk = false;
                lifetimesOk = false;
                generationsOk = false;
                Warn($"StatsExportLoop flush exception: {ex.Message}");
            }

            ExportController.CompleteFlush(snapshotsOk, lifetimesOk, generationsOk, v1SuccessCount);

            if (!snapshotsOk || !lifetimesOk || !generationsOk
                || (batch.V1Snapshots != null && v1SuccessCount < batch.V1Snapshots.Count))
            {
                Warn("StatsExportLoop: backend upload failed; pending records were retained for the next interval.");
            }

            if (ExportController.OverflowDropped > 0)
            {
                Warn(
                    $"StatsExportLoop: pending analytics overflow dropped {ExportController.OverflowDropped} oldest record(s) (bounded FIFO).");
            }
        }

        void Warn(string message)
        {
            var now = Time.unscaledTime;
            if (now - lastWarningUnscaledTime < Mathf.Max(intervalSeconds, 1f))
            {
                return;
            }

            lastWarningUnscaledTime = now;
            Debug.LogWarning(message);
        }
    }
}
